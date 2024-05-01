#pragma warning disable CS1591

using System.ComponentModel.DataAnnotations.Schema;

namespace NBomber.Sinks.Timescale.Contracts;

public class PointLatencyCounts : PointBase
{
    [Column(ColumnNames.Scenario)] public string Scenario { get; set; }
    [Column(ColumnNames.LessOrEq800)] public int LessOrEq800 { get; set; }
    [Column(ColumnNames.More800Less1200)] public int More800Less1200 { get; set; }
    [Column(ColumnNames.MoreOrEq1200)] public int MoreOrEq1200 { get; set; }
}