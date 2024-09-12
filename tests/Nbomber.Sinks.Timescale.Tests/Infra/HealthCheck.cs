using Npgsql;
using RepoDb;

namespace Nbomber.Sinks.Timescale.Tests.Infra
{
    static class HealthCheck
    {
        public static async Task WaitUntilReady(string connectionString)
        {
            while (!await CheckIfDbExist(connectionString))
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        private static async Task<bool> CheckIfDbExist(string connectionString)
        {
            try
            {
                using var connection = new NpgsqlConnection(connectionString);

                return await connection.ExecuteScalarAsync<bool>("SELECT EXISTS (SELECT FROM pg_tables)");
            }
            catch
            {
                return false;
            }
        }
    }
}
