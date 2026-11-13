using System.ComponentModel.DataAnnotations.Schema;
using NBomber.Contracts.Stats;
using NBomber.Sinks.Timescale.DAL;

namespace NBomber.Sinks.Timescale.Contracts;

[Table(TableNames.ThresholdsTable)]
internal class ThresholdStatsDbRecord
{
    [Column(ColumnNames.Time)] public DateTime Time { get; set; }
    [Column(ColumnNames.ScenarioTimestamp)] public TimeSpan ScenarioTimestamp { get; set; }
    [Column(ColumnNames.SessionId)] public string SessionId { get; set; } = string.Empty;
    [Column(ColumnNames.CurrentOperation)] public OperationType CurrentOperation { get; set; }
    [Column(ColumnNames.Scenario)] public string Scenario { get; set; } = string.Empty;
    [Column(ColumnNames.Step)] public string Step { get; set; } = string.Empty;
    [Column(ColumnNames.CheckExpression)] public string CheckExpression { get; set; } = string.Empty;
    [Column(ColumnNames.IsFailed)] public bool IsFailed { get; set; }
    [Column(ColumnNames.ExceptionMsg)] public string ExceptionMsg { get; set; } = string.Empty;
    [Column(ColumnNames.ErrorCount)] public int ErrorCount { get; set; }
}
