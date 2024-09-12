using NBomber.Sinks.Timescale.Contracts;
using NBomber.Sinks.Timescale;
using Npgsql;
using RepoDb;

namespace Nbomber.Sinks.Timescale.Tests.Infra
{
    public class TestHelper
    {
        private readonly string _connectionString;

        public TestHelper(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task CreateTables()
        {
            using var connection = new NpgsqlConnection(_connectionString);

            await connection.ExecuteNonQueryAsync(
                        SqlQueries.CreateStepStatsTable
                      + SqlQueries.CreateSessionsTable
                      + SqlQueries.CreateDbSchemaVersion);
        }

        public async Task SetDbSchemaVersion(int version)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            await connection.ExecuteNonQueryAsync($@"
                            INSERT INTO {SqlQueries.DbSchemaVersion} (""{ColumnNames.Version}"")
                            VALUES ({version})
                            ;");
        }

        public async Task DeleteTables()
        {
            using var connection = new NpgsqlConnection(_connectionString);

            await connection.ExecuteNonQueryAsync
                (@$"DROP TABLE IF EXISTS {SqlQueries.DbSchemaVersion}, 
                                         {SqlQueries.SessionsTable};
                    DROP TABLE IF EXISTS {SqlQueries.StepStatsTable};");
        }

        public async Task<int> GetDBSchemaVersion()
        {
            using var connection = new NpgsqlConnection(_connectionString);
         
            try
            {
                var result = await connection.ExecuteQueryAsync<int>($@"SELECT ""{ColumnNames.Version}"" FROM {SqlQueries.DbSchemaVersion};");
                var currentDbVersion = result.FirstOrDefault();
                return currentDbVersion;
            }
            catch (Exception ex)
            {
                return -1;
            }
        }
    }
}
