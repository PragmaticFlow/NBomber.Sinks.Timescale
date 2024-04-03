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
         
         var writeScenario = Scenario.Create("write_scenario", async context =>
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
                         SqlQueries.InsertIntoPointDataStatusCodesTable(Enumerable.Repeat(fakePointDataStatusCodes, 120)
                             .ToArray()));
                     
                     fakePointDataLatencyCounts.Time = DateTimeOffset.UtcNow;
                     fakePointDataLatencyCounts.SessionId = context.ScenarioInfo.ThreadNumber.ToString();
                     var insert2 = connection2.ExecuteAsync(
                         SqlQueries.InsertIntoPointDataLatencyCountsTable(Enumerable
                             .Repeat(fakePointDataLatencyCounts, 120).ToArray()));
                     
                     fakePointDataStepStats.Time = DateTimeOffset.UtcNow;
                     fakePointDataStepStats.SessionId = context.ScenarioInfo.ThreadNumber.ToString();
                     var insert3 = connection3.ExecuteAsync(
                         SqlQueries.InsertIntoPointDataStepStatsTable(Enumerable.Repeat(fakePointDataStepStats, 120)
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
         
         /*var readScenario = Scenario.Create("read_scenario", async context =>
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
                             StartTime = DateTime.UtcNow.AddMinutes(-1),
                         });
                     
                     var dataStatusCodes = connection2.QueryAsync<PointDataStatusCodes>(
                         SqlQueries.SelectFromPointDataStatusCodesTable(), new
                         {
                             SessionId = context.ScenarioInfo.ThreadNumber.ToString(),
                             EndTime = DateTime.UtcNow,
                             StartTime = DateTime.UtcNow.AddMinutes(-1),
                         });
                     
                     var dataStepStats = connection3.QueryAsync<PointDataStepStats>(
                         SqlQueries.SelectFromPointDataStepStatsTable(), new
                         {
                             SessionId = context.ScenarioInfo.ThreadNumber.ToString(),
                             EndTime = DateTime.UtcNow,
                             StartTime = DateTime.UtcNow.AddMinutes(-1),
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
                 var startTimeLatencyCounts = DateTimeOffset.UtcNow.AddMinutes(-10);
                 var startTimeStatusCodes = DateTimeOffset.UtcNow.AddMinutes(-10);
                 var startTimeStepStats = DateTimeOffset.UtcNow.AddMinutes(-10);
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
                                 EndTime = DateTimeOffset.UtcNow,
                                 StartTime = startTimeLatencyCounts,
                             });
                     
                         var dataStatusCodes = connection2.QueryAsync<PointDataStatusCodes>(
                             SqlQueries.SelectFromPointDataStatusCodesTable(), new
                             {
                                 SessionId = context.ScenarioInfo.ThreadNumber.ToString(),
                                 EndTime = DateTimeOffset.UtcNow,
                                 StartTime = startTimeStatusCodes,
                             });
                     
                         var dataStepStats = connection3.QueryAsync<PointDataStepStats>(
                             SqlQueries.SelectFromPointDataStepStatsTable(), new
                             {
                                 SessionId = context.ScenarioInfo.ThreadNumber.ToString(),
                                 EndTime = DateTimeOffset.UtcNow,
                                 StartTime = startTimeStepStats,
                             });

                         await Task.WhenAll(dataLatencyCounts, dataStatusCodes, dataStepStats);

                         end = true;
                         
                         LatencyRes.AddRange(dataLatencyCounts.Result);
                         StepStatsRes.AddRange(dataStepStats.Result);
                         StatusCodesRes.AddRange(dataStatusCodes.Result);
                         
                         if (dataLatencyCounts.Result.Count() == 120)
                         {
                             startTimeLatencyCounts = dataLatencyCounts.Result.ToArray()[^1].Time;
                             end = false;
                         }
                     
                         if (dataStatusCodes.Result.Count() == 120)
                         {
                             startTimeStatusCodes = dataStatusCodes.Result.ToArray()[^1].Time;
                             end = false;
                         }
                     
                         if (dataStepStats.Result.Count() == 120)
                         {
                             startTimeStepStats = dataStepStats.Result.ToArray()[^1].Time;
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
                 
                 fakePointDataStatusCodes.Time = DateTimeOffset.UtcNow;
                 fakePointDataStatusCodes.SessionId = "0";
                 
                 fakePointDataLatencyCounts.Time = DateTimeOffset.UtcNow;
                 fakePointDataLatencyCounts.SessionId = "0";
                     
                 fakePointDataStepStats.Time = DateTimeOffset.UtcNow;
                 fakePointDataStepStats.SessionId = "0";

                 var fakeLatencyCountsPoints = Enumerable
                     .Repeat(fakePointDataLatencyCounts, 2160).ToArray();
                 for (var i = 1; i < 2160; i++)
                 {
                     fakeLatencyCountsPoints[i].Time = fakeLatencyCountsPoints[i - 1].Time.AddMinutes(-5);
                 }
                 
                 var fakeStatusCodesPoints = Enumerable
                     .Repeat(fakePointDataStatusCodes, 2160).ToArray();
                 for (var i = 1; i < 2160; i++)
                 {
                     fakeStatusCodesPoints[i].Time = fakeStatusCodesPoints[i - 1].Time.AddMinutes(-5);
                 }
                 
                 var fakeStepStatsPoints = Enumerable
                     .Repeat(fakePointDataStepStats, 2160).ToArray();
                 for (var i = 1; i < 2160; i++)
                 {
                     fakeStepStatsPoints[i].Time = fakeStepStatsPoints[i - 1].Time.AddMinutes(-5);
                 }
                 
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