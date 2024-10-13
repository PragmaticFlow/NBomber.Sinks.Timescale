using System.ComponentModel.DataAnnotations.Schema;
using NBomber.Contracts.Stats;
using NBomber.Sinks.Timescale.DAL;

namespace NBomber.Sinks.Timescale.Contracts;

internal class NodeInfoDbRecord
{
    [Column(ColumnNames.Time)] public DateTime Time { get; set; }
    [Column(ColumnNames.SessionId)] public string SessionId { get; set; }
    [Column(ColumnNames.CurrentOperation)] public OperationType CurrentOperation { get; set; }
    [Column(ColumnNames.TestSuite)] public string TestSuite { get; set; }
    [Column(ColumnNames.TestName)] public string TestName { get; set; }
    [Column(ColumnNames.Metadata)] public string Metadata{ get; set; }
    [Column(ColumnNames.NodeInfo)] public string NodeInfo { get; set; }
}