using Npgsql;
using RepoDb;
using ILogger = Serilog.ILogger;

namespace NBomber.Sinks.Timescale.DAL;

internal class DbMigrations(NpgsqlConnection connection, ILogger logger)
{
    public const int SinkSchemaVersion = 7;

    public async Task Run()
    {
        var currentDbVersion = await GetCurrentDbVersion();

        if (currentDbVersion > SinkSchemaVersion)
        {
            var errMessage = $"NBomber.Sinks.Timescale: Your sink's schema version: '{SinkSchemaVersion}' is older than schema version in your database: '{currentDbVersion}'";
            logger.Warning(errMessage);
        }
        else if (currentDbVersion < SinkSchemaVersion) 
        {
            for (var v = currentDbVersion + 1; v <= SinkSchemaVersion; v++)
            {
                await ApplyMigration(v);
            }
        }
    }

    private async Task<int> GetCurrentDbVersion()
    {
        try
        {
            var result = await connection.ExecuteQueryAsync<int>($@"SELECT ""{ColumnNames.Version}"" FROM {TableNames.SchemaVersionTable};");
            var currentDbVersion = result.FirstOrDefault();
            return currentDbVersion;
        }
        catch (PostgresException ex)
        {
            if (ex.SqlState != "42P01") // table "nb_sink_schema_version" does't exist
                logger.Error(ex, ex.Message);
            
            return -1;
        }
        catch (Exception ex) 
        {
            logger.Error(ex, ex.Message);
            return -1;
        }
    }

    private async Task ApplyMigration(int version)
    {
        await using var transaction = await connection.BeginTransactionAsync();

        try
        {
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
                    break;

                case 1:
                    await connection.ExecuteNonQueryAsync($@"
                        ALTER TABLE {TableNames.SessionsTable}
                        ADD COLUMN IF NOT EXISTS {ColumnNames.LastUpdatedTime} TIMESTAMPTZ;
                        ");

                    await connection.ExecuteNonQueryAsync(SqlQueries.CreateDbSchemaVersion);
                    break;

                case 2:
                    await connection.ExecuteNonQueryAsync(SqlQueries.CreateMetricsTable);
                    break;

                case 3:
                    await connection.ExecuteNonQueryAsync(SqlQueries.AddSimulationNameColumn);
                    break;
                
                case 4:
                    await connection.ExecuteNonQueryAsync(SqlQueries.SetNewChunkInterval(TableNames.StepStatsTable, monthCount: 1));
                    await connection.ExecuteNonQueryAsync(SqlQueries.SetNewChunkInterval(TableNames.MetricsTable, monthCount: 1));
                    await connection.ExecuteNonQueryAsync(SqlQueries.AddSessionResultColumn);
                    break;

                case 5:
                    await connection.ExecuteNonQueryAsync(SqlQueries.AddArtifactsColumn);
                    break;

                case 6:
                    await connection.ExecuteNonQueryAsync(SqlQueries.AddBytesPerSecondColumns);
                    break;

                case 7:
                    await connection.ExecuteNonQueryAsync(SqlQueries.AddProjectIdColumn);
                    break;
            }
            
            if (version > 0)
                await connection.ExecuteNonQueryAsync($@"UPDATE {TableNames.SchemaVersionTable} SET ""{ColumnNames.Version}"" = {version};");
            
            await transaction.CommitAsync();
            logger.Debug($"NBomber.Sinks.Timescale migrated to version {version}");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.Error(ex, $"Failed to migrate to version {version}. Transaction is being rolled back.");
        }        
    }
}

