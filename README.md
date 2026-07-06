# Fraud Detection Background Worker

A high-performance .NET 9.0 background worker designed to detect financial transaction fraud. The application processes payment authorizations conforming to the **ISO 8583 standard**, runs them against configurable rules, and flags suspicious transactions.

---

## 🎯 Purpose of the Project

The purpose of this project is to simulate and monitor credit card transaction flows, identify fraudulent activities (such as rapid velocity swiping, card credential testing, and geographic impossible travel anomalies), and log alerts for security analysts. It is built as a highly scalable background service that operates efficiently over large volumes of database transactions.

---

## 🏗️ Folder Structure

```text
FraudDetection/ (Solution Root)
│
├── FraudDetection.sln         # Solution file
├── README.md                  # Main documentation
├── .gitignore                 # Exclusion rules for git
│
├── FraudDetectionWorker/      # 📁 Worker Service Project
│   ├── FraudDetectionWorker.csproj
│   ├── Program.cs             # Main entry point (commands parsing & Dependency Injection setup)
│   ├── FraudWorker.cs         # Background worker service (creates DI scopes & runs rules loop)
│   ├── appsettings.json       # Default configuration & connection strings
│   │
│   ├── Database/
│   │   ├── AppDbContext.cs    # EF Core DbContext mapping tables & indexes
│   │   ├── RuleSyncHelper.cs  # Syncs code-defined rules to the database
│   │   └── SchemaBuilder.cs   # Automatic database & schema creation helper
│   ├── Models/
│   │   ├── AuthorizationTransaction.cs # EF model for ISO 8583 transactions
│   │   ├── FraudAlert.cs      # EF model for flagged suspicious transactions
│   │   └── Rule.cs            # EF model for fraud rules
│   ├── Repositories/          # Repository pattern implementation
│   │   ├── IFraudAlertRepository.cs
│   │   ├── FraudAlertRepository.cs
│   │   ├── ITransactionRepository.cs
│   │   └── TransactionRepository.cs
│   ├── Rules/
│   │   ├── IFraudRule.cs      # Interface for all fraud rules
│   │   ├── CardTestingRule.cs # Rule checking accepted/declined ratio
│   │   ├── ExpiryDateRule.cs  # Rule checking brute-force expiry date guessing
│   │   ├── SpikeRule.cs       # Rule checking massive amount spikes
│   │   ├── TravelRule.cs      # Rule checking geographic impossible travel
│   │   └── VelocityRule.cs    # Rule checking card velocity limits
│   ├── Seeding/
│   │   ├── DataGenerator.cs   # Mock data generator (Luhn check, fraud patterns)
│   │   ├── DataSeeder.cs      # High-speed binary COPY seeder for PostgreSQL
│   │   └── SeedRunner.cs      # Orchestrates DB schema build and seeding execution
│   └── Services/              # Application business services
│       ├── FraudDetectionEngine.cs
│       └── IFraudDetectionEngine.cs
│
└── Tests/                     # 📁 Unit Tests Project
    ├── Tests.csproj           # xUnit test configuration
    ├── FraudTestData.cs       # Static datasets for testing fraud patterns
    └── UnitTest1.cs           # Velocity rule unit test
```

---

## ⚙️ Technical Features

* **ISO 8583 Specification**: Maps financial transaction card message fields case-insensitively to standard database columns (e.g., `f2_pan` for card number, `f11_stan` for trace numbers, `f18_mcc` for merchant categories, `f37_rrn` for retrieval reference numbers, and `f39_responsecode` for transaction results).
* **High-Speed Binary Seeding**: Utilizes PostgreSQL's native binary `COPY` protocol (`BeginBinaryImport` in Npgsql) to seed **1,000,000 rows of mock data in under 15 seconds** with safe datatype formatting (converting `TimeSpan` to `TimeOnly` and UTC offsets).
* **Database Optimization**: Implements B-Tree compound indexing on `(f2_pan, f7_txndatetime)` to fetch a card's historical swipes in milliseconds, bypassing the need for scanning the entire table.
* **Separation of Concerns**: Uses the **Repository Pattern** to separate raw database query implementation from pure business rule checking, making unit testing clean and mockable.
* **Robust State Tracking**: Designed to run via a **Timestamp Sliding Window** or **Status Flagging**, preventing repetitive scans of older transactions and saving memory.

---

## 🛡️ Fraud Rules

Each rule receives a card's full transaction history (grouped by `F2_PAN`) and returns the `TransactionId` of the offending transaction if fraud is detected.

| Rule | Detection Logic | Key Thresholds |
|------|----------------|----------------|
| **Velocity Rule** | Sliding time window over chronologically sorted transactions. Counts how many transactions fall within a rolling 10-minute window using a Queue. | > 5 transactions in 10 minutes |
| **Travel Rule** | Sorts transactions by time, then checks consecutive pairs for a country change. If two consecutive transactions are from different countries within a physically impossible travel time, it flags the second one. | Different country within < 1 hour |
| **Card Testing Rule** | Counts declined transactions (response code ≠ "00"). If there are 3+ declines, it looks for the first approved transaction after the last decline whose amount exceeds 5× the average decline amount. | ≥ 3 declines + approved amount > 5× avg decline |
| **Spike Rule** | Calculates the median transaction amount for a card's history, then checks if the latest transaction exceeds 5× the median. | Latest amount > 5× median |
| **Expiry Date Rule** | Detects brute-force expiry date guessing by counting distinct expiration dates used for the same card within a short rolling time window. | ≥ 3 distinct dates in 5 minutes |

> **Note:** The mock data seeder assigns each card a consistent "home country" so that normal transactions never cross borders. Only the deliberately injected `ImpossibleTravelPattern` creates cross-country activity within a short time window.


## 🔄 Architecture & Workflow

The application leverages .NET Dependency Injection to cleanly separate concerns across the background service lifecycle:

1. **`Program.cs` (Configuration):** Registers all database contexts (Scoped), Repositories (Scoped), the `FraudDetectionEngine` (Scoped), and dynamically injects multiple `IFraudRule` implementations. It also automatically synchronizes the in-code rules to the `rules` database table on startup, and registers the `FraudWorker` as a Singleton Hosted Service.
2. **`FraudWorker.cs` (Scheduler):** Runs continuously in the background. Because it is a Singleton, it uses `IServiceScopeFactory` to create a fresh Dependency Injection scope for each processing cycle, safely retrieving scoped database services without memory leaks.
3. **`FraudDetectionEngine.cs` (Orchestrator):** Requests batched transaction data from the `ITransactionRepository`, groups the historical data by card (`F2_PAN`), and dynamically evaluates the history against the injected collection of `IFraudRule` implementations.
4. **`IFraudRule` (Business Logic):** Pure logic classes (e.g., `VelocityRule`, `TravelRule`, `SpikeRule`, `CardTestingRule`). If a threshold is exceeded, the rule returns a `RuleResult` containing the specific `TransactionId` that caused the breach.
5. **`FraudAlertRepository` (Persistence):** If the Engine detects a fraud rule breach, it utilizes this repository to log the details to the `fraudalerts` table for analyst review.

---

## 🚀 How to Setup and Test the Pipeline

### 1. Prerequisites
Ensure you have the following installed on your machine:
* [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
* [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### 2. Start PostgreSQL Container
Spin up a local PostgreSQL container using Docker. This runs the database in the background:
```bash
docker run --name postgres_db -e POSTGRES_PASSWORD=YourSecurePassword123! -p 5432:5432 -d postgres:latest
```

### 3. Testing the System (Step-by-Step)
The seeder is designed to generate a full **30 days** of historical transaction data. Exactly **10%** of the generated data will be fraudulent, distributed across all 5 rules. The background worker simulates a fast-forward progression, analyzing each historical day with a 5-second sleep in between, until it reaches the present day.

**Step A: Seed the Database**
To automatically create the database schemas and seed it with 100,000 mock transactions **per day**, run:
```bash
dotnet run --project FraudDetectionWorker -- --seed --count 100000
```
*(This generates 100,000 transactions per day for 30 days, resulting in 3,000,000 total rows. It automatically generates the `fraud_detection` DB if it doesn't exist).*

**Step B: Run the Background Worker**
Start the background engine to process the data:
```bash
dotnet run --project FraudDetectionWorker
```
The worker will boot up and start processing data from 30 days ago. It will flag the fraudulent patterns it detects, insert them into the `fraudalerts` table, and sleep for 5 seconds between each historical day. You can press `Ctrl+C` to stop it once it reaches the current day and says "Sleeping for 24 hours".


**Step C: View the Results (DBeaver/SQL)**
You can connect to `localhost:5432` with username `postgres` and password `YourSecurePassword123!` using any SQL client (like DBeaver) to view the `fraudalerts` table.

Alternatively, you can instantly see a summary of the triggered alerts right in your terminal by running this Docker command:
```bash
docker exec postgres_db psql -U postgres -d fraud_detection -c "SELECT rulename, count(*) FROM fraudalerts GROUP BY rulename;"
```

**Verify the 10% Fraud Hit Rate**
If you want to verify that the mock data generator correctly seeded 10% fraudulent activity, you can count the unique credit cards (`F2_PAN`) in both tables. This query shows how many distinct cards were processed vs how many unique cards were flagged:
```bash
docker exec postgres_db psql -U postgres -d fraud_detection -c "SELECT 'Total Cards Processed' AS Metric, COUNT(DISTINCT f2_pan) AS Unique_Cards FROM authorizationtransactions UNION ALL SELECT 'Compromised Cards (Fraud)', COUNT(DISTINCT f2_pan) FROM fraudalerts;"
```

### 4. Resetting the Database for Re-Testing
If you want to clear out the database and start a fresh test run, you can drop the entire schema using this Docker command:
```bash
docker exec postgres_db psql -U postgres -d fraud_detection -c "DROP TABLE IF EXISTS fraudalerts, authorizationtransactions;"
```
Once dropped, just repeat **Step A** and **Step B** to generate a brand new set of randomized fraud data!

---

### Running Unit Tests
To run the automated unit tests:
```bash
dotnet test
```