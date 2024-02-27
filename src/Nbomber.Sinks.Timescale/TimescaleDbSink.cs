using Microsoft.Extensions.Configuration;
using NBomber.Contracts;
using NBomber.Contracts.Stats;

namespace NBomber.Sinks.Timescale;

public class TimescaleDbSink : IReportingSink
{
    public string SinkName { get; }

    public Task Init(IBaseContext context, IConfiguration infraConfig)
    {
        throw new NotImplementedException();
    }

    public Task Start()
    {
        throw new NotImplementedException();
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
}