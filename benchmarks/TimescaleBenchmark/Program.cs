﻿using TimescaleBenchmark;
using Npgsql;
using RepoDb;
using NBomber.CSharp;
using NBomber.Sinks.Timescale.DAL;

new TimescaleDBReportingExample().Run();

public class TimescaleDBReportingExample
{
     private const string CleanDbSql = $"""
         DROP TABLE IF EXISTS {TableNames.StepStatsTable};
         DROP TABLE IF EXISTS {TableNames.SessionsTable};
     """;
    
    public void Run()
    {
        GlobalConfiguration
            .Setup()
            .UsePostgreSql();

        const string connectionString = "Host=localhost;Port=5432;Database=metricsdb;Username=timescaledb;Password=timescaledb;Pooling=true;Maximum Pool Size=300;";

        using var connection = new NpgsqlConnection(connectionString);

        connection.ExecuteNonQuery(CleanDbSql);
        
        connection.ExecuteNonQuery(SqlQueries.CreateStepStatsTable + SqlQueries.CreateSessionsTable + SqlQueries.CreateDbSchemaVersion);
        
        var writeScenario = new WriteScenario().Create(connectionString);
        var readScenario = new ReadScenario().Create(connectionString, DateTime.UtcNow, sessionId: "0");
        
        NBomberRunner
            .RegisterScenarios(writeScenario)
            .WithTestSuite("reporting")
            .WithTestName("timescale_db_demo")
            .Run();
    }
}