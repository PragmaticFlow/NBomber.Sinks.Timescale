// using AutoBogus;
// using Dapper;
// using Npgsql;
// using RepoDb;
// using NBomber.Contracts;
// using NBomber.CSharp;
// using NBomber.Sinks.Timescale;
// using NBomber.Sinks.Timescale.Contracts;
//
// namespace TimescaleBenchmark;
//
// public class WriteScenario
// {
//     public ScenarioProps Create(string connectionString)
//     {
//         PointLatencyCounts fakePointLatencyCounts = new();
//         PointStatusCodes fakePointStatusCodes = new();
//         PointStepStatsOk fakePointStepStatsOk = new();
//         
//         return Scenario.Create("write_scenario", async ctx =>
//         {
//             var step = await Step.Run("write", ctx, async () =>
//             {
//                 await using var connection1 = new NpgsqlConnection(connectionString);
//                 await using var connection2 = new NpgsqlConnection(connectionString);
//                 await using var connection3 = new NpgsqlConnection(connectionString);
//             
//                 var curTime = DateTimeOffset.UtcNow;
//              
//                 fakePointStatusCodes.Time = curTime;
//                 fakePointStatusCodes.SessionId = ctx.ScenarioInfo.InstanceNumber.ToString();
//                 var insert1 = connection1.BinaryBulkInsertAsync(SqlQueries.StatusCodesTableName, Enumerable.Repeat(fakePointStatusCodes, 5));
//                 //await connection1.BinaryBulkInsertAsync(SqlQueries.StatusCodesTableName, Enumerable.Repeat(fakePointStatusCodes, 5));
//                 
//                 fakePointLatencyCounts.Time = curTime;
//                 fakePointLatencyCounts.SessionId = ctx.ScenarioInfo.InstanceNumber.ToString();
//                 var insert2 = connection2.BinaryBulkInsertAsync(SqlQueries.LatencyCountsTableName, Enumerable.Repeat(fakePointLatencyCounts, 5));
//                 //await connection1.BinaryBulkInsertAsync(SqlQueries.LatencyCountsTableName, Enumerable.Repeat(fakePointLatencyCounts, 5));
//             
//                 fakePointStepStatsOk.Time = curTime;
//                 fakePointStepStatsOk.SessionId = ctx.ScenarioInfo.InstanceNumber.ToString();
//                 var insert3 = connection3.BinaryBulkInsertAsync(SqlQueries.StepStatsOkTableName, Enumerable.Repeat(fakePointStepStatsOk, 5));
//                 //await connection1.BinaryBulkInsertAsync(SqlQueries.StepStatsOkTableName, Enumerable.Repeat(fakePointStepStatsOk, 5));
//
//                 await Task.WhenAll(insert1, insert2, insert3);
//                 
//                 return Response.Ok();
//             });
//
//             await Task.Delay(TimeSpan.FromSeconds(5));
//             
//             return Response.Ok();
//         })
//         .WithInit(async ctx =>
//         {
//             await using var connection = new NpgsqlConnection(connectionString);
//             
//             await connection.ExecuteAsync(SqlQueries.CreateStatusCodesTable
//                                           + SqlQueries.CreateLatencyCountsTable 
//                                           + SqlQueries.CreateStepStatsOkTable);
//             
//             var faker = AutoFaker.Create();
//             
//             fakePointLatencyCounts = faker.Generate<PointLatencyCounts>();
//             fakePointStatusCodes = faker.Generate<PointStatusCodes>();
//             fakePointStepStatsOk = faker.Generate<PointStepStatsOk>();
//         })
//         .WithWarmUpDuration(TimeSpan.FromSeconds(3))
//         .WithLoadSimulations(
//             Simulation.RampingConstant(2000, TimeSpan.FromSeconds(30)),
//             Simulation.KeepConstant(2000, TimeSpan.FromMinutes(1))
//         );
//     }
// }