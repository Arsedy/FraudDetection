using FraudDetectionWorker;
using FraudDetectionWorker.Database;
using FraudDetectionWorker.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

// Check if --seed argument is passed
bool isSeedMode = args.Contains("--seed");
int seedCount = 1000000;

if (isSeedMode)
{
    // Check if a specific count was passed via --count <number>
    int countIndex = Array.IndexOf(args, "--count");
    if (countIndex != -1 && countIndex + 1 < args.Length)
    {
        if (int.TryParse(args[countIndex + 1], out int parsedCount))
        {
            seedCount = parsedCount;
        }
    }
}

var builder = Host.CreateApplicationBuilder(args);

// Register DB services
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddTransient<SchemaBuilder>();
builder.Services.AddTransient<DataSeeder>();

// Worker registration
builder.Services.AddHostedService<FraudWorker>();

var host = builder.Build();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

if (isSeedMode)
{
    logger.LogInformation("Application running in SEED MODE. Target count: {Count}", seedCount);

    try
    {
        // 1. Build database/tables/indexes
        var schemaBuilder = host.Services.GetRequiredService<SchemaBuilder>();
        schemaBuilder.EnsureDatabaseAndSchemaCreated();

        // 2. Run Seeder
        var seeder = host.Services.GetRequiredService<DataSeeder>();
        seeder.SeedData(seedCount);

        logger.LogInformation("Seeding completed successfully. Exiting application.");
        return; // Exit application immediately
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Seeding failed with an unhandled exception.");
        Environment.Exit(1);
    }
}

logger.LogInformation("Starting Fraud Detection Background Service...");
await host.RunAsync(); // regular worker execution
