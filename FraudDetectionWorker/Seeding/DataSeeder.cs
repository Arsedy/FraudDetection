using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using Npgsql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FraudDetectionWorker.Seeding;

public class DataSeeder
{
    private readonly string _connectionString;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(IConfiguration configuration, ILogger<DataSeeder> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        _logger = logger;
    }

    public void SeedData(int totalCount = 1000000, int batchSize = 100000)
    {
        _logger.LogInformation("Starting seeding process for {TotalCount} transactions into PostgreSQL...", totalCount);
        
        long startStan = 0;
        long startRrn = 100000000000; // 12 digits minimum starting point

        // Get last sequence numbers from DB to support appending later
        GetLastSequences(ref startStan, ref startRrn);
        _logger.LogInformation("Resuming/starting with STAN: {Stan}, RRN: {Rrn}", startStan, startRrn);

        var generator = new DataGenerator();
        var totalStopwatch = Stopwatch.StartNew();

        int seededCount = 0;
        int batchIndex = 1;
        DateTime baseDate = DateTime.UtcNow.AddDays(-90);

        while (seededCount < totalCount)
        {
            int currentBatchSize = Math.Min(batchSize, totalCount - seededCount);
            _logger.LogInformation("Generating batch {BatchIndex} of size {Size}...", batchIndex, currentBatchSize);

            var batchStopwatch = Stopwatch.StartNew();
            var auths = generator.GenerateBatch(currentBatchSize, ref startStan, ref startRrn, baseDate);
            batchStopwatch.Stop();

            _logger.LogInformation("Generated batch in {ElapsedMs} ms. Bulk importing to PostgreSQL...", batchStopwatch.ElapsedMilliseconds);

            batchStopwatch.Restart();
            BulkInsert(auths, "authorizationtransactions");
            batchStopwatch.Stop();

            seededCount += currentBatchSize;
            _logger.LogInformation("Inserted batch in {ElapsedMs} ms. Total seeded so far: {Count}/{Total}", 
                batchStopwatch.ElapsedMilliseconds, seededCount, totalCount);

            batchIndex++;
        }

        totalStopwatch.Stop();
        _logger.LogInformation("Successfully completed seeding of {TotalCount} rows in {ElapsedSeconds} seconds.", 
            totalCount, totalStopwatch.Elapsed.TotalSeconds);
    }

    private void GetLastSequences(ref long maxStan, ref long maxRrn)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();

        // Check if table contains data
        string checkQuery = "SELECT COUNT(*) FROM authorizationtransactions";
        using (var checkCmd = new NpgsqlCommand(checkQuery, connection))
        {
            long count = Convert.ToInt64(checkCmd.ExecuteScalar());
            if (count == 0) return;
        }

        string query = @"
            SELECT 
                COALESCE(MAX(CAST(f11_stan AS BIGINT)), 0) as MaxStan,
                COALESCE(MAX(CAST(f37_rrn AS BIGINT)), 0) as MaxRrn
            FROM authorizationtransactions";

        using var cmd = new NpgsqlCommand(query, connection);
        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            maxStan = Convert.ToInt64(reader["MaxStan"]);
            maxRrn = Math.Max(maxRrn, Convert.ToInt64(reader["MaxRrn"]));
        }
    }

    private void BulkInsert(DataTable table, string destinationTable)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();

        // Build the COPY command with columns
        var columns = new List<string>();
        foreach (DataColumn column in table.Columns)
        {
            columns.Add($"\"{column.ColumnName.ToLowerInvariant()}\""); // Use lowercase to match folded columns in PG
        }
        
        string copyCommand = $"COPY \"{destinationTable}\" ({string.Join(", ", columns)}) FROM STDIN (FORMAT BINARY)";

        using var writer = connection.BeginBinaryImport(copyCommand);
        foreach (DataRow row in table.Rows)
        {
            writer.StartRow();
            for (int i = 0; i < table.Columns.Count; i++)
            {
                var val = row[i];
                var columnName = table.Columns[i].ColumnName;

                if (val == DBNull.Value || val == null)
                {
                    writer.WriteNull();
                }
                else if (val is TimeSpan ts)
                {
                    writer.Write(TimeOnly.FromTimeSpan(ts)); // Map TimeSpan to TimeOnly for PG TIME format
                }
                else if (val is DateTime dt)
                {
                    if (columnName.EndsWith("Date", StringComparison.OrdinalIgnoreCase))
                    {
                        writer.Write(DateOnly.FromDateTime(dt)); // Map to PG DATE format
                    }
                    else if (dt.Kind == DateTimeKind.Unspecified)
                    {
                        writer.Write(DateTime.SpecifyKind(dt, DateTimeKind.Utc));
                    }
                    else
                    {
                        writer.Write(val);
                    }
                }
                else
                {
                    writer.Write(val);
                }
            }
        }
        writer.Complete();
    }
}
