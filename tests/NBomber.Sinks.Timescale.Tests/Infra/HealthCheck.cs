using Npgsql;
using RepoDb;

namespace NBomber.Sinks.Timescale.Tests.Infra
{
    static class HealthCheck
    {
        public static async Task WaitUntilReady(NpgsqlDataSource dataSource)
        {
            while (!await CheckIfDbExist(dataSource))
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        private static async Task<bool> CheckIfDbExist(NpgsqlDataSource dataSource)
        {
            try
            {
                await using var connection = await dataSource.OpenConnectionAsync();

                return await connection.ExecuteScalarAsync<bool>("SELECT EXISTS (SELECT FROM pg_tables)");
            }
            catch
            {
                return false;
            }
        }
    }
}
