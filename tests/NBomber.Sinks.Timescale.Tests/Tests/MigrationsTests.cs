using NBomber.CSharp;
using NBomber.Sinks.Timescale.DAL;
using NBomber.Sinks.Timescale.Tests.Infra;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.InMemory;

namespace NBomber.Sinks.Timescale.Tests.Tests;

public class MigrationsTests(EnvContextFixture fixture) : IClassFixture<EnvContextFixture>
{
    [Fact]
    public async Task When_DBSchemaVersion_Higher_SinkSchemaVersion_Throw_PlatformNotSupportedException()
    {
        var sinkSchemaVersion = DbMigrations.SinkSchemaVersion;
        var logger = new InMemorySink();

        await fixture.TestHelper.RecreateDatabase();
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

        NBomberRunner
            .RegisterScenarios(scenario)
            .WithReportingSinks(fixture.CreateTimescaleDbSinkInstance())
            .Run();

        var dbSchemaVersion = await fixture.TestHelper.GetDBSchemaVersion();

        Assert.Equal(DbMigrations.SinkSchemaVersion, dbSchemaVersion);
    }
}
