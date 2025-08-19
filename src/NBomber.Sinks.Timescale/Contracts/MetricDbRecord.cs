#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using System.ComponentModel.DataAnnotations.Schema;
using NBomber.Contracts.Stats;
using NBomber.Sinks.Timescale.DAL;

namespace NBomber.Sinks.Timescale.Contracts;

public class MetricDbRecord
{
    [Column(ColumnNames.Time)] public DateTime Time { get; set; }
    [Column(ColumnNames.ScenarioTimestamp)] public TimeSpan ScenarioTimestamp { get; set; }
    [Column(ColumnNames.SessionId)] public string SessionId { get; set; }
    [Column(ColumnNames.CurrentOperation)] public OperationType CurrentOperation { get; set; }
    [Column(ColumnNames.Scenario)] public string Scenario { get; set; }
    [Column(ColumnNames.Metric)] public string Metric { get; set; }
    [Column(ColumnNames.MetricType)] public string MetricType { get; set; }
    [Column(ColumnNames.UnitOfMeasure)] public string UnitOfMeasure { get; set; }
    [Column(ColumnNames.Value)] public double Value { get; set; }
}