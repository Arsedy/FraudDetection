using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FraudDetection.Worker.Database;

using Microsoft.Extensions.Configuration;

namespace FraudDetection.Worker.Seeding;

public class SeedRunner
{
    public static bool IsSeedMode(string[] args) => args.Contains("--seed");

    public static int GetSeedCount(string[] args, IConfiguration? config = null)
    {
        int defaultSeedCount = 100_000; // Default daily seed count if not specified (100k/day * 30 days = 3M total)

        if (config != null && int.TryParse(config["SeedCount"], out int configCount))
        {
            defaultSeedCount = configCount;
        }
        else if (int.TryParse(Environment.GetEnvironmentVariable("SeedCount"), out int envCount))
        {
            defaultSeedCount = envCount;
        }

        int seedCount = defaultSeedCount;
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
        logger.LogInformation("Application running in SEED MODE. Daily count: {Count} (Will generate 30 days of historical data)", seedCount);

        try
        {
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
