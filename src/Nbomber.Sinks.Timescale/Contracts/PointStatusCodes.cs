using System.ComponentModel.DataAnnotations.Schema;

namespace NBomber.Sinks.Timescale.Contracts;

public class PointStatusCodes : PointBase
{
    [Column(ColumnNames.Scenario)] public string Scenario { get; set; }
    [Column(ColumnNames.StatusCode)] public string StatusCode { get; set; }
    [Column(ColumnNames.Count)] public int Count { get; set; }
}