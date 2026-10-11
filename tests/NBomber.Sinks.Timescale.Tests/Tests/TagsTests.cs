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
                CreateTaggedScenario("scenario_1", new() { ["flow"] = "checkout", ["env"] = "prod" }),
                CreateTaggedScenario("scenario_2", new() { ["region"] = "eu" }))
            .WithTags(new Dictionary<string, string> { ["env"] = "prod", ["team"] = "payments" })
            .WithReportingSinks(fixture.CreateTimescaleDbSinkInstance())
            .WithoutReports()
            .Run();

        var tagKeys = await fixture.TestHelper.GetTagKeys();

        tagKeys.ShouldBe(["env", "flow", "region", "team"], ignoreOrder: true);

        // the second session: a key that is saved already and a new one
        NBomberRunner
            .RegisterScenarios(CreateTaggedScenario("scenario_1", new() { ["flow"] = "refund" }))
            .WithTags(new Dictionary<string, string> { ["env"] = "dev", ["owner"] = "qa" })
            .WithReportingSinks(fixture.CreateTimescaleDbSinkInstance())
            .WithoutReports()
            .Run();

        tagKeys = await fixture.TestHelper.GetTagKeys();

        tagKeys.ShouldBe(["env", "flow", "owner", "region", "team"], ignoreOrder: true);

        // both sessions are saved: a key that exists already must not fail the start of a session
        var sessionsCount = await fixture.TestHelper.GetRowsCount(TableNames.SessionsTable);

        sessionsCount.ShouldBe(2);
    }

    private static ScenarioProps CreateTaggedScenario(string scenarioName, Dictionary<string, string> tags)
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
