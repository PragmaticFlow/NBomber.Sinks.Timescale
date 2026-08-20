using RepoDb.Interfaces;
using RepoDb.Options;

namespace NBomber.Sinks.Timescale.Tests.Infra;

/// <summary>
/// Npgsql reads a PostgreSQL 'time' column as TimeOnly and an 'interval' column as TimeSpan,
/// so reading TimeSpan properties (e.g. StepStatsDbRecord.ScenarioTimestamp) back with RepoDb
/// throws an invalid cast whenever the column is 'time'.
/// This handler accepts both types; force: true overrides RepoDb's default TimeSpan mapping.
/// </summary>
public sealed class TimeSpanPropertyHandler : IPropertyHandler<object, TimeSpan>
{
    public TimeSpan Get(object input, PropertyHandlerGetOptions options) =>
        input switch
        {
            TimeOnly timeOnly => timeOnly.ToTimeSpan(), // from 'time' column, always < 24h
            TimeSpan timeSpan => timeSpan,              // from 'interval' column, may exceed 24h
            null or DBNull => default,
            _ => throw new InvalidCastException($"Cannot convert '{input.GetType()}' to TimeSpan.")
        };

    public object Set(TimeSpan input, PropertyHandlerSetOptions options) => input;
}
