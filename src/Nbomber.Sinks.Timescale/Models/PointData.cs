namespace NBomber.Sinks.Timescale.Models;

public class PointData
{
     public string Measurement { get; set; }

    // Tegs
    
    public string StatusCodeStatus { get; set; }
    public string CurrentOperation { get; set; }
    public string NodeType { get; set; }
    public string TestSuite { get; set; }
    public string TestName { get; set; }
    public string ClusterId { get; set; }
    public string Scenario { get; set; }
    public string Step { get; set; }

    // Fields
    public int? ClusterNodeCount { get; set; }
    public int? ClusterNodeCpuCount { get; set; }
    
    public string? SessionId { get; set; }

    public int? AllRequestCountAll { get; set; }
    public long? AllDataTransferAll { get; set; }

    public double? OkLatencyMax { get; set; }
    public double? OkLatencyMean { get; set; }
    public double? OkLatencyMin { get; set; }
    public double? OkLatencyStdDev { get; set; }
    public double? OkLatencyPercent50 { get; set; }
    public double? OkLatencyPercent75 { get; set; }
    public double? OkLatencyPercent95 { get; set; }
    public double? OkLatencyPercent99 { get; set; }

    public long? OkDataTransferMin { get; set; }
    public long? OkDataTransferMean { get; set; }
    public long? OkDataTransferMax { get; set; }
    public long? OkDataTransferAll { get; set; }
    public long? OkDataTransferPercent50 { get; set; }
    public long? OkDataTransferPercent75 { get; set; }
    public long? OkDataTransferPercent95 { get; set; }
    public long? OkDataTransferPercent99 { get; set; }

    public int? FailRequestCount { get; set; }
    public double? FailRequestRps { get; set; }
    
    public double? FailLatencyMax { get; set; }
    public double? FailLatencyMean { get; set; }
    public double? FailLatencyMin { get; set; }
    public double? FailLatencyStdDev { get; set; }
    public double? FailLatencyPercent50 { get; set; }
    public double? FailLatencyPercent75 { get; set; }
    public double? FailLatencyPercent95 { get; set; }
    public double? FailLatencyPercent99 { get; set; }

    public long? FailDataTransferMin { get; set; }
    public long? FailDataTransferMean { get; set; }
    public long? FailDataTransferMax { get; set; }
    public long? FailDataTransferAll { get; set; }
    public long? FailDataTransferPercent50 { get; set; }
    public long? FailDataTransferPercent75 { get; set; }
    public long? FailDataTransferPercent95 { get; set; }
    public long? FailDataTransferPercent99 { get; set; }
    
    public int? SimulationValue { get; set; }

    public int? LatencyCountLessOrEq800 { get; set; }
    public int? LatencyCountMore800Less1200 { get; set; }
    public int? LatencyCountMoreOrEq1200 { get; set; }

    public int? StatusCodeCount { get; set; }
}