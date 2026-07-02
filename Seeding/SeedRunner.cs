using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FraudDetectionWorker.Database;

namespace FraudDetectionWorker.Seeding;

public class SeedRunner
{
    public static bool IsSeedMode(string[] args) => args.Contains("--seed");

    public static int GetSeedCount(string[] args)
    {
        int seedCount = 1000000;
        int countIndex = Array.IndexOf(args, "--count");
        if (countIndex != -1 && countIndex + 1 < args.Length)
        {
            if (int.TryParse(args[countIndex + 1], out int parsedCount))
            {
                seedCount = parsedCount;
            }
        }
        return seedCount;
    }

    public static void Run(IHost host, int seedCount)
    {
        var logger = host.Services.GetRequiredService<ILogger<SeedRunner>>();
        logger.LogInformation("Application running in SEED MODE. Target count: {Count}", seedCount);

        try
        {
            // 1. Build database/tables/indexes
            var schemaBuilder = host.Services.GetRequiredService<SchemaBuilder>();
            schemaBuilder.EnsureDatabaseAndSchemaCreated();

            // 2. Run Seeder
            var seeder = host.Services.GetRequiredService<DataSeeder>();
            seeder.SeedData(seedCount);

            logger.LogInformation("Seeding completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Seeding failed with an unhandled exception.");
            Environment.Exit(1);
        }
    }
}
