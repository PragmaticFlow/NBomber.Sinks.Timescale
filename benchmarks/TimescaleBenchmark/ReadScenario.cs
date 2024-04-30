﻿using AutoBogus;
using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Sinks.Timescale;
using NBomber.Sinks.Timescale.Contracts;
using Npgsql;
using RepoDb;

namespace TimescaleBenchmark;

public class ReadScenario
{
    public ScenarioProps Create(string connectionString, DateTimeOffset startTime, string sessionId)
    {
        return Scenario.Create("read_scenario", async ctx =>
        {
            var end = false;

            var latencyRes = new List<PointLatencyCounts>();
            var statusCodesRes = new List<PointStatusCodes>();
            var stepStatsRes = new List<PointStepStatsOk>();
            
            var endTimeLatencyCounts = startTime;
            var endTimeStatusCodes = startTime;
            var endTimeStepStats = startTime;
            
            while (!end)
            {
                using var connection1 = new NpgsqlConnection(connectionString);
                using var connection2 = new NpgsqlConnection(connectionString);
                using var connection3 = new NpgsqlConnection(connectionString);

                var st1 = endTimeLatencyCounts - TimeSpan.FromMinutes(10);
                
                var dataLatencyCounts = connection1.QueryAsync<PointLatencyCounts>(SqlQueries.LatencyCountsTableName,
                    p => p.SessionId == sessionId
                         && p.Time <= endTimeLatencyCounts
                         && p.Time >= st1);

                var st2 = endTimeStatusCodes - TimeSpan.FromSeconds(10);
                
                var dataStatusCodes = connection2.QueryAsync<PointStatusCodes>(SqlQueries.StatusCodesTableName, 
                    p => p.SessionId == sessionId
                    && p. Time <= endTimeStatusCodes
                    && p. Time >= st2);

                var st3 = endTimeStepStats - TimeSpan.FromSeconds(10);
                
                var dataStepStats = connection3.QueryAsync<PointStepStatsOk>(SqlQueries.CreateStepStatsOkTable,
                    p => p.SessionId == sessionId 
                    && p.Time <= endTimeStepStats
                    && p.Time >= st3);

                await Task.WhenAll(dataLatencyCounts, dataStatusCodes, dataStepStats);

                var dataLatencyCountsArr = dataLatencyCounts.Result.ToArray();
                var dataStatusCodesArr = dataStatusCodes.Result.ToArray();
                var dataStepStatsArr = dataStepStats.Result.ToArray();

                end = true;

                latencyRes.AddRange(dataLatencyCountsArr);
                statusCodesRes.AddRange(dataStatusCodesArr);
                stepStatsRes.AddRange(dataStepStatsArr);

                if (dataLatencyCountsArr.Length >= 120)
                {
                    endTimeLatencyCounts = dataLatencyCountsArr[^1].Time - TimeSpan.FromSeconds(1);
                    end = false;
                }

                if (dataStatusCodesArr.Length >= 120)
                {
                    endTimeStatusCodes = dataStatusCodesArr[^1].Time - TimeSpan.FromSeconds(1);
                    end = false;
                }

                if (dataStepStatsArr.Length >= 120)
                {
                    endTimeStepStats = dataStepStatsArr[^1].Time - TimeSpan.FromSeconds(1);
                    end = false;
                }
            }
            
            return Response.Ok();
        })
        .WithoutWarmUp()
        .WithLoadSimulations(Simulation.KeepConstant(1, TimeSpan.FromSeconds(30)))
        .WithInit(async ctx =>
        {
            PointLatencyCounts fakePointLatencyCounts = new();
            PointStatusCodes fakePointStatusCodes = new();
            PointStepStatsOk fakePointStepStats = new();
            
            var faker = AutoFaker.Create();
            
            fakePointLatencyCounts = faker.Generate<PointLatencyCounts>();
            fakePointStatusCodes = faker.Generate<PointStatusCodes>();
            fakePointStepStats = faker.Generate<PointStepStatsOk>();

            var fakeLatencyCountsPoints = Enumerable
                .Range(0, 2160)
                .Select(i => new PointLatencyCounts
                {
                    Time = startTime.AddSeconds(-5 * i),
                    SessionId = sessionId,
                    CurrentOperation = fakePointLatencyCounts.CurrentOperation,
                    NodeType = fakePointLatencyCounts.NodeType,
                    TestSuite = fakePointLatencyCounts.TestSuite,
                    TestName = fakePointLatencyCounts.TestName,
                    ClusterId = fakePointLatencyCounts.ClusterId,
                    Scenario = fakePointLatencyCounts.Scenario,
                    LessOrEq800 = fakePointLatencyCounts.LessOrEq800,
                    More800Less1200 = fakePointLatencyCounts.More800Less1200,
                    MoreOrEq1200 = fakePointLatencyCounts.MoreOrEq1200
                })
                .ToArray();
            
            var fakeStatusCodesPoints = Enumerable
                .Range(0, 2160)
                .Select(i => new PointStatusCodes
                {
                    Time = startTime.AddSeconds(-5 * i),
                    SessionId = sessionId,
                    CurrentOperation = fakePointStatusCodes.CurrentOperation,
                    NodeType = fakePointStatusCodes.NodeType,
                    TestSuite = fakePointStatusCodes.TestSuite,
                    TestName = fakePointStatusCodes.TestName,
                    ClusterId = fakePointStatusCodes.ClusterId,
                    Scenario = fakePointStatusCodes.Scenario,
                    StatusCode = fakePointStatusCodes.StatusCode,
                    Count = fakePointStatusCodes.Count
                })
                .ToArray();
            
            var fakeStepStatsPoints = Enumerable
                .Range(0, 2160)
                .Select(i => new PointStepStatsOk
                { 
                    Time = startTime.AddSeconds(-5 * i),
                    SessionId = sessionId,
                    CurrentOperation = fakePointStepStats.CurrentOperation,
                    NodeType = fakePointStepStats.NodeType,
                    TestSuite = fakePointStepStats.TestSuite,
                    TestName = fakePointStepStats.TestName,
                    ClusterId = fakePointStepStats.ClusterId,
                    Scenario = fakePointStepStats.Scenario,
                    Step = fakePointStepStats.Step,
                    AllReqCount = fakePointStepStats.AllReqCount,
                    AllDataAll = fakePointStepStats.AllDataAll,
                    OkReqCount = fakePointStepStats.OkReqCount,
                    OkReqRps = fakePointStepStats.OkReqRps,
                    OkLatencyMax = fakePointStepStats.OkLatencyMax,
                    OkLatencyMean = fakePointStepStats.OkLatencyMean,
                    OkLatencyMin = fakePointStepStats.OkLatencyMin,
                    OkLatencyStdDev = fakePointStepStats.OkLatencyStdDev,
                    OkLatencyP50 = fakePointStepStats.OkLatencyP50,
                    OkLatencyP75 = fakePointStepStats.OkLatencyP75,
                    OkLatencyP95 = fakePointStepStats.OkLatencyP95,
                    OkLatencyP99 = fakePointStepStats.OkLatencyP99,
                    OkDataMin = fakePointStepStats.OkDataMin,
                    OkDataMean = fakePointStepStats.OkDataMean,
                    OkDataMax = fakePointStepStats.OkDataMax,
                    OkDataAll = fakePointStepStats.OkDataAll,
                    OkDataP50 = fakePointStepStats.OkDataP50,
                    OkDataP75 = fakePointStepStats.OkDataP75,
                    OkDataP95 = fakePointStepStats.OkDataP95,
                    OkDataP99 = fakePointStepStats.OkDataP99,
                    FailReqCount = fakePointStepStats.FailReqCount,
                    FailReqRps = fakePointStepStats.FailReqRps,
                    // FailLatencyMax = fakePointStepStats.FailLatencyMax,
                    // FailLatencyMean = fakePointStepStats.FailLatencyMean,
                    // FailLatencyMin = fakePointStepStats.FailLatencyMin,
                    // FailLatencyStdDev = fakePointStepStats.FailLatencyStdDev,
                    // FailLatencyP50 = fakePointStepStats.FailLatencyP50,
                    // FailLatencyP75 = fakePointStepStats.FailLatencyP75,
                    // FailLatencyP95 = fakePointStepStats.FailLatencyP95,
                    // FailLatencyP99 = fakePointStepStats.FailLatencyP99,
                    // FailDataMin = fakePointStepStats.FailDataMin,
                    // FailDataMean = fakePointStepStats.FailDataMean,
                    // FailDataMax = fakePointStepStats.FailDataMax,
                    // FailDataAll = fakePointStepStats.FailDataAll,
                    // FailDataP50 = fakePointStepStats.FailDataP50,
                    // FailDataP75 = fakePointStepStats.FailDataP75,
                    // FailDataP95 = fakePointStepStats.FailDataP95,
                    // FailDataP99 = fakePointStepStats.FailDataP99,
                    SimulationValue = fakePointStepStats.SimulationValue
                })
                .ToArray();
            
            using var connection1 = new NpgsqlConnection(connectionString);
            using var connection2 = new NpgsqlConnection(connectionString);
            using var connection3 = new NpgsqlConnection(connectionString);
            
            var insert1 = connection1.BinaryBulkInsertAsync(SqlQueries.StatusCodesTableName, fakeStatusCodesPoints);
            var insert2 = connection2.BinaryBulkInsertAsync(SqlQueries.LatencyCountsTableName, fakeLatencyCountsPoints);
            var insert3 = connection3.BinaryBulkInsertAsync(SqlQueries.CreateStepStatsOkTable, fakeStepStatsPoints);

            await Task.WhenAll(insert1, insert2, insert3);
        });
    }
}