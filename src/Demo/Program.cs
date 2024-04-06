 using AutoBogus;
 using Dapper;
 using NBomber.CSharp;
 using NBomber.Sinks.Timescale;
 using NBomber.Sinks.Timescale.Models;
 using Npgsql;

 new TimescaleDBReportingExample().Run();

 public class TimescaleDBReportingExample
 {
     /*private readonly TimescaleDbSink _timescaleDbSink = 
         new("Host=localhost;Port=5432;Username=andrii;Password=myPass;Pooling=true;");*/
     
     public void Run()
     {
         using var _connection = new NpgsqlConnection(
                 "Host=localhost;Port=5432;Database=timescaledb;Username=andrii;Password=myPass;Pooling=true;");
         var random = new Random();
         
         _connection.Open();

         PointDataLatencyCounts fakePointDataLatencyCounts = new();
         PointDataStatusCodes fakePointDataStatusCodes = new();
         PointDataStepStats fakePointDataStepStats = new();
         
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
                     var insert1 = connection1.ExecuteAsync(
                         SqlQueries.InsertIntoPointDataStatusCodesTable(Enumerable.Repeat(fakePointDataStatusCodes, 360)
                             .ToArray()));
                     
                     fakePointDataLatencyCounts.Time = DateTimeOffset.UtcNow;
                     fakePointDataLatencyCounts.SessionId = context.ScenarioInfo.ThreadNumber.ToString();
                     var insert2 = connection2.ExecuteAsync(
                         SqlQueries.InsertIntoPointDataLatencyCountsTable(Enumerable
                             .Repeat(fakePointDataLatencyCounts, 360).ToArray()));
                     
                     fakePointDataStepStats.Time = DateTimeOffset.UtcNow;
                     fakePointDataStepStats.SessionId = context.ScenarioInfo.ThreadNumber.ToString();
                     var insert3 = connection3.ExecuteAsync(
                         SqlQueries.InsertIntoPointDataStepStatsTable(Enumerable.Repeat(fakePointDataStepStats, 360)
                             .ToArray()));

                     await Task.WhenAll(insert1, insert2, insert3);
                     
                     return Response.Ok();
                 });

                 var step2 = await Step.Run("pause", context, async () =>
                 {
                     await Task.Delay(TimeSpan.FromSeconds(5));

                     return Response.Ok();
                 });
        
                 return Response.Ok();
             }).WithInit( async context =>
             { 
                 await _connection.ExecuteAsync(SqlQueries.CreatePointDataStatusCodesTable
                                                + SqlQueries.CreatePointDataLatencyCountsTable 
                                                + SqlQueries.CreatePointDataStepStatsTable);
                 var faker = AutoFaker.Create();
                 
                 fakePointDataLatencyCounts = faker.Generate<PointDataLatencyCounts>();
                 fakePointDataStatusCodes = faker.Generate<PointDataStatusCodes>();
                 fakePointDataStepStats = faker.Generate<PointDataStepStats>();
             })
             .WithLoadSimulations(
                 Simulation.KeepConstant(100, TimeSpan.FromSeconds(4))
             ).WithoutWarmUp();
         
         var readScenario = Scenario.Create("read_scenario", async context =>
             {
                  var step1 = await Step.Run("read", context, async () =>
                 {
                     await using var connection1 = new NpgsqlConnection(
                         "Host=localhost;Port=5432;Database=timescaledb;Username=andrii;Password=myPass;Pooling=true;");
                      
                     await using var connection2 = new NpgsqlConnection(
                         "Host=localhost;Port=5432;Database=timescaledb;Username=andrii;Password=myPass;Pooling=true;");
                      
                     await using var connection3 = new NpgsqlConnection(
                         "Host=localhost;Port=5432;Database=timescaledb;Username=andrii;Password=myPass;Pooling=true;");

                     var dataLatencyCounts = connection1.QueryAsync<PointDataLatencyCounts>(
                         SqlQueries.SelectFromPointDataLatencyCountsTable(), new
                         {
                             SessionId = context.ScenarioInfo.ThreadNumber.ToString(),
                             EndTime = DateTime.UtcNow,
                             StartTime = DateTime.UtcNow.AddMinutes(-30),
                         });
                     
                     var dataStatusCodes = connection2.QueryAsync<PointDataStatusCodes>(
                         SqlQueries.SelectFromPointDataStatusCodesTable(), new
                         {
                             SessionId = context.ScenarioInfo.ThreadNumber.ToString(),
                             EndTime = DateTime.UtcNow,
                             StartTime = DateTime.UtcNow.AddMinutes(-30),
                         });
                     
                     var dataStepStats = connection3.QueryAsync<PointDataStepStats>(
                         SqlQueries.SelectFromPointDataStepStatsTable(), new
                         {
                             SessionId = context.ScenarioInfo.ThreadNumber.ToString(),
                             EndTime = DateTime.UtcNow,
                             StartTime = DateTime.UtcNow.AddMinutes(-30),
                         });

                     await Task.WhenAll(dataLatencyCounts, dataStatusCodes, dataStepStats);
                     
                     return Response.Ok();
                 });

                 var step2 = await Step.Run("pause", context, async () =>
                 {
                     await Task.Delay(TimeSpan.FromSeconds(2));

                     return Response.Ok();
                 });
        
                 return Response.Ok();
             })
             .WithLoadSimulations(
                 Simulation.KeepConstant(100, TimeSpan.FromSeconds(30))
             ).WithWarmUpDuration(TimeSpan.FromSeconds(3));*/

         var readScenario = Scenario.Create("read_scenario", async context =>
             {
                 var endTimeLatencyCounts = DateTimeOffset.UtcNow;
                 var endTimeStatusCodes = DateTimeOffset.UtcNow;
                 var endTimeStepStats = DateTimeOffset.UtcNow;

                 var end = false;

                 var LatencyRes = new List<PointDataLatencyCounts>();
                 var StatusCodesRes = new List<PointDataStatusCodes>();
                 var StepStatsRes = new List<PointDataStepStats>();

                 var step1 = await Step.Run("read", context, async () =>
                 {
                     while (!end)
                     {
                         await using var connection1 = new NpgsqlConnection(
                             "Host=localhost;Port=5432;Database=timescaledb;Username=andrii;Password=myPass;Pooling=true;");

                         await using var connection2 = new NpgsqlConnection(
                             "Host=localhost;Port=5432;Database=timescaledb;Username=andrii;Password=myPass;Pooling=true;");

                         await using var connection3 = new NpgsqlConnection(
                             "Host=localhost;Port=5432;Database=timescaledb;Username=andrii;Password=myPass;Pooling=true;");

                         var dataLatencyCounts = connection1.QueryAsync<PointDataLatencyCounts>(
                             SqlQueries.SelectFromPointDataLatencyCountsTable(), new
                             {
                                 SessionId = context.ScenarioInfo.ThreadNumber.ToString(),
                                 EndTime = endTimeLatencyCounts,
                                 StartTime = endTimeLatencyCounts - TimeSpan.FromMinutes(11)
                             });

                         var dataStatusCodes = connection2.QueryAsync<PointDataStatusCodes>(
                             SqlQueries.SelectFromPointDataStatusCodesTable(), new
                             {
                                 SessionId = context.ScenarioInfo.ThreadNumber.ToString(),
                                 EndTime = endTimeStatusCodes,
                                 StartTime = endTimeStatusCodes - TimeSpan.FromMinutes(11)
                             });

                         var dataStepStats = connection3.QueryAsync<PointDataStepStats>(
                             SqlQueries.SelectFromPointDataStepStatsTable(), new
                             {
                                 SessionId = context.ScenarioInfo.ThreadNumber.ToString(),
                                 EndTime = endTimeStepStats,
                                 StartTime = endTimeStepStats - TimeSpan.FromMinutes(11)
                             });

                         await Task.WhenAll(dataLatencyCounts, dataStatusCodes, dataStepStats);

                         end = true;

                         LatencyRes.AddRange(dataLatencyCounts.Result);
                         StepStatsRes.AddRange(dataStepStats.Result);
                         StatusCodesRes.AddRange(dataStatusCodes.Result);

                         if (dataLatencyCounts.Result.Count() >= 120)
                         {
                             endTimeLatencyCounts = dataLatencyCounts.Result.ToArray()[^1].Time + TimeSpan.FromSeconds(1);
                             end = false;
                         }

                         if (dataStatusCodes.Result.Count() >= 120)
                         {
                             endTimeStatusCodes = dataStatusCodes.Result.ToArray()[^1].Time  + TimeSpan.FromSeconds(1);
                             end = false;
                         }

                         if (dataStepStats.Result.Count() >= 120)
                         {
                             endTimeStepStats = dataStepStats.Result.ToArray()[^1].Time + TimeSpan.FromSeconds(1);
                             end = false;
                         }
                     }

                     return Response.Ok();
                 });

                 var step2 = await Step.Run("pause", context, async () =>
                 {
                     await Task.Delay(TimeSpan.FromSeconds(30));

                     return Response.Ok();
                 });

                 return Response.Ok();
             })
             .WithLoadSimulations(
                 Simulation.KeepConstant(1, TimeSpan.FromSeconds(30))
             ).WithWarmUpDuration(TimeSpan.FromSeconds(3))
             .WithInit(async context =>
             {
                 await _connection.ExecuteAsync(SqlQueries.CreatePointDataStatusCodesTable
                                                + SqlQueries.CreatePointDataLatencyCountsTable 
                                                + SqlQueries.CreatePointDataStepStatsTable);
                 var faker = AutoFaker.Create();
                 
                 fakePointDataLatencyCounts = faker.Generate<PointDataLatencyCounts>();
                 fakePointDataStatusCodes = faker.Generate<PointDataStatusCodes>();
                 fakePointDataStepStats = faker.Generate<PointDataStepStats>();
                 
                 var startTime = DateTimeOffset.UtcNow;

                 var fakeLatencyCountsPoints = Enumerable
                     .Range(0, 2160).Select(i => new PointDataLatencyCounts
                     {
                         Time = startTime.AddSeconds(-5 * i),
                         SessionId = "0",
                         
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
                         SessionId = "0",
                         
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
                         SessionId = "0",
                         
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

                 var insert1 =
                     connection1.ExecuteAsync(SqlQueries.InsertIntoPointDataStatusCodesTable(fakeStatusCodesPoints));

                 var insert2 =
                     connection2.ExecuteAsync(SqlQueries.InsertIntoPointDataLatencyCountsTable(fakeLatencyCountsPoints));

                 var insert3 =
                     connection3.ExecuteAsync(SqlQueries.InsertIntoPointDataStepStatsTable(fakeStepStatsPoints));

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