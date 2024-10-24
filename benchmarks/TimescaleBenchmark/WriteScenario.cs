﻿using System.Text.Json;
using AutoBogus;
using Dapper;
using Npgsql;
using RepoDb;
using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Sinks.Timescale.Contracts;
using NBomber.Sinks.Timescale.DAL;

namespace TimescaleBenchmark;

public class WriteScenario
{
    public ScenarioProps Create(string connectionString)
    {
        PointDbRecord fakePoint = new();
        
        return Scenario.Create("write_scenario", async ctx =>
        {
            var step = await Step.Run("write", ctx, async () =>
            {
                await using var connection = new NpgsqlConnection(connectionString);
            
                var curTime = DateTime.UtcNow; 
            
                fakePoint.Time = curTime;
                fakePoint.ScenarioTimestamp = TimeSpan.Zero;
                fakePoint.SessionId = ctx.ScenarioInfo.InstanceNumber.ToString();
                fakePoint.CurrentOperation = NBomber.Contracts.Stats.OperationType.Bombing;
                try
                {
                    await connection.BinaryBulkInsertAsync(TableNames.StepStatsTable, Enumerable.Repeat(fakePoint, 5));
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
            
            fakePoint = faker.Generate<PointDbRecord>();
            
            fakePoint.OkLatencyCount = JsonSerializer.Serialize(fakePoint.OkLatencyCount);
            fakePoint.OkStatusCodes = JsonSerializer.Serialize(fakePoint.OkStatusCodes);
            fakePoint.FailLatencyCount = JsonSerializer.Serialize(fakePoint.FailLatencyCount);
            fakePoint.FailStatusCodes = JsonSerializer.Serialize(fakePoint.FailStatusCodes);
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(3))
        .WithLoadSimulations(
            Simulation.RampingConstant(700, TimeSpan.FromSeconds(30)),
            Simulation.KeepConstant(700, TimeSpan.FromMinutes(1))
        );
    }
}