namespace NBomber.Sinks.Timescale.Models;

public class PointDataStart : PointDataBase
{
    public int ClusterNodeCount { get; set; }
    public int ClusterNodeCpuCount { get; set; }
}