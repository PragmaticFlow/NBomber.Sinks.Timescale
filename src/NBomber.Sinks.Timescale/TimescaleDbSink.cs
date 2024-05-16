#pragma warning disable CS1591

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
            SqlQueries.CreateClusterStatsTable
            + SqlQueries.CreateStatusCodesTable
            + SqlQueries.CreateLatencyCountsTable 
            + SqlQueries.CreateStepStatsOkTable
            + SqlQueries.CreateStepStatsFailTable
        );
    }

    public async Task Start()
    {
        if (_mainConnection != null)
        {
            var point = new PointClusterStats
            {
                Time = DateTime.UtcNow,
                NodeCount = 1,
                NodeCpuCount = _context.GetNodeInfo().CoresCount,
            };
            
            AddTestInfoTags(point);
           
            await _mainConnection.InsertAsync(SqlQueries.ClusterStatsTableName, point);
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
            await using var connection1 = new NpgsqlConnection(_connectionString);
            await using var connection2 = new NpgsqlConnection(_connectionString);
            await using var connection3 = new NpgsqlConnection(_connectionString);
            
            var currentTime = DateTimeOffset.UtcNow;

            var updatedStats = stats.Select(AddGlobalInfoStep).ToArray();
            var ok = updatedStats.SelectMany(stats => MapStepsStatsOk(stats, currentTime)).ToArray();
            var fail = updatedStats.SelectMany(stats => MapStepsStatsFail(stats, currentTime)).ToArray();
            
            var t1 = _mainConnection.BinaryBulkInsertAsync(SqlQueries.StepStatsOkTableName, ok);
            var t2 = connection1.BinaryBulkInsertAsync(SqlQueries.StepStatsFailTableName, fail);

            var latencyCounts = stats.Select(stats => MapLatencyCount(stats, currentTime)).ToArray();
            var t3 = connection2.BinaryBulkInsertAsync(SqlQueries.LatencyCountsTableName, latencyCounts);

            var statusCodes = stats.SelectMany(stats => MapStatusCodes(stats, currentTime)).ToArray();
            var t4 = connection3.BinaryBulkInsertAsync(SqlQueries.StatusCodesTableName, statusCodes);

            await Task.WhenAll(t1, t2, t3, t4);
        }
    }

    private ScenarioStats AddGlobalInfoStep(ScenarioStats scnStats)
    {
        var globalStepInfo = new StepStats("global information", scnStats.Ok, scnStats.Fail);
        scnStats.StepStats = scnStats.StepStats.Append(globalStepInfo).ToArray();

        return scnStats;
    }
    
    private void AddTestInfoTags(PointBase point)
    {
        var nodeInfo = _context.GetNodeInfo();
        var testInfo = _context.TestInfo;

        point.SessionId = testInfo.SessionId;
        point.CurrentOperation = nodeInfo.CurrentOperation.ToString().ToLower();
        point.NodeType = nodeInfo.NodeType.ToString();
        point.TestSuite = testInfo.TestSuite;
        point.TestName = testInfo.TestName;
        point.ClusterId = testInfo.ClusterId;
    }
    
    private IEnumerable<PointStepStatsOk> MapStepsStatsOk(ScenarioStats scnStats, DateTimeOffset currentTime)
    {
        var simulation = scnStats.LoadSimulationStats;

        return scnStats.StepStats.Select(step =>
        {
            var okR = step.Ok.Request;
            var okL = step.Ok.Latency;
            var okD = step.Ok.DataTransfer;

            var fR = step.Fail.Request;

            var ok = new PointStepStatsOk
            {
                Time = currentTime,
                Step = step.StepName,
                Scenario = scnStats.ScenarioName,
                
                AllReqCount = step.Ok.Request.Count + step.Fail.Request.Count,
                AllDataAll = step.Ok.DataTransfer.AllBytes + step.Fail.DataTransfer.AllBytes,
                
                OkReqCount = okR.Count,
                OkReqRps = okR.RPS,
                FailReqCount = fR.Count,
                FailReqRps = fR.RPS,
                
                OkLatencyMin = okL.MinMs,
                OkLatencyMean = okL.MeanMs,
                OkLatencyMax = okL.MaxMs,
                OkLatencyStdDev = okL.StdDev,
                OkLatencyP50 = okL.Percent50,
                OkLatencyP75 = okL.Percent75,
                OkLatencyP95 = okL.Percent95,
                OkLatencyP99 = okL.Percent99,
                
                OkDataMin = okD.MinBytes,
                OkDataMean = okD.MeanBytes,
                OkDataMax = okD.MaxBytes,
                OkDataAll = okD.AllBytes,
                OkDataP50 = okD.Percent50,
                OkDataP75 = okD.Percent75,
                OkDataP95 = okD.Percent95,
                OkDataP99 = okD.Percent99,
                
                SimulationValue = simulation.Value
            };
            
            AddTestInfoTags(ok);
            
            return ok;
        });
    }
    
    private IEnumerable<PointStepStatsFail> MapStepsStatsFail(ScenarioStats scnStats, DateTimeOffset currentTime)
    {
        return scnStats.StepStats.Select(step =>
        {
            var fL = step.Fail.Latency;
            var fD = step.Fail.DataTransfer;

            var fail = new PointStepStatsFail
            {
                Time = currentTime,
                Step = step.StepName,
                Scenario = scnStats.ScenarioName,
                
                FailLatencyMin = fL.MinMs,
                FailLatencyMean = fL.MeanMs,
                FailLatencyMax = fL.MaxMs,
                FailLatencyStdDev = fL.StdDev,
                FailLatencyP50 = fL.Percent50,
                FailLatencyP75 = fL.Percent75,
                FailLatencyP95 = fL.Percent95,
                FailLatencyP99 = fL.Percent99,

                FailDataMin = fD.MinBytes,
                FailDataMean = fD.MeanBytes,
                FailDataMax = fD.MaxBytes,
                FailDataAll = fD.AllBytes,
                FailDataP50 = fD.Percent50,
                FailDataP75 = fD.Percent75,
                FailDataP95 = fD.Percent95,
                FailDataP99 = fD.Percent99
            };
            
            AddTestInfoTags(fail);
            
            return fail;
        });
    }

    private PointLatencyCounts MapLatencyCount(ScenarioStats scnStats, DateTimeOffset currentTime)
    {
        var fR = scnStats.Fail.Request;
        
        var point = new PointLatencyCounts
        {
            Time = currentTime,
            LessOrEq800 = scnStats.Ok.Latency.LatencyCount.LessOrEq800,
            More800Less1200 = scnStats.Ok.Latency.LatencyCount.More800Less1200,
            MoreOrEq1200 = scnStats.Ok.Latency.LatencyCount.MoreOrEq1200,
            Scenario = scnStats.ScenarioName,
            FailReqCount = fR.Count
        };
        
        AddTestInfoTags(point);

        return point;
    }

    private IEnumerable<PointStatusCodes> MapStatusCodes(ScenarioStats scnStats, DateTimeOffset currentTime)
    {
        return scnStats
            .Ok.StatusCodes.Concat(scnStats.Fail.StatusCodes)
            .Select(s =>
            {
                var point = new PointStatusCodes
                {
                    Time = currentTime,
                    StatusCode = s.StatusCode,
                    Count = s.Count,
                    Scenario = scnStats.ScenarioName
                };

                AddTestInfoTags(point);

                return point;
            });
    }
}