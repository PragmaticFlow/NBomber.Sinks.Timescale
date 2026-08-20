#pragma warning disable CS8602, CS8618
using Npgsql;
using RepoDb;
using System.Text.Json;

namespace NBomber.Sinks.Timescale.Tests.Infra;

public class EnvContextFixture
{
    private readonly Config? _config;

    public TestHelper TestHelper {  get; private set; }

    public EnvContextFixture()
    {
        _config = JsonSerializer.Deserialize<Config>(json: File.ReadAllText("config.json"));

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(_config.DBSettings.ConnectionString);
        var dataSource = dataSourceBuilder.Build();

        HealthCheck.WaitUntilReady(dataSource).Wait();

        PropertyHandlerMapper.Add<TimeSpan, TimeSpanPropertyHandler>(force: true);

        TestHelper = new TestHelper(dataSource);
    }

    public TimescaleDbSink CreateTimescaleDbSinkInstance()
    {
        return new TimescaleDbSink(new TimescaleDbSinkConfig(_config.DBSettings.ConnectionString));
    }

    public TimescaleDbSink CreateTimescaleDbSinkInstance(bool listenStopCommandEnabled = true)
    {
        return new TimescaleDbSink(new TimescaleDbSinkConfig(_config.DBSettings.ConnectionString, listenStopCommandEnabled));
    }
}

public class Config
{
    public DBSettings DBSettings { get; set; }
}

public class DBSettings
{
    public string ConnectionString { get; set; }
}
