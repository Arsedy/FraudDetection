using FraudDetectionWorker;
using FraudDetectionWorker.Database;
using FraudDetectionWorker.Seeding;
using FraudDetectionWorker.Repositories;
using FraudDetectionWorker.Services;
using FraudDetectionWorker.Rules;
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

builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IFraudDetectionEngine, FraudDetectionEngine>();
builder.Services.AddScoped<IFraudAlertRepository, FraudAlertRepository>();
builder.Services.AddTransient<IFraudRule, CardTestingRule>();
builder.Services.AddTransient<IFraudRule, VelocityRule>();
builder.Services.AddTransient<IFraudRule, SpikeRule>();
builder.Services.AddTransient<IFraudRule, TravelRule>();

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

