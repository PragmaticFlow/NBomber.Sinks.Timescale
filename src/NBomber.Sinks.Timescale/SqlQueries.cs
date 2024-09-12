#pragma warning disable CS1591

using NBomber.Sinks.Timescale.Contracts;

namespace NBomber.Sinks.Timescale;

public static class SqlQueries
{
    public const string StepStatsTable = "nb_step_stats";
    public const string SessionsTable = "nb_sessions";
    public const string DbSchemaVersion = "nb_sink_schema_version";
    public static string CreateStepStatsTable => $@"
        CREATE TABLE IF NOT EXISTS ""{StepStatsTable}""
        (
            ""{ColumnNames.Time}"" TIMESTAMPTZ NOT NULL,
            ""{ColumnNames.ScenarioTimestamp}"" TIME WITHOUT TIME ZONE NOT NULL,
            ""{ColumnNames.SessionId}"" TEXT NOT NULL,
            ""{ColumnNames.CurrentOperation}"" TEXT,            
            
            ""{ColumnNames.Scenario}"" TEXT,
            ""{ColumnNames.Step}"" TEXT,            
            
            ""{ColumnNames.AllReqCount}"" INT,
            ""{ColumnNames.AllDataAll}"" BIGINT,
            
            ""{ColumnNames.OkReqCount}"" INT,
            ""{ColumnNames.OkReqRps}"" DOUBLE PRECISION,
            ""{ColumnNames.OkLatencyMax}"" DOUBLE PRECISION,
            ""{ColumnNames.OkLatencyMean}"" DOUBLE PRECISION,
            ""{ColumnNames.OkLatencyMin}"" DOUBLE PRECISION,
            ""{ColumnNames.OkLatencyStdDev}"" DOUBLE PRECISION,
            ""{ColumnNames.OkLatencyP50}"" DOUBLE PRECISION,
            ""{ColumnNames.OkLatencyP75}"" DOUBLE PRECISION,
            ""{ColumnNames.OkLatencyP95}"" DOUBLE PRECISION,
            ""{ColumnNames.OkLatencyP99}"" DOUBLE PRECISION,
            ""{ColumnNames.OkDataMin}"" BIGINT,
            ""{ColumnNames.OkDataMean}"" BIGINT,
            ""{ColumnNames.OkDataMax}"" BIGINT,
            ""{ColumnNames.OkDataAll}"" BIGINT,
            ""{ColumnNames.OkDataP50}"" BIGINT,
            ""{ColumnNames.OkDataP75}"" BIGINT,
            ""{ColumnNames.OkDataP95}"" BIGINT,
            ""{ColumnNames.OkDataP99}"" BIGINT,
            ""{ColumnNames.OkStatusCodes}"" JSONB,
            ""{ColumnNames.OkLatencyCount}"" JSONB,            
            
            ""{ColumnNames.FailReqCount}"" INT,
            ""{ColumnNames.FailReqRps}"" DOUBLE PRECISION, 
            ""{ColumnNames.FailLatencyMax}"" DOUBLE PRECISION,
            ""{ColumnNames.FailLatencyMean}"" DOUBLE PRECISION,
            ""{ColumnNames.FailLatencyMin}"" DOUBLE PRECISION,
            ""{ColumnNames.FailLatencyStdDev}"" DOUBLE PRECISION,
            ""{ColumnNames.FailLatencyP50}"" DOUBLE PRECISION,
            ""{ColumnNames.FailLatencyP75}"" DOUBLE PRECISION,
            ""{ColumnNames.FailLatencyP95}"" DOUBLE PRECISION,
            ""{ColumnNames.FailLatencyP99}"" DOUBLE PRECISION,
            ""{ColumnNames.FailDataMin}"" BIGINT,
            ""{ColumnNames.FailDataMean}"" BIGINT,
            ""{ColumnNames.FailDataMax}"" BIGINT,
            ""{ColumnNames.FailDataAll}"" BIGINT,
            ""{ColumnNames.FailDataP50}"" BIGINT,
            ""{ColumnNames.FailDataP75}"" BIGINT,
            ""{ColumnNames.FailDataP95}"" BIGINT,
            ""{ColumnNames.FailDataP99}"" BIGINT,
            ""{ColumnNames.FailStatusCodes}"" JSONB,
            ""{ColumnNames.FailLatencyCount}"" JSONB,
            
            ""{ColumnNames.SimulationValue}"" INT
        );
        SELECT create_hypertable('{StepStatsTable}', by_range('{ColumnNames.Time}', INTERVAL '1 day'), if_not_exists => TRUE);
        CREATE INDEX IF NOT EXISTS {ColumnNames.SessionId}_index ON {StepStatsTable} ({ColumnNames.SessionId});
   ";
    
    public static string CreateSessionsTable => $@"
        CREATE TABLE IF NOT EXISTS ""{SessionsTable}""
        (            
            ""{ColumnNames.Time}"" TIMESTAMPTZ NOT NULL,
            ""{ColumnNames.SessionId}"" TEXT PRIMARY KEY,
            ""{ColumnNames.CurrentOperation}"" TEXT, 
            ""{ColumnNames.TestSuite}"" TEXT,
            ""{ColumnNames.TestName}"" TEXT,
            ""{ColumnNames.NodeInfo}"" JSONB            
        );    
       ";

    public static string CreateDbSchemaVersion => $@"
        CREATE TABLE IF NOT EXISTS ""{DbSchemaVersion}""
        (            
            ""{ColumnNames.Version}"" INT PRIMARY KEY         
        );    
       ";
}