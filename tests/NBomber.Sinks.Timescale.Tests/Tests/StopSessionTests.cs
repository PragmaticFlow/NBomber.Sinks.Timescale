using NBomber.Contracts.Stats;
using NBomber.CSharp;
using NBomber.Sinks.Timescale.Tests.Infra;
using Shouldly;

namespace NBomber.Sinks.Timescale.Tests.Tests;

public class StopSessionTests(EnvContextFixture fixture) : IClassFixture<EnvContextFixture>
{
    [Fact]
    public async Task Should_Stop_Session_By_Db_Notification()
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
}
