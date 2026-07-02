using FraudDetectionWorker;
using FraudDetectionWorker.Database;
using FraudDetectionWorker.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

bool isSeedMode = SeedRunner.IsSeedMode(args);
int seedCount = SeedRunner.GetSeedCount(args);

var builder = Host.CreateApplicationBuilder(args);

// Register DB services
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddTransient<SchemaBuilder>();
builder.Services.AddTransient<DataSeeder>();

// Worker registration
builder.Services.AddHostedService<FraudWorker>();

var host = builder.Build();

if (isSeedMode)
{
    SeedRunner.Run(host, seedCount);
    return; // Exit application immediately
}

var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Starting Fraud Detection Background Service...");
await host.RunAsync(); // regular worker execution

