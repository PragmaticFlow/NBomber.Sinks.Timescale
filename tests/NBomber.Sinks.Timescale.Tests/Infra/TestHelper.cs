using NBomber.Sinks.Timescale.DAL;
using Npgsql;
using RepoDb;

namespace NBomber.Sinks.Timescale.Tests.Infra
{
    public class TestHelper(string connectionString)
    {
        public async Task CreateTables()
        {
            await using var connection = new NpgsqlConnection(connectionString);

            await connection.ExecuteNonQueryAsync(
                        SqlQueries.CreateStepStatsTable
                      + SqlQueries.CreateSessionsTable
                      + SqlQueries.CreateDbSchemaVersion);
        }

        public async Task SetDbSchemaVersion(int version)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            await connection.ExecuteNonQueryAsync($@"
                            INSERT INTO {TableNames.SchemaVersionTable} (""{ColumnNames.Version}"")
                            VALUES ({version})
                            ;");
        }

        public async Task DeleteTables()
        {
            await using var connection = new NpgsqlConnection(connectionString);

            await connection.ExecuteNonQueryAsync
                (@$"DROP TABLE IF EXISTS {TableNames.SchemaVersionTable}, 
                                         {TableNames.SessionsTable};
                    DROP TABLE IF EXISTS {TableNames.StepStatsTable};");
        }

        public async Task<int> GetDBSchemaVersion()
        {
            await using var connection = new NpgsqlConnection(connectionString);
         
            try
            {
                var result = await connection.ExecuteQueryAsync<int>($@"SELECT ""{ColumnNames.Version}"" FROM {TableNames.SchemaVersionTable};");
                var currentDbVersion = result.FirstOrDefault();
                return currentDbVersion;
            }
            catch
            {
                return -1;
            }
        }

        public async Task<int> GetDataCount(string tableName)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            try
            {
                var result = await connection.ExecuteScalarAsync<int>($@"SELECT COUNT(*) FROM {tableName};");

                return result;
            }
            catch
            {
                return -1;
            }
        }
    }
}
