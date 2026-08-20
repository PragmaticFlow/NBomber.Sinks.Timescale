using NBomber.Sinks.Timescale.Contracts;
using NBomber.Sinks.Timescale.DAL;
using Npgsql;
using RepoDb;
using System.IO.Compression;
using System.Text;

namespace NBomber.Sinks.Timescale.Tests.Infra;

public class TestHelper(NpgsqlDataSource dataSource)
{
    public async Task CreateTables()
    {
        await using var connection = await dataSource.OpenConnectionAsync();

        await connection.ExecuteNonQueryAsync(
                    SqlQueries.CreateStepStatsTable
                  + SqlQueries.CreateSessionsTable
                  + SqlQueries.CreateDbSchemaVersion);
    }

    public async Task SetDbSchemaVersion(int version)
    {
        await using var connection = await dataSource.OpenConnectionAsync();

        await connection.ExecuteNonQueryAsync($@"
                            INSERT INTO {TableNames.SchemaVersionTable} (""{ColumnNames.Version}"")
                            VALUES ({version})
                            ;");
    }

    public async Task DeleteTables()
    {
        await using var connection = await dataSource.OpenConnectionAsync();

        await connection.ExecuteNonQueryAsync
            (@$"DROP TABLE IF EXISTS {TableNames.SchemaVersionTable}; 
                    DROP TABLE IF EXISTS {TableNames.SessionsTable};
                    DROP TABLE IF EXISTS {TableNames.StepStatsTable};
                    DROP TABLE IF EXISTS {TableNames.MetricsTable};");
    }

    public async Task NotifyStopSession(string sessionId)
    {
        await using var connection = await dataSource.OpenConnectionAsync();

        await connection.ExecuteNonQueryAsync($"SELECT PG_NOTIFY('nbomber_stop_session__{sessionId.Replace('-', '_')}', '')");
    }

    public async Task<int> GetDBSchemaVersion()
    {
        await using var connection = await dataSource.OpenConnectionAsync();

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

    public async Task<int> GetRowsCount(string tableName)
    {
        await using var connection = await dataSource.OpenConnectionAsync();

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

    internal async Task<StepStatsDbRecord[]> GetStepStats(string sessionId)
    {
        await using var connection = await dataSource.OpenConnectionAsync();

        var result = await connection.QueryAsync<StepStatsDbRecord>(
            tableName: TableNames.StepStatsTable,
            where: stats => stats.SessionId == sessionId
        );

        return result.ToArray();
    }

    public async Task<Dictionary<string, string>> GetSessionArtifacts(string sessionId)
    {
        await using var connection = await dataSource.OpenConnectionAsync();

        try
        {
            var result = await connection.ExecuteQueryAsync<byte[]>(
                $@"SELECT {ColumnNames.Artifacts}
                    FROM {TableNames.SessionsTable}
                    WHERE session_id = @sessionId",
                new { sessionId }
            );

            return result.Any() ? UnzipReportFiles(result.First()) : [];
        }
        catch
        {
            return [];
        }
    }

    public static Dictionary<string, string> UnzipReportFiles(byte[] artifacts)
    {
        using var memoryStream = new MemoryStream(artifacts);
        using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read);

        return archive.Entries
            .Select(entry =>
            {
                using var stream = entry.Open();
                using var reader = new StreamReader(stream, Encoding.UTF8);
                return (entry.Name, reader.ReadToEnd());
            })
            .ToDictionary();
    }
}
