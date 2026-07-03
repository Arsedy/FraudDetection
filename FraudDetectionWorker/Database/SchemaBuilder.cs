using System;
using Npgsql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FraudDetectionWorker.Database;

public class SchemaBuilder
{
    private readonly string _connectionString;
    private readonly ILogger<SchemaBuilder> _logger;

    public SchemaBuilder(IConfiguration configuration, ILogger<SchemaBuilder> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        _logger = logger;
    }

    public void EnsureDatabaseAndSchemaCreated()
    {
        _logger.LogInformation("Checking database existence...");
        CreateDatabaseIfNotExist();

        _logger.LogInformation("Creating tables and indexes if they do not exist...");
        CreateTablesAndIndexes();
    }

    private void CreateDatabaseIfNotExist()
    {
        // Parse the connection string to find the database name and build a connection string for 'postgres' default database
        var builder = new NpgsqlConnectionStringBuilder(_connectionString);
        string targetDatabase = builder.Database ?? "fraud_detection";
        
        // Connect to 'postgres' system database to check/create the target database
        builder.Database = "postgres";
        string systemConnectionString = builder.ConnectionString;

        using var connection = new NpgsqlConnection(systemConnectionString);
        connection.Open();

        string checkDbQuery = $"SELECT 1 FROM pg_database WHERE datname = @dbName";
        using (var command = new NpgsqlCommand(checkDbQuery, connection))
        {
            command.Parameters.AddWithValue("dbName", targetDatabase);
            var result = command.ExecuteScalar();

            if (result == null)
            {
                _logger.LogInformation("Database '{Database}' does not exist. Creating database...", targetDatabase);
                // CREATE DATABASE cannot run inside a transaction block, which is standard in PG
                string createDbQuery = $"CREATE DATABASE \"{targetDatabase}\"";
                using var createCommand = new NpgsqlCommand(createDbQuery, connection);
                createCommand.ExecuteNonQuery();
                _logger.LogInformation("Database '{Database}' created successfully.", targetDatabase);
            }
            else
            {
                _logger.LogInformation("Database '{Database}' already exists.", targetDatabase);
            }
        }
    }

    private void CreateTablesAndIndexes()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();

        // 1. Create AuthorizationTransactions Table
        string createAuthTableSql = @"
        CREATE TABLE IF NOT EXISTS AuthorizationTransactions (
            TransactionId UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            MTI CHAR(4) NOT NULL,
            F2_PAN VARCHAR(19) NOT NULL,
            F3_ProcCode CHAR(6) NOT NULL,
            F4_AmountTxn DECIMAL(18,2) NOT NULL,
            F7_TxnDateTime TIMESTAMPTZ NOT NULL,
            F11_STAN CHAR(6) NOT NULL,
            F12_LocalTime TIME NOT NULL,
            F13_LocalDate DATE NOT NULL,
            F14_ExpDate CHAR(4) NOT NULL,
            F18_MCC CHAR(4) NOT NULL,
            F19_AcqCountry CHAR(3) NOT NULL,
            F22_POSEntryMode CHAR(3) NOT NULL,
            F37_RRN CHAR(12) NOT NULL UNIQUE,
            F38_AuthCode CHAR(6) NULL,
            F39_ResponseCode CHAR(2) NOT NULL,
            F41_TID CHAR(8) NOT NULL,
            F42_MID VARCHAR(15) NOT NULL,
            F43_MerchantLoc VARCHAR(40) NOT NULL,
            F49_CurrencyCode CHAR(3) NOT NULL
        );";

        using (var cmd = new NpgsqlCommand(createAuthTableSql, connection))
        {
            cmd.ExecuteNonQuery();
        }



        // 2b. Create FraudAlerts Table
        string createFraudAlertsTableSql = @"
        CREATE TABLE IF NOT EXISTS fraudalerts (
            alertid UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            transactionid UUID NOT NULL,
            f2_pan VARCHAR(19) NOT NULL,
            rulename VARCHAR(50) NOT NULL,
            description VARCHAR(250) NOT NULL,
            isreviewed BOOLEAN NOT NULL DEFAULT FALSE,
            flaggedat TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
            CONSTRAINT FK_FraudAlert_Transaction FOREIGN KEY (transactionid) REFERENCES AuthorizationTransactions(TransactionId)
        );";

        using (var cmd = new NpgsqlCommand(createFraudAlertsTableSql, connection))
        {
            cmd.ExecuteNonQuery();
        }

        // 3. Create point lookup index
        string createBTreeIndexSql = @"
        CREATE INDEX IF NOT EXISTS IX_Auth_PAN_DateTime ON AuthorizationTransactions(F2_PAN, F7_TxnDateTime);";

        using (var cmd = new NpgsqlCommand(createBTreeIndexSql, connection))
        {
            cmd.ExecuteNonQuery();
        }

        // 4. Create composite analytical index for covering queries (equivalent to Columnstore optimization on PG)
        string createCoveringIndexSql = @"
        CREATE INDEX IF NOT EXISTS IX_Auth_Analytics ON AuthorizationTransactions (
            F2_PAN, F4_AmountTxn, F7_TxnDateTime, F18_MCC, F19_AcqCountry, F22_POSEntryMode
        );";

        using (var cmd = new NpgsqlCommand(createCoveringIndexSql, connection))
        {
            cmd.ExecuteNonQuery();
            _logger.LogInformation("Covering B-Tree Index 'IX_Auth_Analytics' created successfully for high-volume aggregations.");
        }
    }
}
