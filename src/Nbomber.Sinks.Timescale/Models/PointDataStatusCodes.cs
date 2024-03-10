namespace NBomber.Sinks.Timescale.Models;

public class PointDataStatusCodes : PointDataBase
{
    public string Scenario { get; set; }
    
    public string StatusCodeStatus { get; set; }
    public int StatusCodeCount { get; set; }
}