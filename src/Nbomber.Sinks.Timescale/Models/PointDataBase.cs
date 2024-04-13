using System.ComponentModel.DataAnnotations.Schema;

namespace NBomber.Sinks.Timescale.Models;

public class PointDataBase
{
    public string Measurement { get; set; }

    [Column("time")]
    public DateTimeOffset Time { get; set; }

    [Column("sessionid")]
    public string SessionId { get; set; }
    public string CurrentOperation { get; set; }
    public string NodeType { get; set; } 
    public string TestSuite { get; set; } 
    public string TestName { get; set; }
    public string ClusterId { get; set; }
}