using Nbomber.Sinks.Timescale.Tests.Infra;
using NBomber.Sinks.Timescale;
using NBomber.CSharp;

namespace Nbomber.Sinks.Timescale.Tests
{
    public class TimescaleTest(EnvContextFixture fixture) : IClassFixture<EnvContextFixture>
    {
        [Fact]
        public async Task When_DBSchemaVersion_Higher_SinkSchemaVersion_Throw_PlatformNotSupportedException()
        {
            var sinkSchemaVersion = DbMigrations.SinkSchemaVersion;

            await fixture.TestHelper.DeleteTables();
            await fixture.TestHelper.CreateTables();
            await fixture.TestHelper.SetDbSchemaVersion(sinkSchemaVersion + 1); // with set higher version

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

            Assert.Throws<Exception>(() =>
            {
                NBomberRunner
                    .RegisterScenarios(scenario)
                    .WithReportingSinks(fixture.CreateTimescaleDbSinkInstance())
                    .Run();
            });
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
            .WithLoadSimulations(Simulation.KeepConstant(1, during: TimeSpan.FromSeconds(10)));

            NBomberRunner
                .RegisterScenarios(scenario)
                .WithReportingSinks(fixture.CreateTimescaleDbSinkInstance())
                .Run();

            var dbSchemaVersion = await fixture.TestHelper.GetDBSchemaVersion();

            Assert.Equal(DbMigrations.SinkSchemaVersion, dbSchemaVersion);
        }

        [Fact]
        public async Task When_Scenario_Completed_DataBase_IsFull()
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

            NBomberRunner
                .RegisterScenarios(scenario)
                .WithReportingSinks(fixture.CreateTimescaleDbSinkInstance())
                .Run();

            var sessionTableCount = await fixture.TestHelper.GetDataCount(SqlQueries.SessionsTable);
            var stepStatsTableCount = await fixture.TestHelper.GetDataCount(SqlQueries.StepStatsTable);

            Assert.True(sessionTableCount > 0);
            Assert.True(stepStatsTableCount > 0);
        }
    }
} 