using System.Text.Json;
using AutoBogus;
using Npgsql;
using RepoDb;
using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Sinks.Timescale.Contracts;
using NBomber.Sinks.Timescale.DAL;

namespace TimescaleBenchmark;

public class ReadScenario
{
    public ScenarioProps Create(string connectionString, DateTime startTime, string sessionId)
    {
        return Scenario.Create("read_scenario", async ctx =>
        {
            var end = false;
            var result = new List<PointDbRecord>();
            var endTime = startTime;
            
            while (!end)
            {
                await using var connection = new NpgsqlConnection(connectionString);

                var st = endTime - TimeSpan.FromMinutes(10);
                
                var dataStepStats = connection.QueryAsync<PointDbRecord>(TableNames.StepStatsTable,
                    p => p.SessionId == sessionId 
                    && p.Time <= endTime
                    && p.Time >= st);
                
                var dataArray = (await dataStepStats).ToArray();

                end = true;
                
                result.AddRange(dataArray);

                if (dataArray.Length >= 120)
                {
                    endTime = dataArray[^1].Time - TimeSpan.FromSeconds(1);
                    end = false;
                }
            }
            
            return Response.Ok();
        })
        .WithoutWarmUp()
        .WithLoadSimulations(Simulation.KeepConstant(1, TimeSpan.FromSeconds(30)))
        .WithInit(async ctx =>
        {
            var faker = AutoFaker.Create();
            var fakePoint = faker.Generate<PointDbRecord>();
            var rnd = new Random();
            
            var fakePoints = Enumerable
                .Range(0, 2160)
                .Select(i => new PointDbRecord
                { 
                    Time = startTime.AddSeconds(-5 * i),
                    ScenarioTimestamp = TimeSpan.Zero,
                    Step = fakePoint.Step,
                    Scenario = fakePoint.Scenario,
                    
                    AllReqCount = rnd.Next(100, 300),
                    AllDataAll = fakePoint.AllDataAll,
                    
                    OkReqCount = fakePoint.OkReqCount,
                    OkReqRps = fakePoint.OkReqRps,
                    FailReqCount = rnd.Next(0, 300) * rnd.Next(0, 2) * rnd.Next(0, 2),
                    FailReqRps = fakePoint.FailReqRps,
                    
                    OkLatencyMax = fakePoint.OkLatencyMax,
                    OkLatencyMean = fakePoint.OkLatencyMean,
                    OkLatencyMin = fakePoint.OkLatencyMin,
                    OkLatencyStdDev = fakePoint.OkLatencyStdDev,
                    OkLatencyP50 = Math.Round(200 + rnd.NextDouble() * 230, 2),
                    OkLatencyP75 = Math.Round(210 + rnd.NextDouble() * 240, 2),
                    OkLatencyP95 = Math.Round(220 + rnd.NextDouble() * 250, 2),
                    OkLatencyP99 = Math.Round(230 + rnd.NextDouble() * 260, 2),
                    
                    FailLatencyMax = fakePoint.OkLatencyMax,
                    FailLatencyMean = fakePoint.OkLatencyMean,
                    FailLatencyMin = fakePoint.OkLatencyMin,
                    FailLatencyStdDev = fakePoint.OkLatencyStdDev,
                    FailLatencyP50 = Math.Round(200 + rnd.NextDouble() * 230, 2),
                    FailLatencyP75 = Math.Round(210 + rnd.NextDouble() * 240, 2),
                    FailLatencyP95 = Math.Round(220 + rnd.NextDouble() * 250, 2),
                    FailLatencyP99 = Math.Round(230 + rnd.NextDouble() * 260, 2),
                    
                    OkDataMin = fakePoint.OkDataMin,
                    OkDataMean = fakePoint.OkDataMean,
                    OkDataMax = fakePoint.OkDataMax,
                    OkDataAll = fakePoint.OkDataAll,
                    OkDataP50 = fakePoint.OkDataP50,
                    OkDataP75 = fakePoint.OkDataP75,
                    OkDataP95 = fakePoint.OkDataP95,
                    OkDataP99 = fakePoint.OkDataP99,
                    
                    FailDataMin = fakePoint.OkDataMin,
                    FailDataMean = fakePoint.OkDataMean,
                    FailDataMax = fakePoint.OkDataMax,
                    FailDataAll = fakePoint.OkDataAll,
                    FailDataP50 = fakePoint.OkDataP50,
                    FailDataP75 = fakePoint.OkDataP75,
                    FailDataP95 = fakePoint.OkDataP95,
                    FailDataP99 = fakePoint.OkDataP99,
                    
                    SimulationValue = fakePoint.SimulationValue,
                    SessionId = sessionId,
                    CurrentOperation = fakePoint.CurrentOperation,
                    
                    OkStatusCodes = JsonSerializer.Serialize(fakePoint.OkStatusCodes),
                    OkLatencyCount = JsonSerializer.Serialize(fakePoint.OkLatencyCount),
                    FailStatusCodes = JsonSerializer.Serialize(fakePoint.FailStatusCodes),
                    FailLatencyCount = JsonSerializer.Serialize(fakePoint.FailLatencyCount)
                })
                .ToArray();
            
            await using var connection = new NpgsqlConnection(connectionString);
            
            var insert = connection.BinaryBulkInsertAsync(TableNames.StepStatsTable, fakePoints);

            await insert;
        });
    }
}