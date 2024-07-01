#pragma warning disable CS1591

namespace NBomber.Sinks.Timescale.Contracts;

using System.ComponentModel.DataAnnotations.Schema;
using NBomber.Contracts.Stats;

public class PointDbRecord
{
    [Column(ColumnNames.Time)] public DateTimeOffset Time { get; set; }
    [Column(ColumnNames.SessionId)] public string SessionId { get; set; }
    [Column(ColumnNames.CurrentOperation)] public OperationType CurrentOperation { get; set; }
    [Column(ColumnNames.TestSuite)] public string TestSuite { get; set; }
    [Column(ColumnNames.TestName)] public string TestName { get; set; }
    [Column(ColumnNames.Scenario)] public string Scenario { get; set; }
    [Column(ColumnNames.Step)] public string Step { get; set; }
    [Column(ColumnNames.OkStepStats)] public string OkStepStats { get; set; }
    [Column(ColumnNames.FailStepStats)] public string FailStepStats { get; set; }
}

public class NodeInfoDbRecord
{
    [Column(ColumnNames.Time)] public DateTimeOffset Time { get; set; }
    [Column(ColumnNames.SessionId)] public string SessionId { get; set; }
    [Column(ColumnNames.NodeInfo)] public string NodeInfo { get; set; }
}