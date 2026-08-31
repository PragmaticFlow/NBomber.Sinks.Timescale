using NBomber.Contracts;
using NBomber.Contracts.Stats;
using NBomber.CSharp;
using NBomber.Sinks.Timescale.Contracts;
using NBomber.Sinks.Timescale.DAL;
using NBomber.Sinks.Timescale.Tests.Infra;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.InMemory;
using Shouldly;

namespace NBomber.Sinks.Timescale.Tests;

public class TimescaleTest(EnvContextFixture fixture) : IClassFixture<EnvContextFixture>
{
    private const string GlobalInformationStep = "global information";

    [Fact]
    public async Task When_DBSchemaVersion_Higher_SinkSchemaVersion_Throw_PlatformNotSupportedException()
    {
        var sinkSchemaVersion = DbMigrations.SinkSchemaVersion;
        var logger = new InMemorySink();

        await fixture.TestHelper.DeleteTables();
        await fixture.TestHelper.CreateTables();
        await fixture.TestHelper.SetDbSchemaVersion(sinkSchemaVersion + 1); // with set higher version

        var logEvents = Array.Empty<LogEvent>();

        var scenario = Scenario.Create("user_flow_scenario", async context =>
        {
            if (context.InvocationNumber == 1)
                logEvents = logger.LogEvents.ToArray();

            var step1 = await Step.Run("step1", context, async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                return Response.Ok(sizeBytes: 10, statusCode: "200");
            });
            return Response.Ok(statusCode: "201", message: "hey");
        })
        .WithoutWarmUp()
        .WithLoadSimulations(Simulation.KeepConstant(1, during: TimeSpan.FromSeconds(1)));

        NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportingSinks(fixture.CreateTimescaleDbSinkInstance())
            .WithLoggerConfig(() => new LoggerConfiguration().WriteTo.Sink(logger))
            .Run();

        var containsSinkError = logEvents.Where(x => x.Level == LogEventLevel.Warning)
            .Any(x => x.RenderMessage().Contains("is older than schema version in your database"));

        Assert.True(containsSinkError);
    }

    [Fact]
    public async Task When_DataBase_Empty_Migrator_Should_Create_DB_WithSinkSchemaVersion()
    {
        await fixture.TestHelper.DeleteTables();

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

        NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportingSinks(fixture.CreateTimescaleDbSinkInstance())
            .Run();

        var dbSchemaVersion = await fixture.TestHelper.GetDBSchemaVersion();

        Assert.Equal(DbMigrations.SinkSchemaVersion, dbSchemaVersion);
    }

    [Fact]
    public async Task When_Scenario_Finished_The_DataBase_Should_Contain_Data()
    {
        await fixture.TestHelper.DeleteTables();

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
    public async Task Should_Stop_Session_By_Db_Notification()
    {
        await fixture.TestHelper.DeleteTables();

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
        .WithLoadSimulations(Simulation.KeepConstant(1, during: TimeSpan.FromSeconds(120)));

        var sessionId = Guid.CreateVersion7().ToString();

        var args = new string[] {
            $"--session-id={sessionId}"
        };

        var process = Task.Run(() =>
        {
            return NBomberRunner
                .RegisterScenarios(scenario)
                .WithReportingSinks(fixture.CreateTimescaleDbSinkInstance())
                .Run(args);
        });

        await Task.Delay(TimeSpan.FromSeconds(6));

        await fixture.TestHelper.NotifyStopSession(sessionId);

        var scenarioResult = await process;

        scenarioResult.Duration.ShouldBeLessThan(TimeSpan.FromSeconds(10));

        scenarioResult.NodeInfo.CurrentOperation.ShouldBe(OperationType.Stop);
    }

    [Fact]
    public async Task Should_StopEarly_WhenListenEnabled_AndStopCommandReceived()
    {
        await fixture.TestHelper.DeleteTables();

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
        .WithLoadSimulations(Simulation.KeepConstant(1, during: TimeSpan.FromSeconds(10)));

        var sessionId = Guid.CreateVersion7().ToString();

        var args = new string[] {
            $"--session-id={sessionId}"
        };

        var sink = fixture.CreateTimescaleDbSinkInstance(listenStopCommandEnabled: true);

        var process = Task.Run(() =>
        {
            return NBomberRunner
                .RegisterScenarios(scenario)
                .WithReportingSinks(sink)
                .Run(args);
        });

        await Task.Delay(TimeSpan.FromSeconds(6));

        await fixture.TestHelper.NotifyStopSession(sessionId);

        var result = await process;

        result.Duration.ShouldBeInRange(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(6));
    }

    [Fact]
    public async Task Should_NotProcessStopNotification_WhenListenDisabled()
    {
        await fixture.TestHelper.DeleteTables();

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
        .WithLoadSimulations(Simulation.KeepConstant(1, during: TimeSpan.FromSeconds(10)));

        var sessionId = Guid.CreateVersion7().ToString();

        var args = new string[] {
            $"--session-id={sessionId}"
        };

        var sink = fixture.CreateTimescaleDbSinkInstance(listenStopCommandEnabled: false);

        var process = Task.Run(() =>
        {
            return NBomberRunner
                .RegisterScenarios(scenario)
                .WithReportingSinks(sink)
                .Run(args);
        });

        await Task.Delay(TimeSpan.FromSeconds(6));

        await fixture.TestHelper.NotifyStopSession(sessionId);

        var result = await process;

        result.Duration.ShouldBeInRange(TimeSpan.FromSeconds(9), TimeSpan.FromSeconds(11));
    }

    [Fact]
    public async Task Cluster_Should_Have_Single_Postgres_Notification_Subscription()
    {
        await fixture.TestHelper.DeleteTables();

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
        .WithLoadSimulations(Simulation.KeepConstant(1, during: TimeSpan.FromSeconds(10)));

        var agentReportingSink = fixture.CreateTimescaleDbSinkInstance();
        var coordinatorReportingSink = fixture.CreateTimescaleDbSinkInstance();

        var agentProcess = Task.Run(() =>
        {
            return NBomberRunner.RegisterScenarios(scenario)
                .WithReportingSinks(agentReportingSink)
                .WithoutReports()
                .Run(["--cluster-agents-count=1", "--cluster-local-dev=true", "--cluster-nats-url=nats://localhost", "--cluster-id=default"]);
        });

        await Task.Delay(2000);

        var coordinatorProcess = Task.Run(() =>
        {
            return NBomberRunner.RegisterScenarios(scenario)
                .WithReportingSinks(coordinatorReportingSink)
                .Run(["--cluster-agents-count=1", "--cluster-local-dev=true", "--cluster-nats-url=nats://localhost", "--cluster-id=default"]);
        });

        bool coordinatorWasListening = false;
        bool agentWasListening = false;

        while (!coordinatorProcess.IsCompleted)
        {
            if (!coordinatorWasListening)
            {
                coordinatorWasListening = coordinatorReportingSink.StopCommandListening;
            }
            if (!agentWasListening)
            {
                agentWasListening = agentReportingSink.StopCommandListening;
            }

            await Task.Delay(400);
        }

        var result = await coordinatorProcess;

        coordinatorWasListening.ShouldBeTrue();
        agentWasListening.ShouldBeFalse();
    }

    [Fact]
    public async Task Global_Information_Step_Should_Have_Scenario_SortIndex_And_Steps_Should_Be_Indexed_Within_Scenario()
    {
        await fixture.TestHelper.DeleteTables();

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

    [Theory]
    [InlineData("1001", "1001")]              // valid project id is parsed
    [InlineData("", "-1")]                    // empty project id falls back to -1
    [InlineData(" ", "-1")]
    public void ParseProjectId_Should_Parse_Value(string projectId, string? expectedProjectId)
    {
        var sink = fixture.CreateTimescaleDbSinkInstance();

        sink.ParseProjectId(projectId).ShouldBe(expectedProjectId);
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