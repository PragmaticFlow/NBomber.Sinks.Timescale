using Serilog;
using Microsoft.Extensions.Configuration;
using NBomber.Contracts;
using NBomber.Contracts.Stats;
using NBomber.Sinks.Timescale.Models;
using NBomber.Sinks.Timescale.Structures;

namespace NBomber.Sinks.Timescale;

public class TimescaleDbSinkConfig
{
    public string ConnectionString { get; set; }
}


public class TimescaleDbSink : IReportingSink
{
    private ILogger _logger;
    private IBaseContext _context;
    private TimescaleDbContext _timescaleDbContext;
    
    public string SinkName => "NBomber.Sinks.TimescaleDb";

    public TimescaleDbSink() { }

    public TimescaleDbSink(string connectionString)
    {
        _timescaleDbContext = new TimescaleDbContext(connectionString);
    }
    
    public TimescaleDbSink(TimescaleDbContext dbContext)
    {
        _timescaleDbContext = dbContext;
    }
    
    public Task Init(IBaseContext context, IConfiguration infraConfig)
    {
        _logger = context.Logger.ForContext<TimescaleDbSink>();
        _context = context;

        var config = infraConfig?.GetSection("InfluxDBSink").Get<TimescaleDbSinkConfig>();
        if (config != null)
        {
            _timescaleDbContext = new TimescaleDbContext(config.ConnectionString);
        }
        
        if (_timescaleDbContext == null)
        {
            _logger.Error("Reporting Sink {0} has problems with initialization. The problem could be related to invalid config structure.", SinkName);
                
            throw new Exception(
                $"Reporting Sink {SinkName} has problems with initialization. The problem could be related to invalid config structure.");
        }
            
        return Task.CompletedTask;
    }

    public Task Start()
    {
        if (_timescaleDbContext != null)
        {
            var point = new PointData
            {
                Measurement = "nbomber",
                
                ClusterNodeCount = 1,
                ClusterNodeCpuCount = _context.GetNodeInfo().CoresCount,
                
                
            };
        }
    }

    public Task SaveRealtimeStats(ScenarioStats[] stats)
    {
        throw new NotImplementedException();
    }

    public Task SaveFinalStats(NodeStats stats)
    {
        throw new NotImplementedException();
    }

    public Task Stop()
    {
        throw new NotImplementedException();
    }
    
    public void Dispose()
    {
        throw new NotImplementedException();
    }

    private void AddTestInfoTags(PointData point)
    {
        var nodeInfo = _context.GetNodeInfo();
        var testInfo = _context.TestInfo;
        
    }
}