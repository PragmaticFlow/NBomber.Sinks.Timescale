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
    private bool _dbError = false;
    
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
        
        var migration = new DbMigrations(_config.ConnectionString, _logger);
        await migration.Run();  
    }

    public async Task Start(SessionStartInfo sessionInfo)
    {
        if (_mainConnection != null)
        {
            var nodeInfo = _context.GetNodeInfo();
            var testInfo = _context.TestInfo;

            var record = new NodeInfoDbRecord
            {
                Time = DateTime.UtcNow,
                SessionId = testInfo.SessionId,
                CurrentOperation = OperationType.Bombing,
                TestSuite = testInfo.TestSuite,
                TestName = testInfo.TestName,
                Metadata = Json.serialize(sessionInfo),
                NodeInfo = Json.serialize(nodeInfo)
            };

            var text = @$"INSERT INTO {TableNames.SessionsTable} 
                        (""{ColumnNames.Time}"",
                        ""{ColumnNames.SessionId}"",
                        ""{ColumnNames.CurrentOperation}"",
                        ""{ColumnNames.TestSuite}"",
                        ""{ColumnNames.TestName}"",
                        ""{ColumnNames.Metadata}"",
                        ""{ColumnNames.NodeInfo}"")
                        VALUES 
                        ('{record.Time}',
                        '{record.SessionId}',
                        '{record.CurrentOperation}',
                        '{record.TestSuite}',
                        '{record.TestName}',
                        '{record.Metadata}'::jsonb,
                        '{record.NodeInfo}'::jsonb)";
            try
            {
                await _mainConnection.ExecuteNonQueryAsync(text);
            }
            catch (Exception ex) 
            {
                _dbError = true;
                _logger.Error(ex, ex.Message);
                throw;
            }
        }
    }

    public Task SaveRealtimeStats(ScenarioStats[] stats) => SaveScenarioStats(stats);

    public Task SaveFinalStats(NodeStats stats) => SaveScenarioStats(stats.ScenarioStats, isFinal: true);

    public Task Stop() => Task.CompletedTask;

    public void Dispose()
    {
        _mainConnection.Close();
        _mainConnection.Dispose();
    }

    private async Task SaveScenarioStats(ScenarioStats[] stats, bool isFinal = false)
    {
        if (_mainConnection != null && !_dbError)
        {
            var currentTime = DateTime.UtcNow;
            
            var points = stats.Select(AddGlobalInfoStep)
                .SelectMany(step => MapToPoint(step, currentTime))
                .ToArray();
            
            if (!isFinal)
            {
                await _mainConnection.BinaryBulkInsertAsync(TableNames.StepStatsTable, points);
            }
            else
            {
                using (var transaction = _mainConnection.EnsureOpen().BeginTransaction())
                {
                    await _mainConnection.BinaryBulkInsertAsync(TableNames.StepStatsTable, points, transaction: (NpgsqlTransaction) transaction);

                    var testInfo = _context.TestInfo;

                    var queryEntity = new NodeInfoDbRecord
                    {
                        SessionId = testInfo.SessionId,
                        CurrentOperation = OperationType.Complete,
                    };

                    var fields = Field.Parse<NodeInfoDbRecord>(e => e.CurrentOperation);
               
                    await _mainConnection.UpdateAsync(TableNames.SessionsTable, queryEntity, fields: fields, transaction: transaction);
                
                    transaction.Commit();
                }
            }
        }
    }

    private ScenarioStats AddGlobalInfoStep(ScenarioStats scnStats)
    {
        var globalStepInfo = new StepStats("global information", scnStats.Ok, scnStats.Fail, 0);
        scnStats.StepStats = scnStats.StepStats.Append(globalStepInfo).ToArray();

        return scnStats;
    }

    private PointDbRecord[] MapToPoint(ScenarioStats scnStats, DateTime currentTime)
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
                ScenarioTimestamp = scnStats.Duration,
                SessionId = testInfo.SessionId,
                CurrentOperation = nodeInfo.CurrentOperation,
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