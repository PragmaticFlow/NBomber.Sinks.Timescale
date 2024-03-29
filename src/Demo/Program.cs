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
                     await using var connection = new NpgsqlConnection(
                             "Host=localhost;Port=5432;Database=timescaledb;Username=andrii;Password=myPass;Pooling=true;");
                     
                     fakePointDataStatusCodes.Time = DateTimeOffset.UtcNow;
                     fakePointDataStatusCodes.SessionId = context.ScenarioInfo.ThreadNumber.ToString();
                     await connection.ExecuteAsync(
                         SqlQueries.InsertIntoPointDataStatusCodesTable([fakePointDataStatusCodes]));
                     
                     fakePointDataLatencyCounts.Time = DateTimeOffset.UtcNow;
                     fakePointDataLatencyCounts.SessionId = context.ScenarioInfo.ThreadNumber.ToString();;
                     await connection.ExecuteAsync(
                         SqlQueries.InsertIntoPointDataLatencyCountsTable([fakePointDataLatencyCounts]));
                     
                     fakePointDataStepStats.Time = DateTimeOffset.UtcNow;
                     fakePointDataStepStats.SessionId = context.ScenarioInfo.ThreadNumber.ToString();;
                     await connection.ExecuteAsync(
                         SqlQueries.InsertIntoPointDataStepStatsTable([fakePointDataStepStats]));
                     
                     return Response.Ok();
                 });

                 var step2 = await Step.Run("pause", context, async () =>
                 {
                     await Task.Delay(TimeSpan.FromSeconds(random.Next(3, 7)));

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
                 Simulation.KeepConstant(1, TimeSpan.FromSeconds(30))
             ).WithoutWarmUp();
         
         var readScenario = Scenario.Create("read_scenario", async context =>
             {
                  var step1 = await Step.Run("read", context, async () =>
                 {
                     await using var connection = new NpgsqlConnection(
                             "Host=localhost;Port=5432;Database=timescaledb;Username=andrii;Password=myPass;Pooling=true;");

                     var dataLatencyCounts = await connection.QueryAsync<PointDataLatencyCounts>(
                         SqlQueries.SelectFromPointDataLatencyCountsTable(), new
                         {
                             SessionId = context.ScenarioInfo.ThreadNumber.ToString(),
                             EndTime = DateTime.UtcNow,
                             StartTime = DateTime.UtcNow.AddMinutes(-1),
                         });
                     
                     var dataStatusCodes = await connection.QueryAsync<PointDataStatusCodes>(
                         SqlQueries.SelectFromPointDataStatusCodesTable(), new
                         {
                             SessionId = context.ScenarioInfo.ThreadNumber.ToString(),
                             EndTime = DateTime.UtcNow,
                             StartTime = DateTime.UtcNow.AddMinutes(-1),
                         });
                     
                     
                     var dataStepStats = await connection.QueryAsync<PointDataStepStats>(
                         SqlQueries.SelectFromPointDataStepStatsTable(), new
                         {
                             SessionId = context.ScenarioInfo.ThreadNumber.ToString(),
                             EndTime = DateTime.UtcNow,
                             StartTime = DateTime.UtcNow.AddMinutes(-1),
                         });
                     
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
                 Simulation.KeepConstant(1, TimeSpan.FromSeconds(30))
             ).WithoutWarmUp();

         NBomberRunner
             .RegisterScenarios(writeScenario, readScenario)
             //.LoadInfraConfig("infra-config.json")
             //.WithReportingInterval(TimeSpan.FromSeconds(5))
             //.WithReportingSinks(_timescaleDbSink)
             .WithTestSuite("reporting")
             .WithTestName("timescale_db_demo")
             .Run();
     }
 }