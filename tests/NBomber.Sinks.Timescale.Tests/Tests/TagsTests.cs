using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Sinks.Timescale.DAL;
using NBomber.Sinks.Timescale.Tests.Infra;
using Shouldly;

namespace NBomber.Sinks.Timescale.Tests.Tests;

public class TagsTests(EnvContextFixture fixture) : IClassFixture<EnvContextFixture>
{
    [Fact]
    public async Task Session_Start_Should_Save_Each_Tag_Key_Once()
    {
        await fixture.TestHelper.RecreateDatabase();

        // the first session: global tags and the tags of two scenarios, with the env key in both
        NBomberRunner
            .RegisterScenarios(
                CreateTaggedScenario("scenario_1", ("flow", "checkout"), ("env", "prod")),
                CreateTaggedScenario("scenario_2", ("region", "eu"))
            )
            .WithTags(("env", "prod"), ("team", "payments"))
            .WithReportingSinks(fixture.CreateTimescaleDbSinkInstance())
            .WithoutReports()
            .Run();

        var tagKeys = await fixture.TestHelper.GetTagKeys();

        tagKeys.ShouldBe(["env", "flow", "region", "team"], ignoreOrder: true);

        // the second session: a key that is saved already and a new one
        NBomberRunner
            .RegisterScenarios(CreateTaggedScenario("scenario_1", ("flow", "refund")))
            .WithTags(("env", "dev"), ("owner", "qa"))
            .WithReportingSinks(fixture.CreateTimescaleDbSinkInstance())
            .WithoutReports()
            .Run();

        tagKeys = await fixture.TestHelper.GetTagKeys();

        tagKeys.ShouldBe(["env", "flow", "owner", "region", "team"], ignoreOrder: true);

        // both sessions are saved: a key that exists already must not fail the start of a session
        var sessionsCount = await fixture.TestHelper.GetRowsCount(TableNames.SessionsTable);

        sessionsCount.ShouldBe(2);
    }

    [Fact]
    public async Task Session_Start_Should_Merge_Unique_Values_Of_Each_Tag_Key()
    {
        await fixture.TestHelper.RecreateDatabase();

        // the first session: env is prod globally and in scenario_1, and dev in scenario_2
        NBomberRunner
            .RegisterScenarios(
                CreateTaggedScenario("scenario_1", ("flow", "checkout"), ("env", "prod")),
                CreateTaggedScenario("scenario_2", ("env", "dev"))
            )
            .WithTags(("env", "prod"), ("team", "payments"))
            .WithReportingSinks(fixture.CreateTimescaleDbSinkInstance())
            .WithoutReports()
            .Run();

        var firstTags = await fixture.TestHelper.GetTagKeyRecords();

        // each key has its values once, a new row gets the same created and last_updated
        firstTags["env"].Values.ShouldBe(["prod", "dev"], ignoreOrder: true);
        firstTags["flow"].Values.ShouldBe(["checkout"]);
        firstTags["team"].Values.ShouldBe(["payments"]);
        firstTags.Values.ShouldAllBe(tag => tag.LastUpdated == tag.Created);

        // the second session: a new value of env, the value of flow that is saved already, a new key
        NBomberRunner
            .RegisterScenarios(CreateTaggedScenario("scenario_1", ("flow", "checkout")))
            .WithTags(("env", "stage"), ("owner", "qa"))
            .WithReportingSinks(fixture.CreateTimescaleDbSinkInstance())
            .WithoutReports()
            .Run();

        var secondTags = await fixture.TestHelper.GetTagKeyRecords();

        // env got a new value: it is added to the saved ones, last_updated moves, created stays
        secondTags["env"].Values.ShouldBe(["prod", "dev", "stage"], ignoreOrder: true);
        secondTags["env"].Created.ShouldBe(firstTags["env"].Created);
        secondTags["env"].LastUpdated.ShouldBeGreaterThan(firstTags["env"].LastUpdated);

        // flow got no new value and team is not in the second session: their rows are not touched
        secondTags["flow"].ShouldBeEquivalentTo(firstTags["flow"]);
        secondTags["team"].ShouldBeEquivalentTo(firstTags["team"]);

        // owner is a new key
        secondTags["owner"].Values.ShouldBe(["qa"]);
        secondTags["owner"].LastUpdated.ShouldBe(secondTags["owner"].Created);
    }

    private static ScenarioProps CreateTaggedScenario(string scenarioName, params (string, string)[] tags)
    {
        return Scenario.Create(scenarioName, async context =>
        {
            await Task.Delay(TimeSpan.FromMilliseconds(100));
            return Response.Ok();
        })
        .WithoutWarmUp()
        .WithLoadSimulations(Simulation.KeepConstant(1, during: TimeSpan.FromSeconds(1)))
        .WithTags(tags);
    }
}
