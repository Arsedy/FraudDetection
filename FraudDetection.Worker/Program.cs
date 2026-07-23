using FraudDetection.Worker;
using FraudDetection.Worker.Database;
using FraudDetection.Worker.Seeding;
using FraudDetection.Worker.Repositories;
using FraudDetection.Worker.Services;
using FraudDetection.Worker.Rules;
using FraudDetection.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ML;

bool isSeedMode = SeedRunner.IsSeedMode(args);
bool noMl = args.Contains("--no-ml");

var builder = Host.CreateApplicationBuilder(args);
int seedCount = SeedRunner.GetSeedCount(args, builder.Configuration);

// Register DB services
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddTransient<SchemaBuilder>();
builder.Services.AddTransient<DataSeeder>();

builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IFraudAlertRepository, FraudAlertRepository>();

// Automatically scan and register all IFraudRule implementations in assembly
var ruleTypes = typeof(IFraudRule).Assembly.GetTypes()
    .Where(t => typeof(IFraudRule).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
foreach (var ruleType in ruleTypes)
{
    builder.Services.AddTransient(typeof(IFraudRule), ruleType);
}

// ────────────────────────────────────────────────────────────────
// ML.NET PredictionEnginePool registration (hybrid mode)
// ────────────────────────────────────────────────────────────────
string[] candidatePaths = [
    Path.Combine(AppContext.BaseDirectory, "fraud_model.zip"),
    "/app/fraud_model.zip",
    Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "fraud_model.zip")),
    Path.GetFullPath("fraud_model.zip")
];
string modelPath = candidatePaths.FirstOrDefault(File.Exists) ?? candidatePaths.Last();

bool mlModelAvailable = !noMl && File.Exists(modelPath);

if (mlModelAvailable)
{
    builder.Services.AddPredictionEnginePool<TransactionFeatures, TransactionPrediction>()
        .FromFile(filePath: modelPath, watchForChanges: true);
}

// Register FraudDetectionEngine with ML awareness
builder.Services.AddScoped<IFraudDetectionEngine>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<FraudDetectionEngine>>();
    var rules = sp.GetRequiredService<IEnumerable<IFraudRule>>();
    var txnRepo = sp.GetRequiredService<ITransactionRepository>();
    var alertRepo = sp.GetRequiredService<IFraudAlertRepository>();

    PredictionEnginePool<TransactionFeatures, TransactionPrediction>? pool = null;
    if (mlModelAvailable)
    {
        pool = sp.GetService<PredictionEnginePool<TransactionFeatures, TransactionPrediction>>();
    }

    return new FraudDetectionEngine(logger, rules, txnRepo, alertRepo, pool, mlEnabled: mlModelAvailable);
});

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

        if (!await dbContext.AuthorizationTransactions.AnyAsync())// Check if seeding is needed by looking for existing transactions in the database
        {
            isSeedMode = true;
        }

        // Log ML model status
        if (noMl)
        {
            progLogger.LogInformation("ML scoring DISABLED (--no-ml flag specified). Running in pure rules-engine mode.");
        }
        else if (mlModelAvailable)
        {
            progLogger.LogInformation("ML scoring ENABLED. Model loaded from: {ModelPath}", modelPath);
        }
        else
        {
            progLogger.LogWarning("ML model not found at {ModelPath}. Running in pure rules-engine mode. " +
                "Train a model first: dotnet run --project FraudDetection.ML.Trainer", modelPath);
        }
    }
    catch (Exception ex)
    {
        progLogger.LogError(ex, "An error occurred during database initialization or rule synchronization.");
        throw;
    }
}

var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Starting Fraud Detection Background Service...");

if (isSeedMode)
{
    logger.LogInformation("Seed mode enabled. Seeding {Count} transactions into the database...", seedCount);
    SeedRunner.Run(host, seedCount);
    logger.LogInformation("Seeding complete. Starting Fraud Detection Background Service to process seeded transactions...");
}

await host.RunAsync();// regular worker execution


