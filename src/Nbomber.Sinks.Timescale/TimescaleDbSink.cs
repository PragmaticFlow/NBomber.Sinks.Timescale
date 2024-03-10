// using Dapper;
// using Serilog;
// using Microsoft.Extensions.Configuration;
// using NBomber.Contracts;
// using NBomber.Contracts.Stats;
// using NBomber.Sinks.Timescale.Models;
// using Npgsql;
//
// namespace NBomber.Sinks.Timescale;
//
// public class TimescaleDbSinkConfig
// {
//     public string ConnectionString { get; set; }
// }
//
// public class TimescaleDbSink : IReportingSink
// {
//     private ILogger _logger;
//     private IBaseContext _context;
//     private NpgsqlConnection _connection;
//     
//     public string SinkName => "NBomber.Sinks.TimescaleDb";
//
//     public TimescaleDbSink() { }
//
//     public TimescaleDbSink(string connectionString)
//     {
//         _connection = new NpgsqlConnection(connectionString);
//     }
//     
//     public TimescaleDbSink(NpgsqlConnection connection)
//     {
//         _connection = connection;
//     }
//     
//     public Task Init(IBaseContext context, IConfiguration infraConfig)
//     {
//         _logger = context.Logger.ForContext<TimescaleDbSink>();
//         _context = context;
//
//         var config = infraConfig?.GetSection("TimescaleDbSink").Get<TimescaleDbSinkConfig>();
//         if (config != null)
//         {
//             _connection = new NpgsqlConnection(config.ConnectionString);
//         }
//         
//         if (_connection == null)
//         {
//             _logger.Error("Reporting Sink {0} has problems with initialization. The problem could be related to invalid config structure.", SinkName);
//                 
//             throw new Exception(
//                 $"Reporting Sink {SinkName} has problems with initialization. The problem could be related to invalid config structure.");
//         }
//
//         return Task.WhenAll(_connection.ExecuteAsync(SqlQueries.CreatePointDataStartTable),
//             _connection.ExecuteAsync(SqlQueries.CreatePointDataStatusCodesTable),
//             _connection.ExecuteAsync(SqlQueries.CreatePointDataLatencyCountsTable),
//             _connection.ExecuteAsync(SqlQueries.CreatePointDataStepStatsTable));
//     }
//
//     public async Task Start()
//     {
//         if (_timescaleDbContext != null)
//         {
//             var point = new PointData
//             {
//                 Time = DateTime.UtcNow,
//                 Measurement = "nbomber",
//                 ClusterNodeCount = 1,
//                 ClusterNodeCpuCount = _context.GetNodeInfo().CoresCount,
//             };
//             
//             AddTestInfoTags(point);
//            
//             await _timescaleDbContext.Points.AddAsync(point);
//             await _timescaleDbContext.SaveChangesAsync();
//             
//         }
//     }
//
//     public Task SaveRealtimeStats(ScenarioStats[] stats) => SaveScenarioStats(stats);
//
//     public Task SaveFinalStats(NodeStats stats) => SaveScenarioStats(stats.ScenarioStats);
//
//     public Task Stop() => Task.CompletedTask;
//     
//     public void Dispose() => _connection.Dispose();
//
//     private void AddTestInfoTags(PointData point)
//     {
//         var nodeInfo = _context.GetNodeInfo();
//         var testInfo = _context.TestInfo;
//
//         point.SessionId = testInfo.SessionId;
//         point.CurrentOperation = nodeInfo.CurrentOperation.ToString().ToLower();
//         point.NodeType = nodeInfo.NodeType.ToString();
//         point.TestSuite = testInfo.TestSuite;
//         point.TestName = testInfo.TestName;
//         point.ClusterId = testInfo.ClusterId;
//     }
//
//     private Task SaveScenarioStats(ScenarioStats[] stats)
//     {
//         
//         if (_timescaleDbContext == null) return Task.CompletedTask;
//         
//         var updatedStats = stats.Select(AddGlobalInfoStep).ToArray();
//
//         var realtimeStats = updatedStats.SelectMany(MapStepsStats).ToArray();
//         var addRealtimeStats = _timescaleDbContext.Points.AddRangeAsync(realtimeStats);
//
//         var latencyCounts = stats.Select(MapLatencyCount).ToArray();
//         var addLatencyCounts = _timescaleDbContext.Points.AddRangeAsync(latencyCounts);
//
//         var statusCodes = stats.SelectMany(MapStatusCodes).ToArray();
//         var addStatusCodes = _timescaleDbContext.Points.AddRangeAsync(statusCodes);
//         
//         Task.WhenAll(addRealtimeStats, addLatencyCounts, addStatusCodes);
//         
//         return _timescaleDbContext.SaveChangesAsync();
//     }
//
//     private ScenarioStats AddGlobalInfoStep(ScenarioStats scnStats)
//     {
//         var globalStepInfo = new StepStats("global information", scnStats.Ok, scnStats.Fail);
//         scnStats.StepStats = scnStats.StepStats.Append(globalStepInfo).ToArray();
//
//         return scnStats;
//     }
//
//     private IEnumerable<PointData> MapStepsStats(ScenarioStats scnStats)
//     {
//         var simulation = scnStats.LoadSimulationStats;
//
//         return scnStats.StepStats.Select(step =>
//         {
//             var okR = step.Ok.Request;
//             var okL = step.Ok.Latency;
//             var okD = step.Ok.DataTransfer;
//
//             var fR = step.Fail.Request;
//             var fL = step.Fail.Latency;
//             var fD = step.Fail.DataTransfer;
//
//             var point = new PointData
//             {
//                 Time = DateTime.UtcNow,
//                 Measurement = "nbomber", 
//                 AllRequestCount = step.Ok.Request.Count + step.Fail.Request.Count,
//                 AllDataTransferAll = step.Ok.DataTransfer.AllBytes + step.Fail.DataTransfer.AllBytes,
//                 
//                 OkRequestCount = okR.Count,
//                 OkRequestRps = okR.RPS,
//                 
//                 OkLatencyMin = okL.MinMs,
//                 OkLatencyMean = okL.MeanMs,
//                 OkLatencyMax = okL.MaxMs,
//                 OkLatencyStdDev = okL.StdDev,
//                 OkLatencyPercent50 = okL.Percent50,
//                 OkLatencyPercent75 = okL.Percent75,
//                 OkLatencyPercent95 = okL.Percent95,
//                 OkLatencyPercent99 = okL.Percent99,
//                 
//                 OkDataTransferMin = okD.MinBytes,
//                 OkDataTransferMean = okD.MeanBytes,
//                 OkDataTransferMax = okD.MaxBytes,
//                 OkDataTransferAll = okD.AllBytes,
//                 OkDataTransferPercent50 = okD.Percent50,
//                 OkDataTransferPercent75 = okD.Percent75,
//                 OkDataTransferPercent95 = okD.Percent95,
//                 OkDataTransferPercent99 = okD.Percent99,
//                 
//                 FailRequestCount = fR.Count,
//                 FailRequestRps = fR.RPS,
//                 
//                 FailLatencyMin = fL.MinMs,
//                 FailLatencyMean = fL.MeanMs,
//                 FailLatencyMax = fL.MaxMs,
//                 FailLatencyStdDev = fL.StdDev,
//                 FailLatencyPercent50 = fL.Percent50,
//                 FailLatencyPercent75 = fL.Percent75,
//                 FailLatencyPercent95 = fL.Percent95,
//                 FailLatencyPercent99 = fL.Percent99,
//                 
//                 FailDataTransferMin = fD.MinBytes,
//                 FailDataTransferMean = fD.MeanBytes,
//                 FailDataTransferMax = fD.MaxBytes,
//                 FailDataTransferAll = fD.AllBytes,
//                 FailDataTransferPercent50 = fD.Percent50,
//                 FailDataTransferPercent75 = fD.Percent75,
//                 FailDataTransferPercent95 = fD.Percent95,
//                 FailDataTransferPercent99 = fD.Percent99,
//                 
//                 SimulationValue = simulation.Value,
//                 
//                 Step = step.StepName,
//                 Scenario = scnStats.ScenarioName
//             };
//             
//             AddTestInfoTags(point);
//             
//             return point;
//         });
//     }
//
//     private PointData MapLatencyCount(ScenarioStats scnStats)
//     {
//         var point = new PointData
//         {
//             Time = DateTime.UtcNow,
//             Measurement = "nbomber",
//             LatencyCountLessOrEq800 = scnStats.Ok.Latency.LatencyCount.LessOrEq800,
//             LatencyCountMore800Less1200 = scnStats.Ok.Latency.LatencyCount.More800Less1200,
//             LatencyCountMoreOrEq1200 = scnStats.Ok.Latency.LatencyCount.MoreOrEq1200,
//             Scenario = scnStats.ScenarioName
//         };
//         
//         AddTestInfoTags(point);
//
//         return point;
//     }
//
//     private IEnumerable<PointData> MapStatusCodes(ScenarioStats scnStats)
//     {
//         return scnStats
//             .Ok.StatusCodes.Concat(scnStats.Fail.StatusCodes)
//             .Select(s =>
//             {
//                 var point = new PointData
//                 {
//                     Time = DateTime.UtcNow,
//                     Measurement = "nbomber",
//                     StatusCodeStatus = s.StatusCode ?? "0",
//                     StatusCodeCount = s.Count,
//                     Scenario = scnStats.ScenarioName
//                 };
//
//                 AddTestInfoTags(point);
//
//                 return point;
//             });
//     }
// }