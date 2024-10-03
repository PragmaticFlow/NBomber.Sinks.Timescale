#pragma warning disable CS1591

namespace NBomber.Sinks.Timescale.Contracts;

using System.ComponentModel.DataAnnotations.Schema;
using NBomber.Contracts.Stats;

public class PointDbRecord
{
    [Column(ColumnNames.Time)] public DateTime Time { get; set; }
    [Column(ColumnNames.ScenarioTimestamp)] public TimeSpan ScenarioTimestamp { get; set; }
    [Column(ColumnNames.SessionId)] public string SessionId { get; set; }
    [Column(ColumnNames.CurrentOperation)] public OperationType CurrentOperation { get; set; }
    

    [Column(ColumnNames.Scenario)] public string Scenario { get; set; }
    [Column(ColumnNames.Step)] public string Step { get; set; }
    [Column(ColumnNames.SortIndex)] public int SortIndex { get; set; }
    
    [Column(ColumnNames.AllReqCount)] public int AllReqCount { get; set; }
    [Column(ColumnNames.AllDataAll)] public long AllDataAll { get; set; }
    
    [Column(ColumnNames.OkReqCount)] public int OkReqCount { get; set; }
    [Column(ColumnNames.OkReqRps)] public double OkReqRps { get; set; }
    [Column(ColumnNames.OkLatencyMax)] public double OkLatencyMax { get; set; }
    [Column(ColumnNames.OkLatencyMean)] public double OkLatencyMean { get; set; }
    [Column(ColumnNames.OkLatencyMin)] public double OkLatencyMin { get; set; }
    [Column(ColumnNames.OkLatencyStdDev)] public double OkLatencyStdDev { get; set; }
    [Column(ColumnNames.OkLatencyP50)] public double OkLatencyP50 { get; set; }
    [Column(ColumnNames.OkLatencyP75)] public double OkLatencyP75 { get; set; }
    [Column(ColumnNames.OkLatencyP95)] public double OkLatencyP95 { get; set; }
    [Column(ColumnNames.OkLatencyP99)] public double OkLatencyP99 { get; set; }
    [Column(ColumnNames.OkDataMin)] public long OkDataMin { get; set; }
    [Column(ColumnNames.OkDataMean)] public long OkDataMean { get; set; }
    [Column(ColumnNames.OkDataMax)] public long OkDataMax { get; set; }
    [Column(ColumnNames.OkDataAll)] public long OkDataAll { get; set; }
    [Column(ColumnNames.OkDataP50)] public long OkDataP50 { get; set; }
    [Column(ColumnNames.OkDataP75)] public long OkDataP75 { get; set; }
    [Column(ColumnNames.OkDataP95)] public long OkDataP95 { get; set; }
    [Column(ColumnNames.OkDataP99)] public long OkDataP99 { get; set; }
    [Column(ColumnNames.OkStatusCodes)] public string OkStatusCodes { get; set; }
    [Column(ColumnNames.OkLatencyCount)] public string OkLatencyCount { get; set; }
    
    [Column(ColumnNames.FailReqCount)] public int FailReqCount { get; set; }
    [Column(ColumnNames.FailReqRps)] public double FailReqRps { get; set; }
    [Column(ColumnNames.FailLatencyMax)] public double FailLatencyMax { get; set; }
    [Column(ColumnNames.FailLatencyMean)] public double FailLatencyMean { get; set; }
    [Column(ColumnNames.FailLatencyMin)] public double FailLatencyMin { get; set; }
    [Column(ColumnNames.FailLatencyStdDev)] public double FailLatencyStdDev { get; set; }
    [Column(ColumnNames.FailLatencyP50)] public double FailLatencyP50 { get; set; }
    [Column(ColumnNames.FailLatencyP75)] public double FailLatencyP75 { get; set; }
    [Column(ColumnNames.FailLatencyP95)] public double FailLatencyP95 { get; set; }
    [Column(ColumnNames.FailLatencyP99)] public double FailLatencyP99 { get; set; }
    [Column(ColumnNames.FailDataMin)] public long FailDataMin { get; set; }
    [Column(ColumnNames.FailDataMean)] public long FailDataMean { get; set; }
    [Column(ColumnNames.FailDataMax)] public long FailDataMax { get; set; }
    [Column(ColumnNames.FailDataAll)] public long FailDataAll { get; set; }
    [Column(ColumnNames.FailDataP50)] public long FailDataP50 { get; set; }
    [Column(ColumnNames.FailDataP75)] public long FailDataP75 { get; set; }
    [Column(ColumnNames.FailDataP95)] public long FailDataP95 { get; set; }
    [Column(ColumnNames.FailDataP99)] public long FailDataP99 { get; set; }
    [Column(ColumnNames.FailStatusCodes)] public string FailStatusCodes { get; set; }
    [Column(ColumnNames.FailLatencyCount)] public string FailLatencyCount { get; set; }
 
    [Column(ColumnNames.SimulationValue)] public int SimulationValue { get; set; }
}

public class NodeInfoDbRecord
{
    [Column(ColumnNames.Time)] public DateTime Time { get; set; }
    [Column(ColumnNames.SessionId)] public string SessionId { get; set; }
    [Column(ColumnNames.CurrentOperation)] public OperationType CurrentOperation { get; set; }
    [Column(ColumnNames.TestSuite)] public string TestSuite { get; set; }
    [Column(ColumnNames.TestName)] public string TestName { get; set; }
    [Column(ColumnNames.Metadata)] public string Metadata{ get; set; }
    [Column(ColumnNames.NodeInfo)] public string NodeInfo { get; set; }
}