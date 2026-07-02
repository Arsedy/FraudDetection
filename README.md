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
│   ├── Program.cs             # Main entry point (commands parsing & DI setup)
│   ├── FraudWorker.cs         # Background worker service (runs rules check loops)
│   ├── appsettings.json       # Default configuration & connection strings
│   │
│   ├── Database/
│   │   ├── AppDbContext.cs    # EF Core DbContext mapping tables & indexes
│   │   └── SchemaBuilder.cs   # Automatic database & schema creation helper
│   ├── Models/
│   │   ├── AuthorizationTransaction.cs # EF model for ISO 8583 transactions
│   │   ├── FraudAlert.cs      # EF model for flagged suspicious transactions
│   │   └── LastFraudCheckTime.cs # Tracks last timestamp processed
│   ├── Repositories/          # Repository pattern implementation
│   │   ├── ITransactionRepository.cs
│   │   └── TransactionRepository.cs
│   ├── Rules/
│   │   ├── IFraudRule.cs      # Interface for all fraud rules
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

## 🚀 How to Setup and Run

### 1. Prerequisites
Ensure you have the following installed on your machine:
* [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
* [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### 2. Start PostgreSQL Container
Spin up a local PostgreSQL container using Docker:
```bash
docker run --name postgres_db -e POSTGRES_PASSWORD=YourSecurePassword123! -p 5432:5432 -d postgres:latest
```

### 3. Clone and Download
Clone the repository to your local machine:
```bash
git clone https://github.com/Arsedy/FraudDetection
cd FraudDetection
```

### 4. Database Setup & Seeding
To automatically create the database, tables, indexes, and seed it with 1,000,000 mock transactions, run the seeding command:
```bash
dotnet run --project FraudDetectionWorker -- --seed --count 1000000
```
*(The seeder will check existing STAN/RRN counters in the database on consecutive runs and append data chronologically without duplicate key collisions.)*

### 5. Running the Background Service
To start the background worker service to monitor and check rules:
```bash
dotnet run --project FraudDetectionWorker
```

### 6. Running Tests
To run unit tests:
```bash
dotnet test
```