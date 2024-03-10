namespace NBomber.Sinks.Timescale.Models;

public class PointDataLatencyCounts : PointDataBase
{
    public string Scenario { get; set; } = "0";
    
    public int LatencyCountLessOrEq800 { get; set; }
    public int LatencyCountMore800Less1200 { get; set; }
    public int LatencyCountMoreOrEq1200 { get; set; }
}