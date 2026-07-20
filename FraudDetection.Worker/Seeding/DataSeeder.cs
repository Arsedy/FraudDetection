using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using Npgsql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FraudDetection.Worker.Seeding;

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

    public void SeedData(int dailyCount = 1000000, int batchSize = 100000)
    {
        int totalDays = 30;
        int totalCount = dailyCount * totalDays;

        _logger.LogInformation("Starting 30-day seeding process. Daily Count: {DailyCount}, Total Expected: {TotalCount} transactions.", dailyCount, totalCount);

        long startStan = 0;
        long startRrn = 100000000000;

        GetLastSequences(ref startStan, ref startRrn);
        _logger.LogInformation("Resuming/starting with STAN: {Stan}, RRN: {Rrn}", startStan, startRrn);

        // Scale card pool to daily count so fraud patterns don't pile up on the same PANs across 30 days
        int cardPoolSize = Math.Max(50000, dailyCount);
        var generator = new DataGenerator(cardPoolSize);
        var totalStopwatch = Stopwatch.StartNew();

        int totalSeededCount = 0;
        int batchIndex = 1;

        // Loop from 30 days ago to yesterday
        for (int dayOffset = -totalDays; dayOffset <= -1; dayOffset++)
        {
            DateTime baseDate = DateTime.UtcNow.Date.AddDays(dayOffset);
            _logger.LogInformation("Seeding Day {DayOffset} (Date: {BaseDate:yyyy-MM-dd})...", dayOffset, baseDate);

            int seededForDay = 0;
            while (seededForDay < dailyCount)
            {
                int currentBatchSize = Math.Min(batchSize, dailyCount - seededForDay);

                var batchStopwatch = Stopwatch.StartNew();
                var auths = generator.GenerateBatch(currentBatchSize, ref startStan, ref startRrn, baseDate);
                batchStopwatch.Stop();

                batchStopwatch.Restart();
                BulkInsert(auths, "authorizationtransactions");
                batchStopwatch.Stop();

                seededForDay += currentBatchSize;
                totalSeededCount += currentBatchSize;

                _logger.LogInformation("  [Day {DayOffset} | Batch {BatchIndex}] Inserted {BatchSize} rows. Day progress: {DayCount}/{DailyCount} (Total: {TotalCount}/{ExpectedTotal})",
                    dayOffset, batchIndex, currentBatchSize, seededForDay, dailyCount, totalSeededCount, totalCount);

                batchIndex++;
            }
        }

        totalStopwatch.Stop();
        _logger.LogInformation("Successfully completed seeding of {TotalCount} rows across {TotalDays} days in {ElapsedSeconds} seconds.",
            totalSeededCount, totalDays, totalStopwatch.Elapsed.TotalSeconds);
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
