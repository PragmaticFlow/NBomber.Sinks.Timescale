using System.Globalization;
using System.Text;
using NBomber.Sinks.Timescale.Models;

namespace NBomber.Sinks.Timescale;

public static class SqlQueries
{
    public static string CreatePointDataLatencyCountsTable => $@"
        CREATE TABLE IF NOT EXISTS latency_counts_points
        (
            ""{nameof(PointDataBase.Measurement)}"" TEXT,
            ""{nameof(PointDataBase.Time)}"" TIMESTAMPTZ,
            ""{nameof(PointDataBase.SessionId)}"" TEXT,
            ""{nameof(PointDataBase.CurrentOperation)}"" TEXT,
            ""{nameof(PointDataBase.NodeType)}"" TEXT,
            ""{nameof(PointDataBase.TestSuite)}"" TEXT,
            ""{nameof(PointDataBase.TestName)}"" TEXT,
            ""{nameof(PointDataBase.ClusterId)}"" TEXT,
            ""{nameof(PointDataLatencyCounts.Scenario)}"" TEXT,
            ""{nameof(PointDataLatencyCounts.LatencyCountLessOrEq800)}"" INT,
            ""{nameof(PointDataLatencyCounts.LatencyCountMore800Less1200)}"" INT,
            ""{nameof(PointDataLatencyCounts.LatencyCountMoreOrEq1200)}"" INT
        );
    SELECT create_hypertable('latency_counts_points', by_range('{nameof(PointDataBase.Time).ToLower()}', INTERVAL '1 day'), if_not_exists => TRUE);
    CREATE INDEX IF NOT EXISTS {nameof(PointDataBase.SessionId)}_index ON latency_counts_points ({nameof(PointDataBase.SessionId)}, {nameof(PointDataBase.Time)} DESC);";

    public static string InsertIntoPointDataLatencyCountsTable(PointDataLatencyCounts[] points) 
    {
        var insertQuery = new StringBuilder();
        insertQuery.AppendLine($@"
        INSERT INTO latency_counts_points
        (
            {nameof(PointDataBase.Measurement)},
            {nameof(PointDataBase.Time)},
            {nameof(PointDataBase.SessionId)},
            {nameof(PointDataBase.CurrentOperation)},
            {nameof(PointDataBase.NodeType)},
            {nameof(PointDataBase.TestSuite)},
            {nameof(PointDataBase.TestName)},
            {nameof(PointDataBase.ClusterId)},
            {nameof(PointDataLatencyCounts.Scenario)},
            {nameof(PointDataLatencyCounts.LatencyCountLessOrEq800)},
            {nameof(PointDataLatencyCounts.LatencyCountMore800Less1200)},
            {nameof(PointDataLatencyCounts.LatencyCountMoreOrEq1200)}
        )
        VALUES ");

        for (var i = 0; i < points.Length; i++)
        {
            var point = points[i];
            insertQuery.Append(@$"
            ('{point.Measurement}',
            '{point.Time:yyyy-MM-dd HH:mm:ss.fff zzz}',
            '{point.SessionId}',
            '{point.CurrentOperation}',
            '{point.NodeType}',
            '{point.TestSuite}',
            '{point.TestName}',
            '{point.ClusterId}',
            '{point.Scenario}',
            {point.LatencyCountLessOrEq800.ToString(CultureInfo.InvariantCulture)},
            {point.LatencyCountMore800Less1200.ToString(CultureInfo.InvariantCulture)},
            {point.LatencyCountMoreOrEq1200.ToString(CultureInfo.InvariantCulture)})");

            insertQuery.AppendLine(i < points.Length - 1 ? "," : ";");
        } 
    
        return insertQuery.ToString();
    }

    public static string SelectFromPointDataLatencyCountsTable()
    {
        return @"
        SELECT * FROM latency_counts_points
        WHERE sessionid = @SessionId
        AND time >= @StartTime
        AND time <= @EndTime
        ORDER BY Time DESC
        LIMIT 120;
        ";
    }
    
    public static string CreatePointDataStartTable => $@"
        CREATE TABLE IF NOT EXISTS start_points
        (
            ""{nameof(PointDataBase.Measurement)}"" TEXT,
            ""{nameof(PointDataBase.Time)}"" TIMESTAMPTZ,
            ""{nameof(PointDataBase.SessionId)}"" TEXT,
            ""{nameof(PointDataBase.CurrentOperation)}"" TEXT,
            ""{nameof(PointDataBase.NodeType)}"" TEXT,
            ""{nameof(PointDataBase.TestSuite)}"" TEXT,
            ""{nameof(PointDataBase.TestName)}"" TEXT,
            ""{nameof(PointDataBase.ClusterId)}"" TEXT,
            ""{nameof(PointDataStart.ClusterNodeCount)}"" INT,
            ""{nameof(PointDataStart.ClusterNodeCpuCount)}"" INT
        );
        SELECT create_hypertable('start_points', by_range('{nameof(PointDataBase.Time).ToLower()}', INTERVAL '1 day'), if_not_exists => TRUE);
        CREATE INDEX IF NOT EXISTS {nameof(PointDataBase.SessionId)}_index ON start_points ({nameof(PointDataBase.SessionId)}, {nameof(PointDataBase.Time)} DESC);";

    public static string InsertIntoPointDataStartTable => $@"
    INSERT INTO start_points
    (
        {nameof(PointDataBase.Measurement)},
        {nameof(PointDataBase.Time)},
        {nameof(PointDataBase.SessionId)},
        {nameof(PointDataBase.CurrentOperation)},
        {nameof(PointDataBase.NodeType)},
        {nameof(PointDataBase.TestSuite)},
        {nameof(PointDataBase.TestName)},
        {nameof(PointDataBase.ClusterId)},
        {nameof(PointDataStart.ClusterNodeCount)},
        {nameof(PointDataStart.ClusterNodeCpuCount)}
    )
    VALUES
    (
        @{nameof(PointDataBase.Measurement)},
        @{nameof(PointDataBase.Time)},
        @{nameof(PointDataBase.SessionId)},
        @{nameof(PointDataBase.CurrentOperation)},
        @{nameof(PointDataBase.NodeType)},
        @{nameof(PointDataBase.TestSuite)},
        @{nameof(PointDataBase.TestName)},
        @{nameof(PointDataBase.ClusterId)},
        @{nameof(PointDataStart.ClusterNodeCount)},
        @{nameof(PointDataStart.ClusterNodeCpuCount)}
    );";
    
    public static string CreatePointDataStatusCodesTable => $@"
        CREATE TABLE IF NOT EXISTS status_codes_points
        (
            ""{nameof(PointDataBase.Measurement)}"" TEXT,
            ""{nameof(PointDataBase.Time)}"" TIMESTAMPTZ,
            ""{nameof(PointDataBase.SessionId)}"" TEXT,
            ""{nameof(PointDataBase.CurrentOperation)}"" TEXT,
            ""{nameof(PointDataBase.NodeType)}"" TEXT,
            ""{nameof(PointDataBase.TestSuite)}"" TEXT,
            ""{nameof(PointDataBase.TestName)}"" TEXT,
            ""{nameof(PointDataBase.ClusterId)}"" TEXT,
            ""{nameof(PointDataStatusCodes.Scenario)}"" TEXT,
            ""{nameof(PointDataStatusCodes.StatusCodeStatus)}"" TEXT,
            ""{nameof(PointDataStatusCodes.StatusCodeCount)}"" INT
        );
        SELECT create_hypertable('status_codes_points', by_range('{nameof(PointDataBase.Time).ToLower()}', INTERVAL '1 day'), if_not_exists => TRUE);
        CREATE INDEX IF NOT EXISTS {nameof(PointDataBase.SessionId)}_index ON status_codes_points ({nameof(PointDataBase.SessionId)}, {nameof(PointDataBase.Time)} DESC);";

    public static string InsertIntoPointDataStatusCodesTable(PointDataStatusCodes[] points)
    {
        var insertQuery = new StringBuilder();
        insertQuery.AppendLine($@"
        INSERT INTO status_codes_points
        (
            {nameof(PointDataBase.Measurement)},
            {nameof(PointDataBase.Time)},
            {nameof(PointDataBase.SessionId)},
            {nameof(PointDataBase.CurrentOperation)},
            {nameof(PointDataBase.NodeType)},
            {nameof(PointDataBase.TestSuite)},
            {nameof(PointDataBase.TestName)},
            {nameof(PointDataBase.ClusterId)},
            {nameof(PointDataStatusCodes.Scenario)},
            {nameof(PointDataStatusCodes.StatusCodeStatus)},
            {nameof(PointDataStatusCodes.StatusCodeCount)}
        )
        VALUES ");

        for (var i = 0; i < points.Length; i++)
        {
            var point = points[i];
            insertQuery.Append(@$"
            ('{point.Measurement}',
            '{point.Time:yyyy-MM-dd HH:mm:ss.fff zzz}',
            '{point.SessionId}',
            '{point.CurrentOperation}',
            '{point.NodeType}',
            '{point.TestSuite}',
            '{point.TestName}',
            '{point.ClusterId}',
            '{point.Scenario}',
            '{point.StatusCodeStatus}',
            {point.StatusCodeCount})");

            insertQuery.AppendLine(i < points.Length - 1 ? "," : ";");
        } 
    
        return insertQuery.ToString();
    }

    public static string SelectFromPointDataStatusCodesTable()
    {
        return @"
            SELECT * FROM status_codes_points
            WHERE sessionid = @SessionId
            AND time >= @StartTime
            AND time <= @EndTime
            ORDER BY Time DESC
            LIMIT 120;
        ";
    }

    public static string CreatePointDataStepStatsTable => $@"
    CREATE TABLE IF NOT EXISTS step_stats_points
    (
        ""{nameof(PointDataBase.Measurement)}"" TEXT,
        ""{nameof(PointDataBase.Time)}"" TIMESTAMPTZ,
        ""{nameof(PointDataBase.SessionId)}"" TEXT,
        ""{nameof(PointDataBase.CurrentOperation)}"" TEXT,
        ""{nameof(PointDataBase.NodeType)}"" TEXT,
        ""{nameof(PointDataBase.TestSuite)}"" TEXT,
        ""{nameof(PointDataBase.TestName)}"" TEXT,
        ""{nameof(PointDataBase.ClusterId)}"" TEXT,
        ""{nameof(PointDataStepStats.Step)}"" TEXT,
        ""{nameof(PointDataStepStats.Scenario)}"" TEXT,
        ""{nameof(PointDataStepStats.AllRequestCount)}"" INT,
        ""{nameof(PointDataStepStats.AllDataTransferAll)}"" BIGINT,
        ""{nameof(PointDataStepStats.OkRequestCount)}"" INT,
        ""{nameof(PointDataStepStats.OkRequestRps)}"" DOUBLE PRECISION,
        ""{nameof(PointDataStepStats.OkLatencyMax)}"" DOUBLE PRECISION,
        ""{nameof(PointDataStepStats.OkLatencyMean)}"" DOUBLE PRECISION,
        ""{nameof(PointDataStepStats.OkLatencyMin)}"" DOUBLE PRECISION,
        ""{nameof(PointDataStepStats.OkLatencyStdDev)}"" DOUBLE PRECISION,
        ""{nameof(PointDataStepStats.OkLatencyPercent50)}"" DOUBLE PRECISION,
        ""{nameof(PointDataStepStats.OkLatencyPercent75)}"" DOUBLE PRECISION,
        ""{nameof(PointDataStepStats.OkLatencyPercent95)}"" DOUBLE PRECISION,
        ""{nameof(PointDataStepStats.OkLatencyPercent99)}"" DOUBLE PRECISION,
        ""{nameof(PointDataStepStats.OkDataTransferMin)}"" BIGINT,
        ""{nameof(PointDataStepStats.OkDataTransferMean)}"" BIGINT,
        ""{nameof(PointDataStepStats.OkDataTransferMax)}"" BIGINT,
        ""{nameof(PointDataStepStats.OkDataTransferAll)}"" BIGINT,
        ""{nameof(PointDataStepStats.OkDataTransferPercent50)}"" BIGINT,
        ""{nameof(PointDataStepStats.OkDataTransferPercent75)}"" BIGINT,
        ""{nameof(PointDataStepStats.OkDataTransferPercent95)}"" BIGINT,
        ""{nameof(PointDataStepStats.OkDataTransferPercent99)}"" BIGINT,
        ""{nameof(PointDataStepStats.FailRequestCount)}"" INT,
        ""{nameof(PointDataStepStats.FailRequestRps)}"" DOUBLE PRECISION,
        ""{nameof(PointDataStepStats.FailLatencyMax)}"" DOUBLE PRECISION,
        ""{nameof(PointDataStepStats.FailLatencyMean)}"" DOUBLE PRECISION,
        ""{nameof(PointDataStepStats.FailLatencyMin)}"" DOUBLE PRECISION,
        ""{nameof(PointDataStepStats.FailLatencyStdDev)}"" DOUBLE PRECISION,
        ""{nameof(PointDataStepStats.FailLatencyPercent50)}"" DOUBLE PRECISION,
        ""{nameof(PointDataStepStats.FailLatencyPercent75)}"" DOUBLE PRECISION,
        ""{nameof(PointDataStepStats.FailLatencyPercent95)}"" DOUBLE PRECISION,
        ""{nameof(PointDataStepStats.FailLatencyPercent99)}"" DOUBLE PRECISION,
        ""{nameof(PointDataStepStats.FailDataTransferMin)}"" BIGINT,
        ""{nameof(PointDataStepStats.FailDataTransferMean)}"" BIGINT,
        ""{nameof(PointDataStepStats.FailDataTransferMax)}"" BIGINT,
        ""{nameof(PointDataStepStats.FailDataTransferAll)}"" BIGINT,
        ""{nameof(PointDataStepStats.FailDataTransferPercent50)}"" BIGINT,
        ""{nameof(PointDataStepStats.FailDataTransferPercent75)}"" BIGINT,
        ""{nameof(PointDataStepStats.FailDataTransferPercent95)}"" BIGINT,
        ""{nameof(PointDataStepStats.FailDataTransferPercent99)}"" BIGINT,
        ""{nameof(PointDataStepStats.SimulationValue)}"" INT
    );
    SELECT create_hypertable('step_stats_points', by_range('{nameof(PointDataBase.Time).ToLower()}', INTERVAL '1 day'), if_not_exists => TRUE);
    CREATE INDEX IF NOT EXISTS {nameof(PointDataBase.SessionId)}_index ON step_stats_points ({nameof(PointDataBase.SessionId)}, {nameof(PointDataBase.Time)} DESC);";

    public static string InsertIntoPointDataStepStatsTable(PointDataStepStats[] points)
    { 
        var insertQuery = new StringBuilder();
        insertQuery.AppendLine(@$"
        INSERT INTO step_stats_points
        (
            {nameof(PointDataBase.Measurement)},
            {nameof(PointDataBase.Time)},
            {nameof(PointDataBase.SessionId)},
            {nameof(PointDataBase.CurrentOperation)},
            {nameof(PointDataBase.NodeType)},
            {nameof(PointDataBase.TestSuite)},
            {nameof(PointDataBase.TestName)},
            {nameof(PointDataBase.ClusterId)},
            {nameof(PointDataStepStats.Step)},
            {nameof(PointDataStepStats.Scenario)},
            {nameof(PointDataStepStats.AllRequestCount)},
            {nameof(PointDataStepStats.AllDataTransferAll)},
            {nameof(PointDataStepStats.OkRequestCount)},
            {nameof(PointDataStepStats.OkRequestRps)},
            {nameof(PointDataStepStats.OkLatencyMax)},
            {nameof(PointDataStepStats.OkLatencyMean)},
            {nameof(PointDataStepStats.OkLatencyMin)},
            {nameof(PointDataStepStats.OkLatencyStdDev)},
            {nameof(PointDataStepStats.OkLatencyPercent50)},
            {nameof(PointDataStepStats.OkLatencyPercent75)},
            {nameof(PointDataStepStats.OkLatencyPercent95)},
            {nameof(PointDataStepStats.OkLatencyPercent99)},
            {nameof(PointDataStepStats.OkDataTransferMin)},
            {nameof(PointDataStepStats.OkDataTransferMean)},
            {nameof(PointDataStepStats.OkDataTransferMax)},
            {nameof(PointDataStepStats.OkDataTransferAll)},
            {nameof(PointDataStepStats.OkDataTransferPercent50)},
            {nameof(PointDataStepStats.OkDataTransferPercent75)},
            {nameof(PointDataStepStats.OkDataTransferPercent95)},
            {nameof(PointDataStepStats.OkDataTransferPercent99)},
            {nameof(PointDataStepStats.FailRequestCount)},
            {nameof(PointDataStepStats.FailRequestRps)},
            {nameof(PointDataStepStats.FailLatencyMax)},
            {nameof(PointDataStepStats.FailLatencyMean)},
            {nameof(PointDataStepStats.FailLatencyMin)},
            {nameof(PointDataStepStats.FailLatencyStdDev)},
            {nameof(PointDataStepStats.FailLatencyPercent50)},
            {nameof(PointDataStepStats.FailLatencyPercent75)},
            {nameof(PointDataStepStats.FailLatencyPercent95)},
            {nameof(PointDataStepStats.FailLatencyPercent99)},
            {nameof(PointDataStepStats.FailDataTransferMin)},
            {nameof(PointDataStepStats.FailDataTransferMean)},
            {nameof(PointDataStepStats.FailDataTransferMax)},
            {nameof(PointDataStepStats.FailDataTransferAll)},
            {nameof(PointDataStepStats.FailDataTransferPercent50)},
            {nameof(PointDataStepStats.FailDataTransferPercent75)},
            {nameof(PointDataStepStats.FailDataTransferPercent95)},
            {nameof(PointDataStepStats.FailDataTransferPercent99)},
            {nameof(PointDataStepStats.SimulationValue)}
        )
        VALUES ");
        
        for (var i = 0; i < points.Length; i++)
        {
            var point = points[i];
            insertQuery.Append(@$"
            ('{point.Measurement}',
            '{point.Time:yyyy-MM-dd HH:mm:ss.fff zzz}',
            '{point.SessionId}',
            '{point.CurrentOperation}',
            '{point.NodeType}',
            '{point.TestSuite}',
            '{point.TestName}',
            '{point.ClusterId}',
            '{point.Step}',
            '{point.Scenario}',
            {point.AllRequestCount},
            {point.AllDataTransferAll},
            {point.OkRequestCount}, 
            {point.OkRequestRps.ToString(CultureInfo.InvariantCulture)},
            {point.OkLatencyMax.ToString(CultureInfo.InvariantCulture)},
            {point.OkLatencyMean.ToString(CultureInfo.InvariantCulture)},
            {point.OkLatencyMin.ToString(CultureInfo.InvariantCulture)},
            {point.OkLatencyStdDev.ToString(CultureInfo.InvariantCulture)},
            {point.OkLatencyPercent50.ToString(CultureInfo.InvariantCulture)},
            {point.OkLatencyPercent75.ToString(CultureInfo.InvariantCulture)},
            {point.OkLatencyPercent95.ToString(CultureInfo.InvariantCulture)},
            {point.OkLatencyPercent99.ToString(CultureInfo.InvariantCulture)},
            {point.OkDataTransferMin},
            {point.OkDataTransferMean},
            {point.OkDataTransferMax},
            {point.OkDataTransferAll},
            {point.OkDataTransferPercent50},
            {point.OkDataTransferPercent75}, 
            {point.OkDataTransferPercent95}, 
            {point.OkDataTransferPercent99},
            {point.FailRequestCount.ToString(CultureInfo.InvariantCulture)},
            {point.FailRequestRps.ToString(CultureInfo.InvariantCulture)}, 
            {point.FailLatencyMax.ToString(CultureInfo.InvariantCulture)}, 
            {point.FailLatencyMean.ToString(CultureInfo.InvariantCulture)}, 
            {point.FailLatencyMin.ToString(CultureInfo.InvariantCulture)},
            {point.FailLatencyStdDev.ToString(CultureInfo.InvariantCulture)}, 
            {point.FailLatencyPercent50.ToString(CultureInfo.InvariantCulture)}, 
            {point.FailLatencyPercent75.ToString(CultureInfo.InvariantCulture)}, 
            {point.FailLatencyPercent95.ToString(CultureInfo.InvariantCulture)},
            {point.FailLatencyPercent99.ToString(CultureInfo.InvariantCulture)},
            {point.FailDataTransferMin}, 
            {point.FailDataTransferMean}, 
            {point.FailDataTransferMax}, 
            {point.FailDataTransferAll}, 
            {point.FailDataTransferPercent50},
            {point.FailDataTransferPercent75}, 
            {point.FailDataTransferPercent95}, 
            {point.FailDataTransferPercent99},
            {point.SimulationValue})");
    
            insertQuery.AppendLine(i < points.Length - 1 ? "," : ";");
        } 
        
        return insertQuery.ToString();
    }

    public static string SelectFromPointDataStepStatsTable()
    {
        return @"
                SELECT * FROM step_stats_points
                WHERE sessionid = @SessionId
                AND time >= @StartTime
                AND time <= @EndTime
                ORDER BY Time DESC
                LIMIT 120;
            ";
    }
}



       