using NBomber.Sinks.Timescale.Contracts;
using Npgsql;
using RepoDb;
using ILogger = Serilog.ILogger;

namespace NBomber.Sinks.Timescale
{
    public class DbMigrations
    {
        public const int SinkSchemaVersion = 0;
        private string _connectionString;
        private ILogger _logger;

        public DbMigrations(string connectionString, ILogger logger)
        {
            _connectionString = connectionString;
            _logger = logger;
        }

        public async Task Run()
        {
            var currentDbVersion = await GetCurrendDBVersion();

            if (currentDbVersion > SinkSchemaVersion)
            {
                var errMessage = $@"Your NBomber.Sinks.Timescale schema version: '{SinkSchemaVersion}' is not compatible with DB schema version: '{currentDbVersion}'";
                _logger.Error(errMessage);
                throw new PlatformNotSupportedException(errMessage);
            }
            else if (currentDbVersion < SinkSchemaVersion) 
            {
                for (var v = currentDbVersion + 1; v <= SinkSchemaVersion; v++)
                {
                    await ApplyMigration(v);
                }
            }
        }

        private async Task<int> GetCurrendDBVersion()
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                var result = await connection.ExecuteQueryAsync<int>($@"SELECT ""{ColumnNames.Version}"" FROM {SqlQueries.DbSchemaVersion};");
                var currentDbVersion = result.FirstOrDefault();
                return currentDbVersion;
            }
            catch (Exception ex) 
            { 
                _logger.Error(ex, ex.Message);
                return -1;
            }
        }

        private async Task ApplyMigration(int version)
        {
            using var connection = new NpgsqlConnection(_connectionString);

            switch (version) 
            {
                case 0:
                    await connection.ExecuteNonQueryAsync(
                        SqlQueries.CreateStepStatsTable
                      + SqlQueries.CreateSessionsTable
                      + SqlQueries.CreateDbSchemaVersion);

                    await connection.ExecuteNonQueryAsync($@"
                            INSERT INTO {SqlQueries.DbSchemaVersion} (""{ColumnNames.Version}"")
                            VALUES ({version})
                            ;");
                    
                    _logger.Debug("Created initial tables");
                    break;

                //case 1:
                //    await connection.ExecuteNonQueryAsync($@"
                //            ALTER TABLE {SqlQueries.StepStatsTable}
                //            ADD COLUMN IF NOT EXISTS {ColumnNames.TestCulomn} TEXT
                //            ;
                            
                //            WITH updated AS (
                //                UPDATE {SqlQueries.DbSchemaVersion}
                //                SET ""{ColumnNames.Version}"" = {version}
                //                RETURNING *
                //            )
                //            INSERT INTO {SqlQueries.DbSchemaVersion} (""{ColumnNames.Version}"")
                //            SELECT 1 
                //            WHERE NOT EXISTS (SELECT * FROM updated);");
                //    break;
            }
        }
    }
}
