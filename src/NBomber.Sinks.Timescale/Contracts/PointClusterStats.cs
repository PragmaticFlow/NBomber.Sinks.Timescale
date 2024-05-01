#pragma warning disable CS1591

using System.ComponentModel.DataAnnotations.Schema;

namespace NBomber.Sinks.Timescale.Contracts;

public class PointClusterStats : PointBase
{
    [Column(ColumnNames.NodeCount)] public int NodeCount { get; set; }
    [Column(ColumnNames.NodeCpuCount)] public int NodeCpuCount { get; set; }
}