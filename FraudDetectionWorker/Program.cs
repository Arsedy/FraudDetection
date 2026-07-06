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

// Automatically scan and register all IFraudRule implementations in assembly
var ruleTypes = typeof(IFraudRule).Assembly.GetTypes()
    .Where(t => typeof(IFraudRule).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
foreach (var ruleType in ruleTypes)
{
    builder.Services.AddTransient(typeof(IFraudRule), ruleType);
}

// Worker registration
builder.Services.AddHostedService<FraudWorker>();

var host = builder.Build();

// Ensure DB is created/migrated and rules are synced on startup
using (var scope = host.Services.CreateScope())
{
    var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
    var progLogger = loggerFactory.CreateLogger("Program");
    
    try
    {
        progLogger.LogInformation("Initializing database schema...");
        var schemaBuilder = scope.ServiceProvider.GetRequiredService<SchemaBuilder>();
        schemaBuilder.EnsureDatabaseAndSchemaCreated();

        progLogger.LogInformation("Synchronizing fraud rules metadata to database...");
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var rules = scope.ServiceProvider.GetServices<IFraudRule>();
        await RuleSyncHelper.SyncRulesAsync(dbContext, rules);
        progLogger.LogInformation("Database initialization and rule synchronization complete.");
    }
    catch (Exception ex)
    {
        progLogger.LogError(ex, "An error occurred during database initialization or rule synchronization.");
        throw;
    }
}

if (isSeedMode)
{
    SeedRunner.Run(host, seedCount);
    return; // Exit application immediately
}

var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Starting Fraud Detection Background Service...");
await host.RunAsync(); // regular worker execution

