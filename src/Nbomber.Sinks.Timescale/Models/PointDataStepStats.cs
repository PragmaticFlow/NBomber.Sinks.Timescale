namespace NBomber.Sinks.Timescale.Models;

public class PointDataStepStats : PointDataBase
{
    public string Scenario { get; set; } 
    public string Step { get; set; }
    
    public int AllRequestCount { get; set; }
    public long AllDataTransferAll { get; set; }
    
    public int OkRequestCount { get; set; }
    public double OkRequestRps { get; set; }
    
    public double OkLatencyMax { get; set; }
    public double OkLatencyMean { get; set; }
    public double OkLatencyMin { get; set; }
    public double OkLatencyStdDev { get; set; }
    public double OkLatencyPercent50 { get; set; }
    public double OkLatencyPercent75 { get; set; }
    public double OkLatencyPercent95 { get; set; }
    public double OkLatencyPercent99 { get; set; }
    
    public long OkDataTransferMin { get; set; }
    public long OkDataTransferMean { get; set; }
    public long OkDataTransferMax { get; set; }
    public long OkDataTransferAll { get; set; }
    public long OkDataTransferPercent50 { get; set; }
    public long OkDataTransferPercent75 { get; set; }
    public long OkDataTransferPercent95 { get; set; }
    public long OkDataTransferPercent99 { get; set; }

    public int FailRequestCount { get; set; }
    public double FailRequestRps { get; set; }
    
    public double FailLatencyMax { get; set; }
    public double FailLatencyMean { get; set; }
    public double FailLatencyMin { get; set; }
    public double FailLatencyStdDev { get; set; }
    public double FailLatencyPercent50 { get; set; }
    public double FailLatencyPercent75 { get; set; }
    public double FailLatencyPercent95 { get; set; }
    public double FailLatencyPercent99 { get; set; }

    public long FailDataTransferMin { get; set; }
    public long FailDataTransferMean { get; set; }
    public long FailDataTransferMax { get; set; }
    public long FailDataTransferAll { get; set; }
    public long FailDataTransferPercent50 { get; set; }
    public long FailDataTransferPercent75 { get; set; }
    public long FailDataTransferPercent95 { get; set; }
    public long FailDataTransferPercent99 { get; set; }
    
    public int SimulationValue { get; set; }
}