using System;
using System.IO;
using Microsoft.ML;
using Microsoft.ML.Data;
using Npgsql;
using Microsoft.Extensions.Configuration;
using FraudDetection.Shared.Models;

namespace FraudDetection.ML.Trainer;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=============================================================");
        Console.WriteLine("   Fraud Detection ML.NET Trainer — Offline Training Tool");
        Console.WriteLine("=============================================================");
        Console.WriteLine();

        // ──────────────────────────────────────────────────────────────
        // 1. Load Configuration
        // ──────────────────────────────────────────────────────────────
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        string connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found in appsettings.json.");

        // Determine output model path (default: solution root)
        string modelOutputPath = args.Length > 0 ? args[0] : Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "fraud_model.zip");
        modelOutputPath = Path.GetFullPath(modelOutputPath);

        Console.WriteLine($"  Database: {MaskConnectionString(connectionString)}");
        Console.WriteLine($"  Model output: {modelOutputPath}");
        Console.WriteLine();

        // ──────────────────────────────────────────────────────────────
        // 2. Verify Training Data Exists
        // ──────────────────────────────────────────────────────────────
        Console.WriteLine("[Step 1/5] Verifying training data availability...");

        long totalTransactions = 0;
        long fraudTransactions = 0;

        using (var conn = new NpgsqlConnection(connectionString))
        {
            conn.Open();

            using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM authorizationtransactions", conn))
            {
                totalTransactions = (long)(cmd.ExecuteScalar() ?? 0);
            }

            using (var cmd = new NpgsqlCommand("SELECT COUNT(DISTINCT transactionid) FROM fraudalerts", conn))
            {
                fraudTransactions = (long)(cmd.ExecuteScalar() ?? 0);
            }
        }

        if (totalTransactions == 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  ERROR: No transactions found in the database.");
            Console.WriteLine("  Please seed data first: dotnet run --project FraudDetectionWorker -- --seed --count 100000");
            Console.ResetColor();
            return;
        }

        if (fraudTransactions == 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  ERROR: No fraud alerts found in the database.");
            Console.WriteLine("  Please run the rules engine first: dotnet run --project FraudDetectionWorker");
            Console.WriteLine("  The trainer needs the existing rules engine output to derive training labels.");
            Console.ResetColor();
            return;
        }

        double fraudRate = (double)fraudTransactions / totalTransactions * 100;
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  ✓ Found {totalTransactions:N0} transactions ({fraudTransactions:N0} fraud-labeled, {fraudRate:F2}% fraud rate)");
        Console.ResetColor();
        Console.WriteLine();

        // ──────────────────────────────────────────────────────────────
        // 3. Initialize ML.NET and Load Data via DatabaseLoader
        // ──────────────────────────────────────────────────────────────
        Console.WriteLine("[Step 2/5] Loading training data from PostgreSQL via DatabaseLoader...");

        var mlContext = new MLContext(seed: 42);

        // SQL query that derives the binary label by LEFT JOINing with fraudalerts.
        // A transaction is labeled as fraud (true) if it has at least one matching fraud alert.
        string trainingQuery = @"
            SELECT 
                CAST(t.f4_amounttxn AS REAL) AS Amount,
                CAST(EXTRACT(HOUR FROM t.f12_localtime) AS REAL) AS LocalTimeHour,
                t.f18_mcc AS MCC,
                t.f19_acqcountry AS AcqCountry,
                t.f22_posentrymode AS POSEntryMode,
                t.f39_responsecode AS ResponseCode,
                t.f49_currencycode AS CurrencyCode,
                CASE WHEN fa.transactionid IS NOT NULL THEN TRUE ELSE FALSE END AS Label
            FROM authorizationtransactions t
            LEFT JOIN (
                SELECT DISTINCT transactionid FROM fraudalerts
            ) fa ON t.transactionid = fa.transactionid";

        var dbLoader = mlContext.Data.CreateDatabaseLoader<TransactionFeatures>();

        var dbSource = new DatabaseSource(
            NpgsqlFactory.Instance,
            connectionString,
            trainingQuery);

        IDataView fullData = dbLoader.Load(dbSource);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("  ✓ Data loaded successfully (streaming via DatabaseLoader — low memory footprint)");
        Console.ResetColor();
        Console.WriteLine();

        // ──────────────────────────────────────────────────────────────
        // 4. Build Pipeline and Train the Model
        // ──────────────────────────────────────────────────────────────
        Console.WriteLine("[Step 3/5] Building ML pipeline and splitting data (80/20)...");

        // Split into train/test sets (80% train, 20% test)
        var splitData = mlContext.Data.TrainTestSplit(fullData, testFraction: 0.2, seed: 42);

        // Feature engineering pipeline:
        //   - OneHotEncode categorical columns (MCC, Country, POS, ResponseCode, Currency)
        //   - Concatenate all features into a single "Features" vector
        //   - Train with LightGBM binary classifier
        var pipeline = mlContext.Transforms.Categorical.OneHotEncoding("MCC_Encoded", "MCC")
            .Append(mlContext.Transforms.Categorical.OneHotEncoding("AcqCountry_Encoded", "AcqCountry"))
            .Append(mlContext.Transforms.Categorical.OneHotEncoding("POSEntryMode_Encoded", "POSEntryMode"))
            .Append(mlContext.Transforms.Categorical.OneHotEncoding("ResponseCode_Encoded", "ResponseCode"))
            .Append(mlContext.Transforms.Categorical.OneHotEncoding("CurrencyCode_Encoded", "CurrencyCode"))
            .Append(mlContext.Transforms.NormalizeMinMax("Amount_Normalized", "Amount"))
            .Append(mlContext.Transforms.NormalizeMinMax("LocalTimeHour_Normalized", "LocalTimeHour"))
            .Append(mlContext.Transforms.Concatenate("Features",
                "Amount_Normalized",
                "LocalTimeHour_Normalized",
                "MCC_Encoded",
                "AcqCountry_Encoded",
                "POSEntryMode_Encoded",
                "ResponseCode_Encoded",
                "CurrencyCode_Encoded"))
            .Append(mlContext.BinaryClassification.Trainers.LightGbm(
                labelColumnName: "Label",
                featureColumnName: "Features",
                numberOfLeaves: 31,
                numberOfIterations: 200,
                minimumExampleCountPerLeaf: 20,
                learningRate: 0.1));

        Console.WriteLine("  Training with LightGBM binary classifier...");
        Console.WriteLine("  (This may take a few minutes depending on data volume)");
        Console.WriteLine();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var trainedModel = pipeline.Fit(splitData.TrainSet);
        stopwatch.Stop();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  ✓ Model trained in {stopwatch.Elapsed.TotalSeconds:F1} seconds");
        Console.ResetColor();
        Console.WriteLine();

        // ──────────────────────────────────────────────────────────────
        // 5. Evaluate the Model
        // ──────────────────────────────────────────────────────────────
        Console.WriteLine("[Step 4/5] Evaluating model on test set (20% holdout)...");

        var predictions = trainedModel.Transform(splitData.TestSet);
        var metrics = mlContext.BinaryClassification.Evaluate(predictions, labelColumnName: "Label");

        Console.WriteLine();
        Console.WriteLine("  ┌─────────────────────────────────────────────┐");
        Console.WriteLine("  │         MODEL EVALUATION RESULTS            │");
        Console.WriteLine("  ├─────────────────────────────────────────────┤");
        Console.WriteLine($"  │  Accuracy:        {metrics.Accuracy:P2}                │");
        Console.WriteLine($"  │  AUC (ROC):       {metrics.AreaUnderRocCurve:F4}                  │");
        Console.WriteLine($"  │  F1 Score:        {metrics.F1Score:F4}                  │");
        Console.WriteLine($"  │  Precision:       {metrics.PositivePrecision:F4}                  │");
        Console.WriteLine($"  │  Recall:          {metrics.PositiveRecall:F4}                  │");
        Console.WriteLine("  └─────────────────────────────────────────────┘");
        Console.WriteLine();

        // Print confusion matrix
        Console.WriteLine("  Confusion Matrix:");
        Console.WriteLine($"    ┌──────────────────────────────────────────┐");
        Console.WriteLine($"    │              Predicted                   │");
        Console.WriteLine($"    │           Clean      Fraud               │");
        Console.WriteLine($"    │  Actual                                  │");
        Console.WriteLine($"    │  Clean   {metrics.ConfusionMatrix.GetFormattedConfusionTable()}");
        Console.WriteLine($"    └──────────────────────────────────────────┘");
        Console.WriteLine();

        // Quality gate: warn if AUC is too low
        if (metrics.AreaUnderRocCurve < 0.70)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  ⚠ WARNING: AUC is below 0.70. The model may not be reliable enough.");
            Console.WriteLine("    Consider increasing training data volume or reviewing feature engineering.");
            Console.ResetColor();
            Console.WriteLine();
        }

        // ──────────────────────────────────────────────────────────────
        // 6. Save the Model
        // ──────────────────────────────────────────────────────────────
        Console.WriteLine("[Step 5/5] Saving trained model...");

        // Ensure output directory exists
        string? outputDir = Path.GetDirectoryName(modelOutputPath);
        if (!string.IsNullOrEmpty(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        mlContext.Model.Save(trainedModel, splitData.TrainSet.Schema, modelOutputPath);

        var fileInfo = new FileInfo(modelOutputPath);
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  ✓ Model saved to: {modelOutputPath}");
        Console.WriteLine($"  ✓ File size: {fileInfo.Length / 1024.0:F1} KB");
        Console.ResetColor();
        Console.WriteLine();

        Console.WriteLine("=============================================================");
        Console.WriteLine("   Training Complete! Next steps:");
        Console.WriteLine("   1. Run the hybrid worker: dotnet run --project FraudDetectionWorker");
        Console.WriteLine("      (The worker will automatically load fraud_model.zip)");
        Console.WriteLine("=============================================================");
    }

    /// <summary>
    /// Masks the password in a connection string for safe console output.
    /// </summary>
    private static string MaskConnectionString(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        builder.Password = "****";
        return builder.ConnectionString;
    }
}
