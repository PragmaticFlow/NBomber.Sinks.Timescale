using NBomber.Sinks.Timescale.Models;

namespace NBomber.Sinks.Timescale;

public class SqlQueries
{
     public static string CreatePointDataLatencyCountsTable => $@"
        CREATE TABLE IF NOT EXISTS latency_counts_points
        (
            {nameof(PointDataBase.Measurement)} TEXT,
            {nameof(PointDataBase.Time)} TIMESTAMPTZ,
            {nameof(PointDataBase.SessionId)} TEXT,
            {nameof(PointDataBase.CurrentOperation)} TEXT,
            {nameof(PointDataBase.NodeType)} TEXT,
            {nameof(PointDataBase.TestSuite)} TEXT,
            {nameof(PointDataBase.TestName)} TEXT,
            {nameof(PointDataBase.ClusterId)} TEXT,
            {nameof(PointDataLatencyCounts.Scenario)} TEXT,
            {nameof(PointDataLatencyCounts.LatencyCountLessOrEq800)} INT,
            {nameof(PointDataLatencyCounts.LatencyCountMore800Less1200)} INT,
            {nameof(PointDataLatencyCounts.LatencyCountMoreOrEq1200)} INT
        );";

    public static string CreatePointDataStartTable => $@"
        CREATE TABLE IF NOT EXISTS start_points
        (
            {nameof(PointDataBase.Measurement)} TEXT,
            {nameof(PointDataBase.Time)} TIMESTAMPTZ,
            {nameof(PointDataBase.SessionId)} TEXT,
            {nameof(PointDataBase.CurrentOperation)} TEXT,
            {nameof(PointDataBase.NodeType)} TEXT,
            {nameof(PointDataBase.TestSuite)} TEXT,
            {nameof(PointDataBase.TestName)} TEXT,
            {nameof(PointDataBase.ClusterId)} TEXT,
            {nameof(PointDataStart.ClusterNodeCount)} INT,
            {nameof(PointDataStart.ClusterNodeCpuCount)} INT
        );";

    public static string CreatePointDataStatusCodesTable => $@"
        CREATE TABLE IF NOT EXISTS status_codes_points
        (
            {nameof(PointDataBase.Measurement)} TEXT,
            {nameof(PointDataBase.Time)} TIMESTAMPTZ,
            {nameof(PointDataBase.SessionId)} TEXT,
            {nameof(PointDataBase.CurrentOperation)} TEXT,
            {nameof(PointDataBase.NodeType)} TEXT,
            {nameof(PointDataBase.TestSuite)} TEXT,
            {nameof(PointDataBase.TestName)} TEXT,
            {nameof(PointDataBase.ClusterId)} TEXT,
            {nameof(PointDataStatusCodes.Scenario)} TEXT,
            {nameof(PointDataStatusCodes.StatusCodeStatus)} TEXT,
            {nameof(PointDataStatusCodes.StatusCodeCount)} INT
        );";
    
    public static string CreatePointDataStepStatsTable => $@"
    CREATE TABLE IF NOT EXISTS step_stats_points
    (
        {nameof(PointDataBase.Measurement)} TEXT,
        {nameof(PointDataBase.Time)} TIMESTAMPTZ,
        {nameof(PointDataBase.SessionId)} TEXT,
        {nameof(PointDataBase.CurrentOperation)} TEXT,
        {nameof(PointDataBase.NodeType)} TEXT,
        {nameof(PointDataBase.TestSuite)} TEXT,
        {nameof(PointDataBase.TestName)} TEXT,
        {nameof(PointDataBase.ClusterId)} TEXT,
        {nameof(PointDataStepStats.Scenario)} TEXT,
        {nameof(PointDataStepStats.AllRequestCount)} INT,
        {nameof(PointDataStepStats.AllDataTransferAll)} BIGINT,
        {nameof(PointDataStepStats.OkRequestCount)} INT,
        {nameof(PointDataStepStats.OkRequestRps)} DOUBLE PRECISION,
        {nameof(PointDataStepStats.OkLatencyMax)} DOUBLE PRECISION,
        {nameof(PointDataStepStats.OkLatencyMean)} DOUBLE PRECISION,
        {nameof(PointDataStepStats.OkLatencyMin)} DOUBLE PRECISION,
        {nameof(PointDataStepStats.OkLatencyStdDev)} DOUBLE PRECISION,
        {nameof(PointDataStepStats.OkLatencyPercent50)} DOUBLE PRECISION,
        {nameof(PointDataStepStats.OkLatencyPercent75)} DOUBLE PRECISION,
        {nameof(PointDataStepStats.OkLatencyPercent95)} DOUBLE PRECISION,
        {nameof(PointDataStepStats.OkLatencyPercent99)} DOUBLE PRECISION,
        {nameof(PointDataStepStats.OkDataTransferMin)} BIGINT,
        {nameof(PointDataStepStats.OkDataTransferMean)} BIGINT,
        {nameof(PointDataStepStats.OkDataTransferMax)} BIGINT,
        {nameof(PointDataStepStats.OkDataTransferAll)} BIGINT,
        {nameof(PointDataStepStats.OkDataTransferPercent50)} BIGINT,
        {nameof(PointDataStepStats.OkDataTransferPercent75)} BIGINT,
        {nameof(PointDataStepStats.OkDataTransferPercent95)} BIGINT,
        {nameof(PointDataStepStats.OkDataTransferPercent99)} BIGINT,
        {nameof(PointDataStepStats.FailRequestCount)} INT,
        {nameof(PointDataStepStats.FailRequestRps)} DOUBLE PRECISION,
        {nameof(PointDataStepStats.FailLatencyMax)} DOUBLE PRECISION,
        {nameof(PointDataStepStats.FailLatencyMean)} DOUBLE PRECISION,
        {nameof(PointDataStepStats.FailLatencyMin)} DOUBLE PRECISION,
        {nameof(PointDataStepStats.FailLatencyStdDev)} DOUBLE PRECISION,
        {nameof(PointDataStepStats.FailLatencyPercent50)} DOUBLE PRECISION,
        {nameof(PointDataStepStats.FailLatencyPercent75)} DOUBLE PRECISION,
        {nameof(PointDataStepStats.FailLatencyPercent95)} DOUBLE PRECISION,
        {nameof(PointDataStepStats.FailLatencyPercent99)} DOUBLE PRECISION,
        {nameof(PointDataStepStats.FailDataTransferMin)} BIGINT,
        {nameof(PointDataStepStats.FailDataTransferMean)} BIGINT,
        {nameof(PointDataStepStats.FailDataTransferMax)} BIGINT,
        {nameof(PointDataStepStats.FailDataTransferAll)} BIGINT,
        {nameof(PointDataStepStats.FailDataTransferPercent50)} BIGINT,
        {nameof(PointDataStepStats.FailDataTransferPercent75)} BIGINT,
        {nameof(PointDataStepStats.FailDataTransferPercent95)} BIGINT,
        {nameof(PointDataStepStats.FailDataTransferPercent99)} BIGINT,
        {nameof(PointDataStepStats.SimulationValue)} INT
    );";

}