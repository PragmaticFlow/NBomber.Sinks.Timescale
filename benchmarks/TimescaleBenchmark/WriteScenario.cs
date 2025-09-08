using System.Text.Json;
using AutoBogus;
using Dapper;
using Npgsql;
using RepoDb;
using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Sinks.Timescale.Contracts;
using NBomber.Sinks.Timescale.DAL;
using NBomber.Sinks.Timescale.Domain;

namespace TimescaleBenchmark;

public class WriteScenario
{
    public ScenarioProps Create(string connectionString)
    {
        StepStatsDbRecord fakeStepPoint = new();
        
        return Scenario.Create("write_scenario", async ctx =>
        {
            var step = await Step.Run("write", ctx, async () =>
            {
                await using var connection = new NpgsqlConnection(connectionString);
            
                var curTime = DateTime.UtcNow; 
            
                fakeStepPoint.Time = curTime;
                fakeStepPoint.ScenarioTimestamp = TimeSpan.Zero;
                fakeStepPoint.SessionId = ctx.ScenarioInfo.InstanceNumber.ToString();
                
                fakeStepPoint.CurrentOperation = TimescaleOperationType.Bombing;
                try
                {
                    await connection.BinaryBulkInsertAsync(TableNames.StepStatsTable, Enumerable.Repeat(fakeStepPoint, 5));
                }
                catch
                {
                    // ignored
                }

                return Response.Ok();
            });

            await Task.Delay(TimeSpan.FromSeconds(5));
            
            return Response.Ok();
        })
        .WithInit(async ctx =>
        {
            await using var connection = new NpgsqlConnection(connectionString);
            
            await connection.ExecuteAsync(SqlQueries.CreateStepStatsTable);
            
            var faker = AutoFaker.Create();
            
            fakeStepPoint = faker.Generate<StepStatsDbRecord>();
            
            fakeStepPoint.OkLatencyCount = JsonSerializer.Serialize(fakeStepPoint.OkLatencyCount);
            fakeStepPoint.OkStatusCodes = JsonSerializer.Serialize(fakeStepPoint.OkStatusCodes);
            fakeStepPoint.FailLatencyCount = JsonSerializer.Serialize(fakeStepPoint.FailLatencyCount);
            fakeStepPoint.FailStatusCodes = JsonSerializer.Serialize(fakeStepPoint.FailStatusCodes);
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(3))
        .WithLoadSimulations(
            Simulation.RampingConstant(700, TimeSpan.FromSeconds(30)),
            Simulation.KeepConstant(700, TimeSpan.FromMinutes(1))
        );
    }
}