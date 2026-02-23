#pragma warning disable CS8602, CS8618
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Services;
using Npgsql;
using System.Text.Json;

namespace NBomber.Sinks.Timescale.Tests.Infra
{
    public class EnvContextFixture : IDisposable
    {
        private readonly Config? _config;
        private readonly ICompositeService _docker;

        public TestHelper TestHelper {  get; private set; }

        public EnvContextFixture()
        {
            _config = JsonSerializer.Deserialize<Config>(json: File.ReadAllText("config.json"));

            if (_config.StartDockerCompose)
            {
                var dockerCompose = "docker-compose.yml";

                _docker = new Builder()
                    .UseContainer()
                    .UseCompose()
                    .FromFile(dockerCompose)
                    .RemoveOrphans()
                    .Build();

                _docker.Start();
            }

            var dataSourceBuilder = new NpgsqlDataSourceBuilder(_config.DBSettings.ConnectionString);
            var dataSource = dataSourceBuilder.Build();

            HealthCheck.WaitUntilReady(dataSource).Wait();

            TestHelper = new TestHelper(dataSource);
        }

        public TimescaleDbSink CreateTimescaleDbSinkInstance()
        {
            return new TimescaleDbSink(new TimescaleDbSinkConfig(_config.DBSettings.ConnectionString));
        }
        
        public void Dispose()
        {
            if (_config.StartDockerCompose)
            {
                _docker.Stop();
                _docker.Dispose();
            }
        }
    }

    public class Config
    {
        public bool StartDockerCompose { get; set; }
        public DBSettings DBSettings { get; set; }
    }

    public class DBSettings
    {
        public string ConnectionString { get; set; }
    }
}
