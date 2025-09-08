using System.ComponentModel.DataAnnotations.Schema;
using NBomber.Sinks.Timescale.DAL;
using NBomber.Sinks.Timescale.Domain;

namespace NBomber.Sinks.Timescale.Contracts;

internal class MetricDbRecord
{
    [Column(ColumnNames.Time)] public DateTime Time { get; set; }
    [Column(ColumnNames.ScenarioTimestamp)] public TimeSpan ScenarioTimestamp { get; set; }
    [Column(ColumnNames.SessionId)] public string SessionId { get; set; }
    [Column(ColumnNames.CurrentOperation)] public TimescaleOperationType CurrentOperation { get; set; }
    [Column(ColumnNames.Scenario)] public string Scenario { get; set; }
    [Column(ColumnNames.Metric)] public string Metric { get; set; }
    [Column(ColumnNames.MetricType)] public string MetricType { get; set; }
    [Column(ColumnNames.UnitOfMeasure)] public string UnitOfMeasure { get; set; }
    [Column(ColumnNames.Value)] public double Value { get; set; }
}