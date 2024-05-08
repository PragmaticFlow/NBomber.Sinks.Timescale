﻿using AutoBogus;
using Dapper;
using NBomber.CSharp;
using NBomber.Sinks.Timescale;
using NBomber.Sinks.Timescale.Contracts;
using Npgsql;
using RepoDb;
using TimescaleBenchmark;

new TimescaleDBReportingExample().Run();

public class TimescaleDBReportingExample
{
    private string CleanDbSql = $"""
        DROP TABLE IF EXISTS {SqlQueries.StatusCodesTableName};
        DROP TABLE IF EXISTS {SqlQueries.LatencyCountsTableName};
        DROP TABLE IF EXISTS {SqlQueries.StepStatsOkTableName};
    """;
    
    public void Run()
    {
        GlobalConfiguration
            .Setup()
            .UsePostgreSql();

        var connectionString = "Host=localhost;Port=5432;Database=timescaledb;Username=timescaledb;Password=timescaledb;Pooling=true;Maximum Pool Size=300;";
        
        using var connection = new NpgsqlConnection(connectionString);

        connection.ExecuteNonQuery(CleanDbSql);
        
        connection.ExecuteNonQuery(
            SqlQueries.CreateLatencyCountsTable 
            + SqlQueries.CreateStatusCodesTable
            + SqlQueries.CreateStepStatsOkTable);

        var writeScenario = new WriteScenario().Create(connectionString);
        var readScenario = new ReadScenario().Create(connectionString, DateTimeOffset.UtcNow, sessionId: "0");
        
        NBomberRunner
            .RegisterScenarios(writeScenario)
            .WithTestSuite("reporting")
            .WithTestName("timescale_db_demo")
            .Run();
    }
}