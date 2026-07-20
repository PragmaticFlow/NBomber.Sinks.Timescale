using System.IO.Compression;
using System.Text;
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

/// <summary>
/// Configuration class for the TimescaleDbSink.
/// </summary>
public class TimescaleDbSinkConfig
{
    /// <summary>Initializes a new instance with default values.</summary>
    public TimescaleDbSinkConfig() { }

    /// <summary>Initializes a new instance with the specified connection string and optional stop-command listener setting.</summary>
    public TimescaleDbSinkConfig(string connectionString, bool listenStopCommandEnabled = true)
    {
        ConnectionString = connectionString;
        ListenStopCommandEnabled = listenStopCommandEnabled;
    }

    /// <summary>
    /// Gets or sets the connection string for TimescaleDB.
    /// </summary>
    public string ConnectionString { get; set; } = "";

    /// <summary>
    /// Indicates whether PostgreSQL session channel (LISTEN/NOTIFY) is enabled.
    /// Defaults to true when absent from JSON configuration.
    /// </summary>
    public bool ListenStopCommandEnabled { get; set; } = true;
}

/// <summary>
/// Reporting sink for NBomber that stores performance statistics and metrics in TimescaleDB.
/// </summary>
public class TimescaleDbSink : IReportingSink
{
    private ILogger _logger;
    private IBaseContext _context;
    private NpgsqlDataSource _dataSource;
    private TimescaleDbSinkConfig _config = new("");
    private bool _disposed = false;
    private CancellationTokenSource _sessionChannelCTS = new();

    internal static readonly string StopSessionChannelName = "nbomber_stop_session";

    /// <summary>
    /// Gets the name of the sink.
    /// </summary>
    public string SinkName => "NBomber.Sinks.TimescaleDb";

    /// <summary>
    /// Indicates whether the stop command is currently being listened for.
    /// </summary>
    public bool StopCommandListening { get; set; } = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="TimescaleDbSink"/> class with default configuration.
    /// </summary>
    public TimescaleDbSink() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="TimescaleDbSink"/> class using the specified configuration.
    /// </summary>
    /// <param name="config">The configuration object containing the TimescaleDB connection string.</param>
    public TimescaleDbSink(TimescaleDbSinkConfig config)
    {
        _config = config;
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(config.ConnectionString);
        _dataSource = dataSourceBuilder.Build();
    }

    /// <summary>
    /// Initializes the sink with the NBomber context and configuration.
    /// Also opens a connection to the TimescaleDB and runs database migrations.
    /// </summary>
    /// <param name="context">NBomber base context object.</param>
    /// <param name="infraConfig">Infrastructure configuration section.</param>
    /// <returns>A task representing the asynchronous initialization operation.</returns>
    public async Task Init(IBaseContext context, IConfiguration infraConfig)
    {
        _logger = context.Logger.ForContext<TimescaleDbSink>();
        _context = context;

        var config = infraConfig?.GetSection("TimescaleDbSink").Get<TimescaleDbSinkConfig>();
        if (config != null)
        {
            _config = config;
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(_config.ConnectionString);
            _dataSource = dataSourceBuilder.Build();
        }

        if (_dataSource == null)
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

        var nodeInfo = context.GetNodeInfo();
        if (!nodeInfo.NodeType.IsAgent && _config.ListenStopCommandEnabled) 
        {
            StartListenStopCommand(_dataSource);
        }

        await using var connection = await _dataSource.OpenConnectionAsync();
        var migration = new DbMigrations(connection, _logger);
        await migration.Run();
    }

    /// <summary>
    /// Called at the beginning of a test session. Stores session metadata in TimescaleDB.
    /// </summary>
    /// <param name="sessionInfo">Information about the test session.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task Start(SessionStartInfo sessionInfo)
    {
        if (_dataSource != null)
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
                    await using var connection = await _dataSource.OpenConnectionAsync();
                    var res = await connection.InsertAsync(TableNames.SessionsTable, record);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, ex.Message);
                    throw;
                }
            }
        }
    }

    /// <summary>
    /// Saves real-time scenario statistics during the bombing phase of the test run.
    /// </summary>
    /// <param name="stats">An array of scenario statistics.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SaveRealtimeStats(ScenarioStats[] stats)
    {
        var currentTime = DateTime.UtcNow;

        var points = stats.Select(AddGlobalInfoStep)
            .SelectMany(step => MapStepToDbRecord(step, currentTime, OperationType.Bombing))
            .ToArray();

        await using var connection = await _dataSource.OpenConnectionAsync();
        await connection.BinaryBulkInsertAsync(TableNames.StepStatsTable, points);
    }

    /// <summary>
    /// Saves real-time metric statistics (counters and gauges) during the bombing phase.
    /// </summary>
    /// <param name="metrics">The metrics data to store.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SaveRealtimeMetrics(MetricStats metrics)
    {
        var points = MapMetricToDbRecord(metrics, DateTime.UtcNow, OperationType.Bombing);
        
        await using var connection = await _dataSource.OpenConnectionAsync();
        await connection.BinaryBulkInsertAsync(TableNames.MetricsTable, points);
    }

    /// <summary>
    /// Saves final aggregated statistics and metrics after the test run has completed.
    /// Updates the session record to mark the test as completed.
    /// </summary>
    /// <param name="stats">The final node statistics.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SaveFinalStats(NodeStats stats)
    {
        var currentTime = DateTime.UtcNow;
        var operation = OperationType.Complete;
        var testInfo = _context.TestInfo;

        var metrics = MapMetricToDbRecord(stats.Metrics, currentTime, operation);

        var stepsStats = stats.ScenarioStats.Select(AddGlobalInfoStep)
            .SelectMany(step => MapStepToDbRecord(step, currentTime, operation))
            .ToArray();

        var artifacts = stats.ReportFiles.Length > 0
            ? ZipArtifacts(Path.GetDirectoryName(stats.ReportFiles.First().FilePath))
            : [];

        var queryEntity = new SessionInfoDbRecord
        {
            SessionId = testInfo.SessionId,
            CurrentOperation = stats.NodeInfo.CurrentOperation,
            LastUpdatedTime = currentTime,
            SessionResult = JsonSerializer.Serialize(new SessionResult(stepsStats)),
            Artifacts = artifacts.Length == 0 ? null : artifacts
        };

        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var ts = await connection.BeginTransactionAsync();

        await connection.BinaryBulkInsertAsync(TableNames.StepStatsTable, stepsStats, transaction: ts);
        await connection.BinaryBulkInsertAsync(TableNames.MetricsTable, metrics, transaction: ts);

        var fields = Field.Parse<SessionInfoDbRecord>(e => new { e.SessionId, e.CurrentOperation, e.LastUpdatedTime, e.SessionResult, e.Artifacts });
        await connection.UpdateAsync(TableNames.SessionsTable, queryEntity, fields: fields, transaction: ts);

        await ts.CommitAsync();
    }

    /// <summary>
    /// Called when the test session ends
    /// </summary>
    public Task Stop() => Task.CompletedTask;

    /// <summary>
    /// Disposes the sink by closing and releasing the TimescaleDB connection.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _sessionChannelCTS.Cancel();
            _sessionChannelCTS.Dispose();
            _dataSource?.Dispose();
            _disposed = true;
        }
    }

    private ScenarioStats AddGlobalInfoStep(ScenarioStats scnStats)
    {
        var globalStepInfo = new StepStats("global information", scnStats.Ok, scnStats.Fail, 0);
        scnStats.StepStats = scnStats.StepStats.Append(globalStepInfo).ToArray();

        return scnStats;
    }

    private MetricDbRecord[] MapMetricToDbRecord(MetricStats stats, DateTime currentTime, OperationType operationType)
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

    private StepStatsDbRecord[] MapStepToDbRecord(ScenarioStats scnStats, DateTime currentTime, OperationType currentOperation)
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
            .Select(step => new StepStatsDbRecord
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
                OkBytesPerSecond = step.Ok.DataTransfer.BytesPerSecond,
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
                FailBytesPerSecond = step.Fail.DataTransfer.BytesPerSecond,
                FailDataAll = step.Fail.DataTransfer.AllBytes,
                FailDataP50 = step.Fail.DataTransfer.Percent50,
                FailDataP75 = step.Fail.DataTransfer.Percent75,
                FailDataP95 = step.Fail.DataTransfer.Percent95,
                FailDataP99 = step.Fail.DataTransfer.Percent99,
                FailStatusCodes = JsonSerializer.Serialize(step.Fail.StatusCodes),
                FailLatencyCount = JsonSerializer.Serialize(step.Fail.Latency.LatencyCount),

                SimulationName = scnStats.LoadSimulationStats.SimulationName,
                SimulationValue = scnStats.LoadSimulationStats.Value
            })
            .ToArray();
    }

    private string GetListenStopCommandChannelName()
    {
        return $"{StopSessionChannelName}__{_context.TestInfo.SessionId.Replace("-", "_")}";
    }

    private void StartListenStopCommand(NpgsqlDataSource dataSource)
    {
        _ = Task.Run(async () =>
        {
            while (!_disposed)
            {
                try
                {
                    await using var connection = await dataSource.OpenConnectionAsync();

                    var channel = GetListenStopCommandChannelName();
                    
                    await connection.ExecuteNonQueryAsync($"LISTEN {channel}");

                    connection.Notification += (obj, e) =>
                    {
                        _context.StopCurrentTest(reason: "Received Stop command from TimescaleDB");
                    };

                    while (!_disposed)
                    {
                        StopCommandListening = true;
                        _logger.Debug($"{nameof(TimescaleDbSink)}: waiting on the PUSH message");
                        
                        await connection.WaitAsync(_sessionChannelCTS.Token);
                    }

                    await connection.ExecuteNonQueryAsync($"UNLISTEN {channel}");
                }
                catch (OperationCanceledException)
                {
                    // do nothing
                }
                catch (Exception ex) when (!_disposed)
                {
                    _logger.Warning(ex, $"{nameof(TimescaleDbSink)}: failed to connect, retrying in 5s...");
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
                finally
                {
                    StopCommandListening = false;
                }
            }
        });
    }

    private byte[] ZipArtifacts(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            return [];

        var files = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);

        if (files.Length == 0)
            return [];

        var artifacts = files.ToDictionary(
            filePath => Path.GetRelativePath(folderPath, filePath), 
            filePath =>
            {
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream, Encoding.UTF8);

                return reader.ReadToEnd();
            }
        );

        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (fileName, content) in artifacts)
            {
                var entry = archive.CreateEntry(fileName, CompressionLevel.Optimal);
                using var entryStream = entry.Open();
                using var writer = new StreamWriter(entryStream, Encoding.UTF8);
                writer.Write(content);
            }
        }

        return memoryStream.ToArray();
    }
}