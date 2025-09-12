using Npgsql;
using RepoDb;
using ILogger = Serilog.ILogger;

namespace NBomber.Sinks.Timescale.DAL;

internal class DbMigrations(NpgsqlConnection connection, ILogger logger)
{
    public const int SinkSchemaVersion = 3;

    public async Task Run()
    {
        var currentDbVersion = await GetCurrentDbVersion();

        if (currentDbVersion > SinkSchemaVersion)
        {
            var errMessage = $"NBomber.Sinks.Timescale: Your sink's schema version: '{SinkSchemaVersion}' is older than schema version in your database: '{currentDbVersion}'";
            logger.Error(errMessage);
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
                    await connection.ExecuteNonQueryAsync($@"
                        UPDATE {TableNames.SchemaVersionTable}
                        SET ""{ColumnNames.Version}"" = {version}
                        WHERE ""{ColumnNames.Version}"" < 1;
                        ");                    
                    break;

                case 2:
                    await connection.ExecuteNonQueryAsync(SqlQueries.CreateMetricsTable);
                    
                    await connection.ExecuteNonQueryAsync($@"
                        UPDATE {TableNames.SchemaVersionTable}
                        SET ""{ColumnNames.Version}"" = {version}
                        WHERE ""{ColumnNames.Version}"" < 2;
                        ");                    
                    break;

                case 3:
                    await connection.ExecuteNonQueryAsync(SqlQueries.AddSimulationNameColumn);

                    await connection.ExecuteNonQueryAsync($@"
                        UPDATE {TableNames.SchemaVersionTable}
                        SET ""{ColumnNames.Version}"" = {version}
                        WHERE ""{ColumnNames.Version}"" < 3;
                        ");                    
                    break;
            }
            
            await transaction.CommitAsync();
            logger.Debug($"Migrated to version {version}");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.Error(ex, $"Failed to migrate to version {version}. Transaction is being rolled back.");
        }        
    }
}

