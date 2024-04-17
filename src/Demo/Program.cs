 using AutoBogus;
 using Dapper;
 using Microsoft.Extensions.Logging;
 using RepoDb;
 using NBomber.CSharp;
 using NBomber.Sinks.Timescale;
 using NBomber.Sinks.Timescale.Models;
 using Serilog;
 using Serilog.Extensions.Logging;
 using Npgsql;

 new TimescaleDBReportingExample().Run();

 public class TimescaleDBReportingExample
 {
     /*private readonly TimescaleDbSink _timescaleDbSink = 
         new("Host=localhost;Port=5432;Username=andrii;Password=myPass;Pooling=true;");*/
     
     public void Run()
     {
         Log.Logger = new LoggerConfiguration()
             .WriteTo.File("log.txt")
             .CreateLogger();
         
         var loggerFactory = LoggerFactory.Create(builder => builder.AddSerilog());
         NpgsqlLoggingConfiguration.InitializeLogging(loggerFactory);
         
         using var _connection = new NpgsqlConnection(
                 "Host=localhost;Port=5432;Database=timescaledb;Username=andrii;Password=myPass;Pooling=true;");
         var random = new Random();
         
         _connection.Open();
         
         PointDataLatencyCounts fakePointDataLatencyCounts = new();
         PointDataStatusCodes fakePointDataStatusCodes = new();
         PointDataStepStats fakePointDataStepStats = new();
         
         var startTime = DateTimeOffset.UtcNow;
         
         var globalId = "0"; 
         
          /*var writeScenario = Scenario.Create("write_scenario", async context =>
             {
                  var step1 = await Step.Run("insert", context, async () =>
                  {
                      await using var connection1 = new NpgsqlConnection(
                          "Host=localhost;Port=5432;Database=timescaledb;Username=andrii;Password=myPass;Pooling=true;");
                      
                      await using var connection2 = new NpgsqlConnection(
                          "Host=localhost;Port=5432;Database=timescaledb;Username=andrii;Password=myPass;Pooling=true;");
                      
                      await using var connection3 = new NpgsqlConnection(
                          "Host=localhost;Port=5432;Database=timescaledb;Username=andrii;Password=myPass;Pooling=true;");
                      
                     fakePointDataStatusCodes.Time = DateTimeOffset.UtcNow;
                     fakePointDataStatusCodes.SessionId = context.ScenarioInfo.ThreadNumber.ToString();
                     var insert1 = connection1.BinaryBulkInsertAsync("status_codes_points", Enumerable.Repeat(fakePointDataStatusCodes, 120));
                     
                     fakePointDataLatencyCounts.Time = DateTimeOffset.UtcNow;
                     fakePointDataLatencyCounts.SessionId = context.ScenarioInfo.ThreadNumber.ToString();
                     var insert2 = connection2.BinaryBulkInsertAsync("latency_counts_points", Enumerable.Repeat(fakePointDataLatencyCounts, 120));
                     
                     fakePointDataStepStats.Time = DateTimeOffset.UtcNow;
                     fakePointDataStepStats.SessionId = context.ScenarioInfo.ThreadNumber.ToString();
                     var insert3 = connection3.BinaryBulkInsertAsync("step_stats_points",Enumerable.Repeat(fakePointDataStepStats, 120));

                     await Task.WhenAll(insert1, insert2, insert3);
                     
                     return Response.Ok();
                 });
        
                 return Response.Ok();
             }).WithInit( async context =>
             { 
                 /*await _connection.ExecuteAsync(SqlQueries.CreatePointDataStatusCodesTable
                                                + SqlQueries.CreatePointDataLatencyCountsTable 
                                                + SqlQueries.CreatePointDataStepStatsTable);#1#
                 var faker = AutoFaker.Create();
                 
                 fakePointDataLatencyCounts = faker.Generate<PointDataLatencyCounts>();
                 fakePointDataStatusCodes = faker.Generate<PointDataStatusCodes>();
                 fakePointDataStepStats = faker.Generate<PointDataStepStats>();
             })
             .WithLoadSimulations(
                 Simulation.KeepConstant(30, TimeSpan.FromSeconds(30))
             ).WithoutWarmUp();*/
         
         var readScenario = Scenario.Create("read_scenario", async context =>
             {
                 var end = false;

                 var LatencyRes = new List<PointDataLatencyCounts>();
                 var StatusCodesRes = new List<PointDataStatusCodes>();
                 var StepStatsRes = new List<PointDataStepStats>();

                 var step1 = await Step.Run("read", context, async () =>
                 {
                     var endTimeLatencyCounts = startTime;
                     var endTimeStatusCodes = startTime;
                     var endTimeStepStats = startTime;
                     
                     while (!end)
                     {
                         await using var connection1 = new NpgsqlConnection(
                             "Host=localhost;Port=5432;Database=timescaledb;Username=andrii;Password=myPass;Pooling=true;");

                         await using var connection2 = new NpgsqlConnection(
                             "Host=localhost;Port=5432;Database=timescaledb;Username=andrii;Password=myPass;Pooling=true;");

                         await using var connection3 = new NpgsqlConnection(
                             "Host=localhost;Port=5432;Database=timescaledb;Username=andrii;Password=myPass;Pooling=true;");

                         var st1 = endTimeLatencyCounts - TimeSpan.FromMinutes(10);
                         
                         var dataLatencyCounts = connection1.QueryAsync<PointDataLatencyCounts>("latency_counts_points",
                             p => p.SessionId == globalId
                                  && p.Time <= endTimeLatencyCounts
                                  && p.Time >= st1);

                         var st2 = endTimeStatusCodes - TimeSpan.FromSeconds(10);
                         
                         var dataStatusCodes = connection2.QueryAsync<PointDataStatusCodes>("status_codes_points", 
                             p => p.SessionId == globalId
                             && p. Time <= endTimeStatusCodes
                             && p. Time >= st2);

                         var st3 = endTimeStepStats - TimeSpan.FromSeconds(10);
                         
                         var dataStepStats = connection3.QueryAsync<PointDataStepStats>("step_stats_points",
                             p => p.SessionId == globalId 
                             && p.Time <= endTimeStepStats
                             && p.Time >= st3);

                         await Task.WhenAll(dataLatencyCounts, dataStatusCodes, dataStepStats);

                         var dataLatencyCountsArr = dataLatencyCounts.Result.ToArray();
                         var dataStatusCodesArr = dataStatusCodes.Result.ToArray();
                         var dataStepStatsArr = dataStepStats.Result.ToArray();

                         end = true;

                         LatencyRes.AddRange(dataLatencyCountsArr);
                         StepStatsRes.AddRange(dataStepStatsArr);
                         StatusCodesRes.AddRange(dataStatusCodesArr);

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
                 });

                 return Response.Ok();
             })
             .WithLoadSimulations(
                 Simulation.KeepConstant(1, TimeSpan.FromSeconds(30))
             ).WithoutWarmUp()
             .WithInit(async context =>
             {
                 GlobalConfiguration
                     .Setup()
                     .UsePostgreSql();
                 
                 var faker = AutoFaker.Create();
                 
                 fakePointDataLatencyCounts = faker.Generate<PointDataLatencyCounts>();
                 fakePointDataStatusCodes = faker.Generate<PointDataStatusCodes>();
                 fakePointDataStepStats = faker.Generate<PointDataStepStats>();

                 var fakeLatencyCountsPoints = Enumerable
                     .Range(0, 2160).Select(i => new PointDataLatencyCounts
                     {
                         Time = startTime.AddSeconds(-5 * i),
                         SessionId = globalId,
                         
                         Measurement = fakePointDataLatencyCounts.Measurement,
                         CurrentOperation = fakePointDataLatencyCounts.CurrentOperation,
                         NodeType = fakePointDataLatencyCounts.NodeType,
                         TestSuite = fakePointDataLatencyCounts.TestSuite,
                         TestName = fakePointDataLatencyCounts.TestName,
                         ClusterId = fakePointDataLatencyCounts.ClusterId,
                         Scenario = fakePointDataLatencyCounts.Scenario,
                         LatencyCountLessOrEq800 = fakePointDataLatencyCounts.LatencyCountLessOrEq800,
                         LatencyCountMore800Less1200 = fakePointDataLatencyCounts.LatencyCountMore800Less1200,
                         LatencyCountMoreOrEq1200 = fakePointDataLatencyCounts.LatencyCountMoreOrEq1200
                     }).ToArray();
                 
                 var fakeStatusCodesPoints = Enumerable
                     .Range(0, 2160).Select(i => new PointDataStatusCodes
                     {
                         Time = startTime.AddSeconds(-5 * i),
                         SessionId = globalId,
                         
                         Measurement = fakePointDataStatusCodes.Measurement,
                         CurrentOperation = fakePointDataStatusCodes.CurrentOperation,
                         NodeType = fakePointDataStatusCodes.NodeType,
                         TestSuite = fakePointDataStatusCodes.TestSuite,
                         TestName = fakePointDataStatusCodes.TestName,
                         ClusterId = fakePointDataStatusCodes.ClusterId,
                         Scenario = fakePointDataStatusCodes.Scenario,
                         StatusCodeStatus = fakePointDataStatusCodes.StatusCodeStatus,
                         StatusCodeCount = fakePointDataStatusCodes.StatusCodeCount
                     }).ToArray();
                 
                 var fakeStepStatsPoints = Enumerable
                     .Range(0, 2160).Select(i => new PointDataStepStats()
                     { 
                         Time = startTime.AddSeconds(-5 * i),
                         SessionId = globalId,
                         
                         Measurement = fakePointDataStepStats.Measurement,
                         CurrentOperation = fakePointDataStepStats.CurrentOperation,
                         NodeType = fakePointDataStepStats.NodeType,
                         TestSuite = fakePointDataStepStats.TestSuite,
                         TestName = fakePointDataStepStats.TestName,
                         ClusterId = fakePointDataStepStats.ClusterId,
                         Scenario = fakePointDataStepStats.Scenario,
                         Step = fakePointDataStepStats.Step,
                         AllRequestCount = fakePointDataStepStats.AllRequestCount,
                         AllDataTransferAll = fakePointDataStepStats.AllDataTransferAll,
                         OkRequestCount = fakePointDataStepStats.OkRequestCount,
                         OkRequestRps = fakePointDataStepStats.OkRequestRps,
                         OkLatencyMax = fakePointDataStepStats.OkLatencyMax,
                         OkLatencyMean = fakePointDataStepStats.OkLatencyMean,
                         OkLatencyMin = fakePointDataStepStats.OkLatencyMin,
                         OkLatencyStdDev = fakePointDataStepStats.OkLatencyStdDev,
                         OkLatencyPercent50 = fakePointDataStepStats.OkLatencyPercent50,
                         OkLatencyPercent75 = fakePointDataStepStats.OkLatencyPercent75,
                         OkLatencyPercent95 = fakePointDataStepStats.OkLatencyPercent95,
                         OkLatencyPercent99 = fakePointDataStepStats.OkLatencyPercent99,
                         OkDataTransferMin = fakePointDataStepStats.OkDataTransferMin,
                         OkDataTransferMean = fakePointDataStepStats.OkDataTransferMean,
                         OkDataTransferMax = fakePointDataStepStats.OkDataTransferMax,
                         OkDataTransferAll = fakePointDataStepStats.OkDataTransferAll,
                         OkDataTransferPercent50 = fakePointDataStepStats.OkDataTransferPercent50,
                         OkDataTransferPercent75 = fakePointDataStepStats.OkDataTransferPercent75,
                         OkDataTransferPercent95 = fakePointDataStepStats.OkDataTransferPercent95,
                         OkDataTransferPercent99 = fakePointDataStepStats.OkDataTransferPercent99,
                         FailRequestCount = fakePointDataStepStats.FailRequestCount,
                         FailRequestRps = fakePointDataStepStats.FailRequestRps,
                         FailLatencyMax = fakePointDataStepStats.FailLatencyMax,
                         FailLatencyMean = fakePointDataStepStats.FailLatencyMean,
                         FailLatencyMin = fakePointDataStepStats.FailLatencyMin,
                         FailLatencyStdDev = fakePointDataStepStats.FailLatencyStdDev,
                         FailLatencyPercent50 = fakePointDataStepStats.FailLatencyPercent50,
                         FailLatencyPercent75 = fakePointDataStepStats.FailLatencyPercent75,
                         FailLatencyPercent95 = fakePointDataStepStats.FailLatencyPercent95,
                         FailLatencyPercent99 = fakePointDataStepStats.FailLatencyPercent99,
                         FailDataTransferMin = fakePointDataStepStats.FailDataTransferMin,
                         FailDataTransferMean = fakePointDataStepStats.FailDataTransferMean,
                         FailDataTransferMax = fakePointDataStepStats.FailDataTransferMax,
                         FailDataTransferAll = fakePointDataStepStats.FailDataTransferAll,
                         FailDataTransferPercent50 = fakePointDataStepStats.FailDataTransferPercent50,
                         FailDataTransferPercent75 = fakePointDataStepStats.FailDataTransferPercent75,
                         FailDataTransferPercent95 = fakePointDataStepStats.FailDataTransferPercent95,
                         FailDataTransferPercent99 = fakePointDataStepStats.FailDataTransferPercent99,
                         SimulationValue = fakePointDataStepStats.SimulationValue
                     }).ToArray();
                 
                 await using var connection1 = new NpgsqlConnection(
                     "Host=localhost;Port=5432;Database=timescaledb;Username=andrii;Password=myPass;Pooling=true;");
                      
                 await using var connection2 = new NpgsqlConnection(
                     "Host=localhost;Port=5432;Database=timescaledb;Username=andrii;Password=myPass;Pooling=true;");
                      
                 await using var connection3 = new NpgsqlConnection(
                     "Host=localhost;Port=5432;Database=timescaledb;Username=andrii;Password=myPass;Pooling=true;");
                 
                 var insert1 = connection1.BinaryBulkInsertAsync("status_codes_points", fakeStatusCodesPoints);
                 
                 var insert2 = connection2.BinaryBulkInsertAsync("latency_counts_points", fakeLatencyCountsPoints);

                 var insert3 = connection3.BinaryBulkInsertAsync("step_stats_points", fakeStepStatsPoints);

                 await Task.WhenAll(insert1, insert2, insert3);
             });

         NBomberRunner
             .RegisterScenarios(readScenario)
             //.LoadInfraConfig("infra-config.json")
             //.WithReportingInterval(TimeSpan.FromSeconds(5))
             //.WithReportingSinks(_timescaleDbSink)
             .WithTestSuite("reporting")
             .WithTestName("timescale_db_demo")
             .Run();
     }
 }