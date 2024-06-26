#pragma warning disable CS1591

using System.Text.Json;
using FSharp.Json;
using Microsoft.Extensions.Configuration;
using Npgsql;
using RepoDb;
using ILogger = Serilog.ILogger;
using NBomber.Contracts;
using NBomber.Contracts.Stats;
using NBomber.Sinks.Timescale.Contracts;

namespace NBomber.Sinks.Timescale;

public class TimescaleDbSinkConfig(string connectionString)
{
    public string ConnectionString { get; set; } = connectionString;
}

public class TimescaleDbSink : IReportingSink
{
    private ILogger _logger;
    private IBaseContext _context;
    private NpgsqlConnection _mainConnection;
    private string _connectionString = "";
    
    public string SinkName => "NBomber.Sinks.TimescaleDb";

    public TimescaleDbSink() { }

    public TimescaleDbSink(string connectionString)
    {
        _connectionString = connectionString;
        _mainConnection = new NpgsqlConnection(connectionString);
    }
    
    public async Task Init(IBaseContext context, IConfiguration infraConfig)
    {
        _logger = context.Logger.ForContext<TimescaleDbSink>();
        _context = context;

        var config = infraConfig?.GetSection("TimescaleDbSink").Get<TimescaleDbSinkConfig>();
        if (config != null)
        {
            _connectionString = config.ConnectionString;
            _mainConnection = new NpgsqlConnection(_connectionString);
        }
        
        if (_mainConnection == null)
        {
            _logger.Error(
                "Reporting Sink {0} has problems with initialization. The problem could be related to invalid config structure.",
                SinkName);

            throw new Exception(
                $"Reporting Sink {SinkName} has problems with initialization. The problem could be related to invalid config structure.");
        }
        
        GlobalConfiguration
            .Setup()
            .UsePostgreSql();
        
        await _mainConnection.OpenAsync();
        
        await _mainConnection.ExecuteNonQueryAsync(
            SqlQueries.CreateStepStatsTable
            + SqlQueries.CreateClusterStatsTable
        );
    }

    public async Task Start()
    {
        if (_mainConnection != null)
        {
            var nodeInfo = _context.GetNodeInfo();
            
            var record = new NodeInfoDbRecord
            {
                Time = DateTimeOffset.UtcNow,
                SessionId = _context.TestInfo.SessionId,
                NodeInfo = Json.serialize(nodeInfo)
            };
            
            await _mainConnection.BinaryBulkInsertAsync(SqlQueries.ClusterStatsTableName, new [] { record });
        }
    }

    public Task SaveRealtimeStats(ScenarioStats[] stats) => SaveScenarioStats(stats);

    public Task SaveFinalStats(NodeStats stats) => SaveScenarioStats(stats.ScenarioStats);

    public Task Stop() => Task.CompletedTask;

    public void Dispose()
    {
        _mainConnection.Close();
        _mainConnection.Dispose();
    }

    private async Task SaveScenarioStats(ScenarioStats[] stats)
    {
        if (_mainConnection != null)
        {
            var currentTime = DateTimeOffset.UtcNow;
            
            var points = stats.Select(AddGlobalInfoStep)
                .SelectMany(step => MapToPoint(step, currentTime))
                .ToArray();
            
            await _mainConnection.BinaryBulkInsertAsync(SqlQueries.StepStatsTable, points);
        }
    }

    private ScenarioStats AddGlobalInfoStep(ScenarioStats scnStats)
    {
        var globalStepInfo = new StepStats("global information", scnStats.Ok, scnStats.Fail);
        scnStats.StepStats = scnStats.StepStats.Append(globalStepInfo).ToArray();

        return scnStats;
    }

    private PointDbRecord[] MapToPoint(ScenarioStats scnStats, DateTimeOffset currentTime)
    {
        var nodeInfo = _context.GetNodeInfo();
        var testInfo = _context.TestInfo;
        
        return scnStats.StepStats
            .Select(step =>
            {
                // clear status code message for Bombing
                if (nodeInfo.CurrentOperation != OperationType.Complete)
                {
                    foreach (var status in step.Ok.StatusCodes)
                        status.Message = "";
                    
                    foreach (var status in step.Fail.StatusCodes)
                        status.Message = "";
                }
                return step;
            })
            .Select(step => new PointDbRecord
            {
                Time = currentTime,
                SessionId = testInfo.SessionId,
                CurrentOperation = nodeInfo.CurrentOperation,
                TestSuite = testInfo.TestSuite,
                TestName = testInfo.TestName,
                Scenario = scnStats.ScenarioName,
                Step = step.StepName,

                AllReqCount = step.Ok.Request.Count + step.Fail.Request.Count,
                AllDataAll = step.Ok.DataTransfer.AllBytes + step.Fail.DataTransfer.AllBytes,
                OkReqCount = step.Ok.Request.Count,
                OkReqRps = step.Ok.Request.RPS,
                OkLatencyMin = step.Ok.Latency.MinMs,
                OkLatencyMean = step.Ok.Latency.MeanMs,
                OkLatencyMax = step.Ok.Latency.MaxMs,
                OkLatencyStdDev = step.Ok.Latency.StdDev,
                OkLatencyP50 = step.Ok.Latency.Percent50,
                OkLatencyP75 = step.Ok.Latency.Percent75,
                OkLatencyP95 = step.Ok.Latency.Percent95,
                OkLatencyP99 = step.Ok.Latency.Percent99,
                OkDataMin = step.Ok.DataTransfer.MinBytes,
                OkDataMean = step.Ok.DataTransfer.MeanBytes,
                OkDataMax = step.Ok.DataTransfer.MaxBytes,
                OkDataAll = step.Ok.DataTransfer.AllBytes,
                OkDataP50 = step.Ok.DataTransfer.Percent50,
                OkDataP75 = step.Ok.DataTransfer.Percent75,
                OkDataP95 = step.Ok.DataTransfer.Percent95,
                OkDataP99 = step.Ok.DataTransfer.Percent99,
                OkStatusCodes = JsonSerializer.Serialize(step.Ok.StatusCodes),
                OkLatencyCount = JsonSerializer.Serialize(step.Ok.Latency.LatencyCount),

                FailReqCount = step.Fail.Request.Count,
                FailReqRps = step.Fail.Request.RPS,
                FailLatencyMin = step.Fail.Latency.MinMs,
                FailLatencyMean = step.Fail.Latency.MeanMs,
                FailLatencyMax = step.Fail.Latency.MaxMs,
                FailLatencyStdDev = step.Fail.Latency.StdDev,
                FailLatencyP50 = step.Fail.Latency.Percent50,
                FailLatencyP75 = step.Fail.Latency.Percent75,
                FailLatencyP95 = step.Fail.Latency.Percent95,
                FailLatencyP99 = step.Fail.Latency.Percent99,
                FailDataMin = step.Fail.DataTransfer.MinBytes,
                FailDataMean = step.Fail.DataTransfer.MeanBytes,
                FailDataMax = step.Fail.DataTransfer.MaxBytes,
                FailDataAll = step.Fail.DataTransfer.AllBytes,
                FailDataP50 = step.Fail.DataTransfer.Percent50,
                FailDataP75 = step.Fail.DataTransfer.Percent75,
                FailDataP95 = step.Fail.DataTransfer.Percent95,
                FailDataP99 = step.Fail.DataTransfer.Percent99,
                FailStatusCodes = JsonSerializer.Serialize(step.Fail.StatusCodes),
                FailLatencyCount = JsonSerializer.Serialize(step.Fail.Latency.LatencyCount),

                SimulationValue = scnStats.LoadSimulationStats.Value
            })
            .ToArray();
    }
}