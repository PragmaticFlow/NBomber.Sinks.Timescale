#pragma warning disable CS1591

using NBomber.Sinks.Timescale.Contracts;

namespace NBomber.Sinks.Timescale;

public static class SqlQueries
{
    public const string StepStatsTable = "step_stats";
    public const string ClusterStatsTableName = "cluster_stats";
    
    public static string CreateStepStatsTable => $@"
        CREATE TABLE IF NOT EXISTS ""{StepStatsTable}""
        (
            ""{ColumnNames.Time}"" TIMESTAMPTZ NOT NULL,
            ""{ColumnNames.SessionId}"" TEXT NOT NULL,
            ""{ColumnNames.CurrentOperation}"" TEXT NOT NULL,            
            ""{ColumnNames.TestSuite}"" TEXT NOT NULL,
            ""{ColumnNames.TestName}"" TEXT NOT NULL,
            ""{ColumnNames.Scenario}"" TEXT NOT NULL,
            ""{ColumnNames.Step}"" TEXT NOT NULL,            
            ""{ColumnNames.OkStepStats}"" JSONB NOT NULL,
            ""{ColumnNames.FailStepStats}"" JSONB NOT NULL
        );
        SELECT create_hypertable('{StepStatsTable}', by_range('{ColumnNames.Time}', INTERVAL '1 day'), if_not_exists => TRUE);
        CREATE INDEX IF NOT EXISTS {ColumnNames.SessionId}_index ON {StepStatsTable} ({ColumnNames.SessionId});
   ";
    
    public static string CreateClusterStatsTable => $@"
        CREATE TABLE IF NOT EXISTS ""{ClusterStatsTableName}""
        (            
            ""{ColumnNames.Time}"" TIMESTAMPTZ NOT NULL,
            ""{ColumnNames.SessionId}"" TEXT NOT NULL,            
            ""{ColumnNames.NodeInfo}"" JSONB NOT NULL            
        );
        SELECT create_hypertable('{ClusterStatsTableName}', by_range('{ColumnNames.Time}', INTERVAL '1 day'), if_not_exists => TRUE);
        CREATE INDEX IF NOT EXISTS {ColumnNames.SessionId}_index ON {ClusterStatsTableName} ({ColumnNames.SessionId});
       ";
}   