using System.ComponentModel.DataAnnotations.Schema;

namespace NBomber.Sinks.Timescale.Contracts;

public class PointBase
{
    [Column(ColumnNames.Time)] public DateTimeOffset Time { get; set; }
    [Column(ColumnNames.SessionId)] public string SessionId { get; set; }
    [Column(ColumnNames.CurrentOperation)] public string CurrentOperation { get; set; }
    [Column(ColumnNames.NodeType)] public string NodeType { get; set; } 
    [Column(ColumnNames.TestSuite)] public string TestSuite { get; set; } 
    [Column(ColumnNames.TestName)] public string TestName { get; set; }
    [Column(ColumnNames.ClusterId)] public string ClusterId { get; set; }
}