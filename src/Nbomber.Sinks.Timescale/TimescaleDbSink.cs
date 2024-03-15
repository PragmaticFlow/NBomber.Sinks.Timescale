using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NBomber.Contracts;
using NBomber.Contracts.Stats;
using NBomber.Sinks.Timescale.Models;
using Npgsql;
using ILogger = Serilog.ILogger;

namespace NBomber.Sinks.Timescale;

public class TimescaleDbSinkConfig(string connectionString)
{
    public string ConnectionString { get; set; } = connectionString;
}

public class TimescaleDbSink : IReportingSink
{
    private ILogger _logger;
    private IBaseContext _context;
    private NpgsqlConnection _connection;

    private StreamWriter _logFileWriter;
    private ILoggerFactory _factory;
    
    public string SinkName => "NBomber.Sinks.TimescaleDb";

    public TimescaleDbSink() { }

    public TimescaleDbSink(string connectionString)
    {
        _logFileWriter = new StreamWriter("psql_log.txt", append: true);
        _factory = LoggerFactory.Create(builder =>
        {
            builder.AddSimpleConsole(options =>
            {
                options.IncludeScopes = true;
                options.SingleLine = true;
                options.TimestampFormat = "HH:mm:ss ";
            });

            builder.AddProvider(new CustomFileLoggerProvider(_logFileWriter));
        });
            
            
        NpgsqlLoggingConfiguration.InitializeLogging(_factory);
        
        _connection = new NpgsqlConnection(connectionString);
    }
    
    public TimescaleDbSink(NpgsqlConnection connection)
    {
        _connection = connection;
    }
    
    public async Task Init(IBaseContext context, IConfiguration infraConfig)
    {
       
        _logger = context.Logger.ForContext<TimescaleDbSink>();
        _context = context;

        var config = infraConfig?.GetSection("TimescaleDbSink").Get<TimescaleDbSinkConfig>();
        if (config != null)
        {
            _connection = new NpgsqlConnection(config.ConnectionString);
        }
        
        if (_connection == null)
        {
            _logger.Error("Reporting Sink {0} has problems with initialization. The problem could be related to invalid config structure.", SinkName);
                
            throw new Exception(
                $"Reporting Sink {SinkName} has problems with initialization. The problem could be related to invalid config structure.");
        }
        
        _connection.Open();
        
        await _connection.ExecuteAsync(SqlQueries.CreatePointDataStartTable + SqlQueries.CreatePointDataStatusCodesTable + SqlQueries.CreatePointDataLatencyCountsTable + SqlQueries.CreatePointDataStepStatsTable);
    }

    public async Task Start()
    {
        if (_connection != null)
        {
            var point = new PointDataStart
            {
                Time = DateTime.UtcNow,
                Measurement = "nbomber",
                ClusterNodeCount = 1,
                ClusterNodeCpuCount = _context.GetNodeInfo().CoresCount,
            };
            
            AddTestInfoTags(point);
           
            await _connection.ExecuteAsync(SqlQueries.InsertIntoPointDataStartTable, point);
        }
    }

    public Task SaveRealtimeStats(ScenarioStats[] stats) => SaveScenarioStats(stats);

    public Task SaveFinalStats(NodeStats stats) => SaveScenarioStats(stats.ScenarioStats);

    public Task Stop() => Task.CompletedTask;

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
        _logFileWriter.Dispose();
        _factory.Dispose();
    }

    private void AddTestInfoTags(PointDataBase point)
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

    private async Task SaveScenarioStats(ScenarioStats[] stats)
    {

        if (_connection != null)
        {
            var updatedStats = stats.Select(AddGlobalInfoStep).ToArray();

            var realtimeStats = updatedStats.SelectMany(MapStepsStats).ToArray();
            await _connection.ExecuteAsync(SqlQueries.InsertIntoPointDataStepStatsTable(realtimeStats));

            var latencyCounts = stats.Select(MapLatencyCount).ToArray();
            await _connection.ExecuteAsync(SqlQueries.InsertIntoPointDataLatencyCountsTable(latencyCounts));

            var statusCodes = stats.SelectMany(MapStatusCodes).ToArray();
            await _connection.ExecuteAsync(SqlQueries.InsertIntoPointDataStatusCodesTable(statusCodes));
        }
    }

    private ScenarioStats AddGlobalInfoStep(ScenarioStats scnStats)
    {
        var globalStepInfo = new StepStats("global information", scnStats.Ok, scnStats.Fail);
        scnStats.StepStats = scnStats.StepStats.Append(globalStepInfo).ToArray();

        return scnStats;
    }

    private IEnumerable<PointDataStepStats> MapStepsStats(ScenarioStats scnStats)
    {
        var simulation = scnStats.LoadSimulationStats;

        return scnStats.StepStats.Select(step =>
        {
            var okR = step.Ok.Request;
            var okL = step.Ok.Latency;
            var okD = step.Ok.DataTransfer;

            var fR = step.Fail.Request;
            var fL = step.Fail.Latency;
            var fD = step.Fail.DataTransfer;

            var point = new PointDataStepStats
            {
                Time = DateTime.UtcNow,
                Measurement = "nbomber", 
                AllRequestCount = step.Ok.Request.Count + step.Fail.Request.Count,
                AllDataTransferAll = step.Ok.DataTransfer.AllBytes + step.Fail.DataTransfer.AllBytes,
                
                OkRequestCount = okR.Count,
                OkRequestRps = okR.RPS,
                
                OkLatencyMin = okL.MinMs,
                OkLatencyMean = okL.MeanMs,
                OkLatencyMax = okL.MaxMs,
                OkLatencyStdDev = okL.StdDev,
                OkLatencyPercent50 = okL.Percent50,
                OkLatencyPercent75 = okL.Percent75,
                OkLatencyPercent95 = okL.Percent95,
                OkLatencyPercent99 = okL.Percent99,
                
                OkDataTransferMin = okD.MinBytes,
                OkDataTransferMean = okD.MeanBytes,
                OkDataTransferMax = okD.MaxBytes,
                OkDataTransferAll = okD.AllBytes,
                OkDataTransferPercent50 = okD.Percent50,
                OkDataTransferPercent75 = okD.Percent75,
                OkDataTransferPercent95 = okD.Percent95,
                OkDataTransferPercent99 = okD.Percent99,
                
                FailRequestCount = fR.Count,
                FailRequestRps = fR.RPS,
                
                FailLatencyMin = fL.MinMs,
                FailLatencyMean = fL.MeanMs,
                FailLatencyMax = fL.MaxMs,
                FailLatencyStdDev = fL.StdDev,
                FailLatencyPercent50 = fL.Percent50,
                FailLatencyPercent75 = fL.Percent75,
                FailLatencyPercent95 = fL.Percent95,
                FailLatencyPercent99 = fL.Percent99,
                
                FailDataTransferMin = fD.MinBytes,
                FailDataTransferMean = fD.MeanBytes,
                FailDataTransferMax = fD.MaxBytes,
                FailDataTransferAll = fD.AllBytes,
                FailDataTransferPercent50 = fD.Percent50,
                FailDataTransferPercent75 = fD.Percent75,
                FailDataTransferPercent95 = fD.Percent95,
                FailDataTransferPercent99 = fD.Percent99,
                
                SimulationValue = simulation.Value,
                
                Step = step.StepName,
                Scenario = scnStats.ScenarioName
            };
            
            AddTestInfoTags(point);
            
            return point;
        });
    }

    private PointDataLatencyCounts MapLatencyCount(ScenarioStats scnStats)
    {
        var point = new PointDataLatencyCounts
        {
            Time = DateTime.UtcNow,
            Measurement = "nbomber",
            LatencyCountLessOrEq800 = scnStats.Ok.Latency.LatencyCount.LessOrEq800,
            LatencyCountMore800Less1200 = scnStats.Ok.Latency.LatencyCount.More800Less1200,
            LatencyCountMoreOrEq1200 = scnStats.Ok.Latency.LatencyCount.MoreOrEq1200,
            Scenario = scnStats.ScenarioName
        };
        
        AddTestInfoTags(point);

        return point;
    }

    private IEnumerable<PointDataStatusCodes> MapStatusCodes(ScenarioStats scnStats)
    {
        return scnStats
            .Ok.StatusCodes.Concat(scnStats.Fail.StatusCodes)
            .Select(s =>
            {
                var point = new PointDataStatusCodes
                {
                    Time = DateTime.UtcNow,
                    Measurement = "nbomber",
                    StatusCodeStatus = s.StatusCode ?? "0",
                    StatusCodeCount = s.Count,
                    Scenario = scnStats.ScenarioName
                };

                AddTestInfoTags(point);

                return point;
            });
    }
}