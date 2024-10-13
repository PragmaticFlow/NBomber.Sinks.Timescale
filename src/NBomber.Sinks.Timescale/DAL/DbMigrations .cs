using Npgsql;
using RepoDb;
using ILogger = Serilog.ILogger;

namespace NBomber.Sinks.Timescale.DAL;

internal class DbMigrations(string connectionString, ILogger logger)
{
    public const int SinkSchemaVersion = 0;

    public async Task Run()
    {
        var currentDbVersion = await GetCurrendDbVersion();

        if (currentDbVersion > SinkSchemaVersion)
        {
            var errMessage = $@"Your NBomber.Sinks.Timescale schema version: '{SinkSchemaVersion}' is not compatible with DB schema version: '{currentDbVersion}'";
            logger.Error(errMessage);
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

    private async Task<int> GetCurrendDbVersion()
    {
        try
        {
            using var connection = new NpgsqlConnection(connectionString);
            var result = await connection.ExecuteQueryAsync<int>($@"SELECT ""{ColumnNames.Version}"" FROM {TableNames.SchemaVersionTable};");
            var currentDbVersion = result.FirstOrDefault();
            return currentDbVersion;
        }
        catch (Exception ex) 
        { 
            logger.Error(ex, ex.Message);
            return -1;
        }
    }

    private async Task ApplyMigration(int version)
    {
        await using var connection = new NpgsqlConnection(connectionString);

        switch (version) 
        {
            case 0:
                await connection.ExecuteNonQueryAsync(
                    SqlQueries.CreateStepStatsTable
                  + SqlQueries.CreateSessionsTable
                  + SqlQueries.CreateDbSchemaVersion);

                await connection.ExecuteNonQueryAsync($@"
                        INSERT INTO {TableNames.SchemaVersionTable} (""{ColumnNames.Version}"")
                        VALUES ({version})
                        ;");
                
                logger.Debug("Created initial tables");
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

