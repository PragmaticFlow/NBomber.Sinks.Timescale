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
                OkStepStats = JsonSerializer.Serialize(step.Ok),
                FailStepStats = JsonSerializer.Serialize(step.Fail)
            })
            .ToArray();
    }
}