using Ductus.FluentDocker.Services;
using Ductus.FluentDocker.Builders;
using System.Text.Json;
using NBomber.Sinks.Timescale;

namespace Nbomber.Sinks.Timescale.Tests.Infra
{
    public class EnvContextFixture : IDisposable
    {
        private readonly ICompositeService _docker;
        private static readonly bool UseDocker = true;
        private readonly Func<TimescaleDbSink> _createSincFn;

        public TestHelper TestHelper {  get; private set; }
        public TimescaleDbSink TimescaleDbSink { get; private set; }

        public EnvContextFixture()
        {
            var config = JsonSerializer.Deserialize<Config>(
                json: File.ReadAllText("config.Development.json")
            );

            _createSincFn = () => new TimescaleDbSink(config.DBSettings.ConnectionString);

            if (UseDocker)
            {
                var dockerCompose = Path.Combine(Directory.GetCurrentDirectory(), "docker-compose.yml");

                _docker = new Builder()
                    .UseContainer()
                    .UseCompose()
                    .FromFile(dockerCompose)
                    .RemoveOrphans()
                    .Build();

                _docker.Start();
            }

            HealthCheck.WaitUntilReady(config.DBSettings.ConnectionString).Wait();

            TestHelper = new TestHelper(config.DBSettings.ConnectionString);
        }

        public TimescaleDbSink CreateTimescaleDbSinkInstance()
        {
            return _createSincFn();
        }
        public void Dispose()
        {
            if (UseDocker)
            {
                _docker.Stop();
                _docker.Dispose();
            }
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
}
