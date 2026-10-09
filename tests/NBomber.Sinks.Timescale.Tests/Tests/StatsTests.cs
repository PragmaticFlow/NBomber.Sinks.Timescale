using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Sinks.Timescale.Contracts;
using NBomber.Sinks.Timescale.DAL;
using NBomber.Sinks.Timescale.Tests.Infra;
using Shouldly;

namespace NBomber.Sinks.Timescale.Tests.Tests;

public class StatsTests(EnvContextFixture fixture) : IClassFixture<EnvContextFixture>
{
    private const string GlobalInformationStep = "global information";

    [Fact]
    public async Task When_Scenario_Finished_The_DataBase_Should_Contain_Data()
    {
        await fixture.TestHelper.RecreateDatabase();

        var scenario = Scenario.Create("user_flow_scenario", async context =>
        {
            var step1 = await Step.Run("step1", context, async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                return Response.Ok(sizeBytes: 10, statusCode: "200");
            });
            return Response.Ok(statusCode: "201", message: "hey");
        })
        .WithoutWarmUp()
        .WithLoadSimulations(Simulation.KeepConstant(1, during: TimeSpan.FromSeconds(1)));

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportingSinks(fixture.CreateTimescaleDbSinkInstance())
            .Run();

        var sessionTableCount = await fixture.TestHelper.GetRowsCount(TableNames.SessionsTable);
        var stepStatsTableCount = await fixture.TestHelper.GetRowsCount(TableNames.StepStatsTable);
        var metricsTableCount = await fixture.TestHelper.GetRowsCount(TableNames.MetricsTable);
        var sessionArtifacts = await fixture.TestHelper.GetSessionArtifacts(stats.TestInfo.SessionId);

        var htmlReport = sessionArtifacts
            .FirstOrDefault(x => x.Key.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
            .Value;

        htmlReport.ShouldNotBeNull();

        htmlReport.ShouldContain("<!DOCTYPE HTML>", Case.Insensitive);
        sessionArtifacts.ShouldContain(x => x.Key.StartsWith("nbomber-log-", StringComparison.OrdinalIgnoreCase)); // should contain file nbomber-log-****
        sessionTableCount.ShouldBe(1);
        stepStatsTableCount.ShouldBeGreaterThan(0);
        metricsTableCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Global_Information_Step_Should_Have_Scenario_SortIndex_And_Steps_Should_Be_Indexed_Within_Scenario()
    {
        await fixture.TestHelper.RecreateDatabase();

        var stats = NBomberRunner
            .RegisterScenarios(
                CreateUserFlowScenario("user_flow_scenario_1"),
                CreateUserFlowScenario("user_flow_scenario_2"))
            .WithReportingSinks(fixture.CreateTimescaleDbSinkInstance())
            .WithoutReports()
            .Run();

        var stepStats = await fixture.TestHelper.GetStepStats(stats.TestInfo.SessionId);

        stats.ScenarioStats.Length.ShouldBe(2);

        var scenario1Index = SortIndexOf(stepStats, "user_flow_scenario_1", GlobalInformationStep);
        var scenario2Index = SortIndexOf(stepStats, "user_flow_scenario_2", GlobalInformationStep);

        scenario1Index.ShouldBe(0);
        scenario2Index.ShouldBe(1);

        foreach (var scnStats in stats.ScenarioStats)
        {
            SortIndexOf(stepStats, scnStats.ScenarioName, "login").ShouldBe(1);
            SortIndexOf(stepStats, scnStats.ScenarioName, "get_product").ShouldBe(2);
            SortIndexOf(stepStats, scnStats.ScenarioName, "buy_product").ShouldBe(3);

            SortIndexOf(stepStats, scnStats.ScenarioName, GlobalInformationStep)
                .ShouldBe(scnStats.SortIndex);
        }
    }

    private static ScenarioProps CreateUserFlowScenario(string scenarioName)
    {
        return Scenario.Create(scenarioName, async context =>
        {
            await Step.Run("login", context, () => Task.FromResult(Response.Ok(sizeBytes: 10, statusCode: "200")));
            await Step.Run("get_product", context, () => Task.FromResult(Response.Ok(sizeBytes: 20, statusCode: "200")));
            await Step.Run("buy_product", context, () => Task.FromResult(Response.Ok(sizeBytes: 30, statusCode: "200")));

            return Response.Ok(statusCode: "201");
        })
        .WithoutWarmUp()
        .WithLoadSimulations(Simulation.KeepConstant(1, during: TimeSpan.FromSeconds(2)));
    }

    private static int SortIndexOf(StepStatsDbRecord[] stepStats, string scenario, string step)
    {
        return stepStats
            .Where(x => x.Scenario == scenario && x.Step == step)
            .Select(x => x.SortIndex)
            .Distinct()
            .ShouldHaveSingleItem($"'{step}' of '{scenario}' should be recorded with exactly one sort index");
    }
}
