using NBomber.Sinks.Timescale.Contracts;

namespace NBomber.Sinks.Timescale;

public static class SqlQueries
{
    public const string StepStatsOkTableName = "step_stats_ok";
    public const string StepStatsFailTableName = "step_stats_fail";
    public const string StatusCodesTableName = "status_codes";
    public const string LatencyCountsTableName = "latency_counts";
    public const string ClusterStatsTableName = "cluster_stats";
    
    public static string CreateLatencyCountsTable => $@"
        CREATE TABLE IF NOT EXISTS ""{LatencyCountsTableName}""
        (            
            ""{ColumnNames.Time}"" TIMESTAMPTZ NOT NULL,
            ""{ColumnNames.SessionId}"" TEXT NOT NULL,
            ""{ColumnNames.CurrentOperation}"" TEXT,
            ""{ColumnNames.NodeType}"" TEXT,
            ""{ColumnNames.TestSuite}"" TEXT,
            ""{ColumnNames.TestName}"" TEXT,
            ""{ColumnNames.ClusterId}"" TEXT,
            ""{ColumnNames.Scenario}"" TEXT,
            ""{ColumnNames.LessOrEq800}"" INT,
            ""{ColumnNames.More800Less1200}"" INT,
            ""{ColumnNames.MoreOrEq1200}"" INT
        );
        SELECT create_hypertable('{LatencyCountsTableName}', by_range('{ColumnNames.Time}', INTERVAL '1 day'), if_not_exists => TRUE);
        CREATE INDEX IF NOT EXISTS {ColumnNames.SessionId}_index ON {LatencyCountsTableName} ({ColumnNames.SessionId}, {ColumnNames.Time} DESC);
   ";
    
    public static string CreateClusterStatsTable => $@"
        CREATE TABLE IF NOT EXISTS ""{ClusterStatsTableName}""
        (            
            ""{ColumnNames.Time}"" TIMESTAMPTZ NOT NULL,
            ""{ColumnNames.SessionId}"" TEXT NOT NULL,
            ""{ColumnNames.CurrentOperation}"" TEXT,
            ""{ColumnNames.NodeType}"" TEXT,
            ""{ColumnNames.TestSuite}"" TEXT,
            ""{ColumnNames.TestName}"" TEXT,
            ""{ColumnNames.ClusterId}"" TEXT,
            ""{ColumnNames.NodeCount}"" INT,
            ""{ColumnNames.NodeCpuCount}"" INT
        );
        SELECT create_hypertable('{ClusterStatsTableName}', by_range('{ColumnNames.Time}', INTERVAL '1 day'), if_not_exists => TRUE);
        CREATE INDEX IF NOT EXISTS {ColumnNames.SessionId}_index ON {ClusterStatsTableName} ({ColumnNames.SessionId}, {ColumnNames.Time} DESC);
       ";
    
    public static string CreateStatusCodesTable => $@"
        CREATE TABLE IF NOT EXISTS ""{StatusCodesTableName}""
        (
            ""{ColumnNames.Time}"" TIMESTAMPTZ NOT NULL,
            ""{ColumnNames.SessionId}"" TEXT NOT NULL,
            ""{ColumnNames.CurrentOperation}"" TEXT,
            ""{ColumnNames.NodeType}"" TEXT,
            ""{ColumnNames.TestSuite}"" TEXT,
            ""{ColumnNames.TestName}"" TEXT,
            ""{ColumnNames.ClusterId}"" TEXT,
            ""{ColumnNames.Scenario}"" TEXT,
            ""{ColumnNames.StatusCode}"" TEXT,
            ""{ColumnNames.Count}"" INT
        );
        SELECT create_hypertable('{StatusCodesTableName}', by_range('{ColumnNames.Time}', INTERVAL '1 day'), if_not_exists => TRUE);
        CREATE INDEX IF NOT EXISTS {ColumnNames.SessionId}_index ON {StatusCodesTableName} ({ColumnNames.SessionId}, {ColumnNames.Time} DESC);
       ";

    public static string CreateStepStatsOkTable => $@"
        CREATE TABLE IF NOT EXISTS ""{StepStatsOkTableName}""
        (
            ""{ColumnNames.Time}"" TIMESTAMPTZ NOT NULL,
            ""{ColumnNames.SessionId}"" TEXT NOT NULL,
            ""{ColumnNames.CurrentOperation}"" TEXT,
            ""{ColumnNames.NodeType}"" TEXT,
            ""{ColumnNames.TestSuite}"" TEXT,
            ""{ColumnNames.TestName}"" TEXT,
            ""{ColumnNames.ClusterId}"" TEXT,
            ""{ColumnNames.Step}"" TEXT,
            ""{ColumnNames.Scenario}"" TEXT,
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
            ""{ColumnNames.FailReqCount}"" INT,
            ""{ColumnNames.FailReqRps}"" DOUBLE PRECISION,            
            ""{ColumnNames.SimulationValue}"" INT
        );
        SELECT create_hypertable('{StepStatsOkTableName}', by_range('{ColumnNames.Time}', INTERVAL '1 day'), if_not_exists => TRUE);
        CREATE INDEX IF NOT EXISTS {ColumnNames.SessionId}_index ON {StepStatsOkTableName} ({ColumnNames.SessionId}, {ColumnNames.Time} DESC);
   ";
    
    public static string CreateStepStatsFailTable => $@"
        CREATE TABLE IF NOT EXISTS ""{StepStatsFailTableName}""
        (
            ""{ColumnNames.Time}"" TIMESTAMPTZ NOT NULL,
            ""{ColumnNames.SessionId}"" TEXT NOT NULL,
            ""{ColumnNames.CurrentOperation}"" TEXT,
            ""{ColumnNames.NodeType}"" TEXT,
            ""{ColumnNames.TestSuite}"" TEXT,
            ""{ColumnNames.TestName}"" TEXT,
            ""{ColumnNames.ClusterId}"" TEXT,
            ""{ColumnNames.Step}"" TEXT,
            ""{ColumnNames.Scenario}"" TEXT,            
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
            ""{ColumnNames.FailDataP99}"" BIGINT            
        );
        SELECT create_hypertable('{StepStatsFailTableName}', by_range('{ColumnNames.Time}', INTERVAL '1 day'), if_not_exists => TRUE);
        CREATE INDEX IF NOT EXISTS {ColumnNames.SessionId}_index ON {StepStatsFailTableName} ({ColumnNames.SessionId}, {ColumnNames.Time} DESC);
   ";
}   