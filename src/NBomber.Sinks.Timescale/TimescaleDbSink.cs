#pragma warning disable CS1591

using System.Text.Json;
using FSharp.Json;
using Microsoft.Extensions.Configuration;
using Npgsql;
using RepoDb;
using ILogger = Serilog.ILogger;
using NBomber.Contracts;
using NBomber.Contracts.Metrics;
using NBomber.Contracts.Stats;
using NBomber.Sinks.Timescale.Contracts;
using NBomber.Sinks.Timescale.DAL;

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
    private TimescaleDbSinkConfig _config = new("");
    
    public string SinkName => "NBomber.Sinks.TimescaleDb";

    public TimescaleDbSink() { }

    public TimescaleDbSink(TimescaleDbSinkConfig config)
    {
        _config = config;
        _mainConnection = new NpgsqlConnection(_config.ConnectionString);
    }
    
    public async Task Init(IBaseContext context, IConfiguration infraConfig)
    {
        _logger = context.Logger.ForContext<TimescaleDbSink>();
        _context = context;

        var config = infraConfig?.GetSection("TimescaleDbSink").Get<TimescaleDbSinkConfig>();
        if (config != null)
        {
            _config = config;
            _mainConnection = new NpgsqlConnection(_config.ConnectionString);
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
        
        var migration = new DbMigrations(_mainConnection, _logger);
        await migration.Run();  
    }

    public async Task Start(SessionStartInfo sessionInfo)
    {
        if (_mainConnection != null)
        {
            var nodeInfo = _context.GetNodeInfo();
            var testInfo = _context.TestInfo;
            var startTime = DateTime.UtcNow;

            if (!nodeInfo.NodeType.IsAgent)
            {
                var record = new SessionInfoDbRecord
                {
                    Time = startTime,
                    LastUpdatedTime = startTime,
                    SessionId = testInfo.SessionId,
                    CurrentOperation = OperationType.Bombing,
                    TestSuite = testInfo.TestSuite,
                    TestName = testInfo.TestName,
                    Metadata = Json.serialize(sessionInfo),
                    NodeInfo = Json.serialize(nodeInfo)
                };

                try
                {
                    var res = await _mainConnection.InsertAsync(tableName: TableNames.SessionsTable, record);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, ex.Message);
                    throw;
                }
            }
        }
    }

    public async Task SaveRealtimeStats(ScenarioStats[] stats)
    {
        var currentTime = DateTime.UtcNow;
            
        var points = stats.Select(AddGlobalInfoStep)
            .SelectMany(step => MapToPoint(step, currentTime, OperationType.Bombing))
            .ToArray();
            
        await _mainConnection.BinaryBulkInsertAsync(TableNames.StepStatsTable, points);
    }

    public async Task SaveRealtimeMetrics(MetricStats metrics)
    {
        var points = MapMetrics(metrics, DateTime.UtcNow, OperationType.Bombing);
        await _mainConnection.BinaryBulkInsertAsync(TableNames.MetricsTable, points);
    }

    public async Task SaveFinalStats(NodeStats stats)
    {
        var currentTime = DateTime.UtcNow;
        var operation = OperationType.Complete;
        var testInfo = _context.TestInfo;
            
        var metricsPoints = MapMetrics(stats.Metrics, currentTime, operation);
            
        var statsPoints = stats.ScenarioStats.Select(AddGlobalInfoStep)
            .SelectMany(step => MapToPoint(step, currentTime, operation))
            .ToArray();

        var queryEntity = new SessionInfoDbRecord
        {
            SessionId = testInfo.SessionId,
            CurrentOperation = operation,
            LastUpdatedTime = currentTime,
        };

        using var transaction = _mainConnection.EnsureOpen().BeginTransaction();
            
        await _mainConnection.BinaryBulkInsertAsync(TableNames.StepStatsTable, statsPoints, transaction: (NpgsqlTransaction) transaction);
        await _mainConnection.BinaryBulkInsertAsync(TableNames.MetricsTable, metricsPoints, transaction: (NpgsqlTransaction) transaction);

        var fields = Field.Parse<SessionInfoDbRecord>(e => new { e.CurrentOperation, e.LastUpdatedTime });
        await _mainConnection.UpdateAsync(TableNames.SessionsTable, queryEntity, fields: fields, transaction: transaction);
                
        transaction.Commit();
    }

    public Task Stop() => Task.CompletedTask;

    public void Dispose()
    {
        _mainConnection?.Close();
        _mainConnection?.Dispose();
    }

    private ScenarioStats AddGlobalInfoStep(ScenarioStats scnStats)
    {
        var globalStepInfo = new StepStats("global information", scnStats.Ok, scnStats.Fail, 0);
        scnStats.StepStats = scnStats.StepStats.Append(globalStepInfo).ToArray();

        return scnStats;
    }

    private MetricDbRecord[] MapMetrics(MetricStats stats, DateTime currentTime, OperationType operationType)
    {
        var testInfo = _context.TestInfo;
        
        var counters = stats.Counters.Select(x => new MetricDbRecord
        {
            Time = currentTime,
            ScenarioTimestamp = stats.Duration,
            SessionId = testInfo.SessionId,
            CurrentOperation = operationType,
            Scenario = x.ScenarioName,
            Metric = x.MetricName,
            MetricType = "counter",
            UnitOfMeasure = x.UnitOfMeasure,
            Value = x.Value
        });
        
        var gauges = stats.Gauges.Select(x => new MetricDbRecord
        {
            Time = currentTime,
            ScenarioTimestamp = stats.Duration,
            SessionId = testInfo.SessionId,
            CurrentOperation = operationType,
            Scenario = x.ScenarioName,
            Metric = x.MetricName,
            MetricType = "gauge",
            UnitOfMeasure = x.UnitOfMeasure,
            Value = x.Value
        });
        
        return counters.Concat(gauges).ToArray();
    }
    
    private PointDbRecord[] MapToPoint(ScenarioStats scnStats, DateTime currentTime, OperationType currentOperation)
    {
        var testInfo = _context.TestInfo;
        
        return scnStats.StepStats
            .Select(step =>
            {
                // clear status code message for Bombing
                if (currentOperation != OperationType.Complete)
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
                ScenarioTimestamp = scnStats.Duration,
                SessionId = testInfo.SessionId,
                CurrentOperation = currentOperation,
                Scenario = scnStats.ScenarioName,
                Step = step.StepName,
                SortIndex = step.SortIndex,

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