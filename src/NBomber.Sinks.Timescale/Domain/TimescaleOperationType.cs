#pragma warning disable CS1591

namespace NBomber.Sinks.Timescale.Domain;

public enum TimescaleOperationType
{
    None,
    Init,
    WarmUp,
    Bombing,
    Stopped,
    Completed,
    Error
}
