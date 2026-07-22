# Hybrid Fraud Detection System

A high-performance .NET 9.0 application designed to detect financial transaction fraud (conforming to the **ISO 8583 standard**) using a hybrid architecture. It integrates an **ML.NET binary classification model** as a fast pre-filter to bypass or invoke a database-heavy, code-defined **Rules Engine** (`VelocityRule`, `TravelRule`, `CardTestingRule`, etc.). 

The solution includes an offline **ML Training Console Application**, an **ASP.NET Core Web API** (with Swagger UI) to expose analytics and ML prediction endpoints, and a premium **Blazor Admin Dashboard** to visualize alerts, inspect transactions, and manually score them against the ML model.

---

## 🏗️ Project Architecture & Folder Structure

This solution is divided into clean, decoupled projects:

```text
FraudDetection/ (Solution Root)
│
├── .github/
│   └── workflows/
│       └── dotnet-ci.yml          # Automated GitHub Actions CI/CD pipeline
├── FraudDetection.sln             # Solution file
├── README.md                      # Main documentation
├── .gitignore                     # Exclusion rules for git
├── .dockerignore                  # Docker build exclusions
├── docker-compose.yml             # Docker Compose orchestration (all services)
│
├── FraudDetection.Shared/             # 📁 Shared Class Library
│   ├── Models/
│   │   ├── TransactionFeatures.cs     # ML.NET input schema (features)
│   │   └── TransactionPrediction.cs   # ML.NET output schema (score/probability)
│   └── FraudDetection.Shared.csproj
│
├── FraudDetection.ML.Trainer/         # 📁 Offline ML.NET Training App (Console)
│   ├── Program.cs                     # Streams DB, trains LightGBM, saves model.zip
│   ├── appsettings.json               # Connection string config
│   └── FraudDetection.ML.Trainer.csproj
│
├── FraudDetection.Worker/              # 📁 Worker Service Project (Rules & DI Host)
│   ├── Program.cs                     # CLI runner & DI configuration (PredictionEnginePool)
│   ├── FraudWorker.cs                 # Background worker execution loop with checkpoint resume
│   ├── Dockerfile                     # Container build definition
│   ├── appsettings.json               # Worker configuration & connection strings
│   ├── Database/
│   │   ├── AppDbContext.cs            # EF Core DbContext mapping tables & indexes
│   │   ├── RuleSyncHelper.cs          # Synchronizes rule metadata & ML synthetic rules
│   │   └── SchemaBuilder.cs           # Auto DB schema creator with composite indexes
│   ├── Models/
│   │   ├── AuthorizationTransaction.cs # EF model for ISO 8583 transactions
│   │   ├── FraudAlert.cs              # EF model for flagged fraud alerts
│   │   ├── FraudCheckState.cs         # High-watermark checkpoint state tracking
│   │   └── Rule.cs                    # EF model for active rules metadata
│   ├── Repositories/                  # Repository pattern
│   │   ├── FraudAlertRepository.cs
│   │   └── TransactionRepository.cs
│   ├── Rules/                         # Rules Engine implementations
│   │   ├── IFraudRule.cs              # Rule interface & RuleResult contract
│   │   ├── CardTestingRule.cs         # Declines ratio vs approval spikes
│   │   ├── ExpiryDateRule.cs          # Brute-force expiration date guessing
│   │   ├── SpikeRule.cs               # Transaction amount vs median amount
│   │   ├── TravelRule.cs              # Impossible speed/geographic travel
│   │   └── VelocityRule.cs            # Card swipe frequency window
│   └── Seeding/
│       ├── DataGenerator.cs           # Mock transaction generator (10% fraud hit rate)
│       ├── DataSeeder.cs              # High-speed binary COPY seeder
│       └── SeedRunner.cs              # Seed orchestration
│
├── FraudDetection.WebAPI/             # 📁 Read-Only Monitoring API (ASP.NET Core)
│   ├── Program.cs                     # CORS, EF Core, Swagger UI, controllers mapping
│   ├── Dockerfile                     # Container build definition
│   ├── appsettings.json               # Web API configuration
│   ├── Controllers/
│   │   ├── AlertsController.cs        # Paginated alerts & review status toggle
│   │   ├── MetricsController.cs       # KPI aggregates, alerts-by-rule, daily metrics
│   │   ├── MLController.cs            # ML.NET prediction endpoint (POST /api/ml/predict)
│   │   ├── RulesController.cs         # Fetch active rules metadata
│   │   └── TransactionsController.cs  # Paginated transaction logs & single-txn lookup
│   └── Properties/launchSettings.json
│
├── FraudDetection.Dashboard/          # 📁 Blazor Dashboard (Interactive Server UI)
│   ├── Program.cs                     # Blazor host & API HttpClient configuration
│   ├── Dockerfile                     # Container build definition
│   ├── appsettings.json               # Dashboard configuration pointing to API
│   ├── _Imports.razor                 # Global imports & render modes
│   ├── wwwroot/
│   │   └── app.css                    # Premium design system (dark mode, glassmorphism)
│   └── Components/
│       ├── App.razor                  # Root HTML document
│       ├── Routes.razor               # Route routing host
│       ├── Layout/
│       │   └── MainLayout.razor       # Sidebar navigation shell
│       └── Pages/
│           ├── Home.razor             # KPIs, css progress bars, alerts overview
│           ├── Alerts.razor           # Paginated datagrid with review action, PAN filters
│           ├── Transactions.razor     # Transaction log with inspect, copy, & ML score actions
│           ├── Model.razor            # ML Score Checker — manual or auto-fill scoring
│           └── Rules.razor            # Grid cards listing active rules
│
└── Tests/                             # 📁 Unit Tests Project
    ├── Tests.csproj
    ├── FraudTestData.cs               # Static datasets for testing fraud patterns
    └── UnitTest1.cs                   # Velocity rule unit test
```

---

## 🔄 Hybrid Flow Pipeline

When a transaction is processed by the hybrid background service, it executes the following path:

```
                  ┌──────────────────────────────┐
                  │ Transaction Record Ingestion │
                  └──────────────┬───────────────┘
                                 │
                 [Score with ML.NET Model (0.0-1.0)]
                                 │
           ┌─────────────────────┼─────────────────────┐
           │                     │                     │
      Score < 0.30         0.30 <= Score < 0.85     Score >= 0.85
           │                     │                     │
    ┌──────▼──────┐       ┌──────▼──────┐       ┌──────▼──────┐
    │  Bypass DB  │       │ Query DB for│       │ Bypass DB   │
    │ Rules Checks│       │ Card History│       │ Rules Check │
    │   (Clean)   │       └──────┬──────┘       │   (Fraud)   │
    └─────────────┘              │              └──────┬──────┘
                          [Evaluate Rules]             │
                                 │              ┌──────▼──────┐
                          Flagged? ────► Yes ──►│ Write Alert │
                                 │              │  to DB log  │
                                 No             └─────────────┘
                                 │
                            ┌────▼────┐
                            │ Clean / │
                            │ Ignore  │
                            └─────────┘
```

1. **ML Score Gen**: ML.NET calculates the fraud probability of a transaction based strictly on its static parameters (Amount, MCC, Country, POS mode, Currency, Hour). No history lookup is performed at this stage.
2. **Score < 0.30 (Clean)**: Skips the rules engine completely. Saves DB query execution.
3. **Score >= 0.85 (Immediate Fraud)**: Skips the rules engine, flags immediately under the `ML_HighConfidenceFraud` rule to notify analysts.
4. **Grey Area (0.30 - 0.84)**: Queries transaction history and runs the existing EF-backed Rules Engine (`VelocityRule`, `TravelRule`, etc.).

---

## ⚡ Technical Features

* **LightGBM Classification**: Employs ML.NET's high-speed LightGBM binary classifier for lightning-fast tabular predictions.
* **Low-Memory Database Streaming**: Uses `DatabaseLoader` (from `Microsoft.ML.Experimental`) to stream historical PostgreSQL records directly, allowing model training over millions of rows without RAM bloat.
* **Watch Engine Pooling**: Integrates `PredictionEnginePool` in the background worker host, utilizing its native file-watching mechanism to automatically reload `fraud_model.zip` when retrained, without restarting the worker.
* **High-Speed Binary Seeding**: Seeds **1,000,000 rows in under 15 seconds** using PostgreSQL's binary copy protocol (`BeginBinaryImport` in Npgsql).
* **Database Optimization**: Implements compound indexing on `(F2_PAN, F7_TxnDateTime)` for rules-checking queries and covering B-Tree indexes for complex dashboard metric aggregations.
* **Resumable Checkpointing**: The worker uses a high-watermark strategy (`FraudCheckState` table) to resume processing from the last checked transaction date, avoiding redundant re-scans on restart.
* **Premium Blazor Design**: A dark-themed admin layout built using Inter typography, subtle gradient animations, and CSS glassmorphism cards.
* **ML Score Checker**: An interactive dashboard page allowing analysts to manually score any transaction against the ML model with a visual probability gauge.
* **Swagger API Docs**: OpenAPI documentation with Swagger UI available at `/docs` for exploring and testing all API endpoints.
* **Docker Compose**: Full containerized deployment with PostgreSQL, Worker, WebAPI, and Dashboard services.

---

## 🚀 Setup & Execution Guide

### Option A: Docker Compose (Recommended)

The fastest way to run the entire system. All services (PostgreSQL, Worker, WebAPI, Dashboard) are orchestrated via Docker Compose.

#### Prerequisites
* [Docker Desktop](https://www.docker.com/products/docker-desktop/)

#### 1. First-Time Setup: Seed & Train

Before running Docker Compose for the first time, you need to seed the database and train the ML model locally (the model file is mounted into the containers):

```bash
# Start only PostgreSQL
docker compose up postgres -d

# Seed the database (run from host)
dotnet run --project FraudDetection.Worker -- --seed --count 100000

# Generate fraud labels (pure rules mode)
dotnet run --project FraudDetection.Worker -- --no-ml
# Wait for "Sleeping for 24 hours...", then Ctrl+C

# Train the ML model
dotnet run --project FraudDetection.ML.Trainer
# This outputs fraud_model.zip to the solution root
```

#### 2. Run All Services

```bash
docker compose up --build -d
```

| Service       | URL                           | Description               |
|---------------|-------------------------------|---------------------------|
| Dashboard     | http://localhost:5002          | Blazor Admin UI           |
| WebAPI        | http://localhost:5001          | REST API                  |
| Swagger UI    | http://localhost:5001/docs     | Interactive API Docs      |
| PostgreSQL    | localhost:5432                 | Database                  |
| Worker        | (background)                  | Fraud processing service  |

#### 3. Stop All Services

```bash
docker compose down
```

To also remove the database volume (full reset):
```bash
docker compose down -v
```

---

### Option B: Local Development (without Docker)

#### Prerequisites
* [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
* [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for PostgreSQL only)

#### 1. Start PostgreSQL Container
Spin up a local PostgreSQL container using Docker:
```bash
docker run --name postgres_db -e POSTGRES_PASSWORD=YourSecurePassword123! -p 5432:5432 -d postgres:latest
```

---

#### 2. Step-by-Step Pipeline Walkthrough

##### Step A: Seed the Database
Seed the database with mock transactions. By default, 10% of these transactions contain deliberate fraud patterns:
```bash
dotnet run --project FraudDetection.Worker -- --seed --count 100000
```
*(Creates the `fraud_detection` database schema and inserts 3,000,000 total rows representing 30 historical days).*

##### Step B: Populate Initial Fraud Labels (Pure Rules Engine Mode)
To train the ML model, we need labeled data. Run the worker in pure rules mode using the `--no-ml` flag to analyze the historical records and populate the `fraudalerts` table:
```bash
dotnet run --project FraudDetection.Worker -- --no-ml
```
*Wait for the console log to catch up to the current day and output "Sleeping for 24 hours...", then press `Ctrl+C` to terminate the worker.*

##### Step C: Train the ML.NET Model
Run the training project. It queries the database, JOINs the transactions with the alerts, trains the LightGBM model, prints classification metrics, and outputs the model to the solution root:
```bash
dotnet run --project FraudDetection.ML.Trainer
```
*You will see the model's metrics output (Accuracy, AUC, F1-Score, and a Confusion Matrix).*

##### Step D: Run the Hybrid Worker
Start the background worker without any flags. It detects the `fraud_model.zip` file and automatically enables the hybrid ML filter:
```bash
dotnet run --project FraudDetection.Worker
```
*Notice the logs outputting the hybrid stats breakdown: "ML Hybrid Stats — Clean (skipped): X, Immediate Fraud: Y, Grey Area (rules executed): Z".*

##### Step E: Launch the Monitoring Dashboard
Open two separate terminals and execute:

**Terminal 1 (Web API):**
```bash
dotnet run --project FraudDetection.WebAPI
```

**Terminal 2 (Blazor Dashboard):**
```bash
dotnet run --project FraudDetection.Dashboard
```

Once running, open your browser and navigate to **`http://localhost:5003`** to access the dashboard. 
* Monitor system KPI metrics, view daily alert volumes, list pending fraud alerts, search by card number, and toggle their review status.

Navigate to **`http://localhost:5001/docs`** to access the interactive Swagger UI for exploring all API endpoints.

---

### 🧪 Running Unit Tests
To run the automated unit tests verifying the rules engine logic:
```bash
dotnet test
```