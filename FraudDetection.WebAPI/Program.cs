using FraudDetection.Worker.Database;
using FraudDetection.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.ML;


var builder = WebApplication.CreateBuilder(args);

// Register EF Core with PostgreSQL (read-only monitoring context)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

string[] candidatePaths = [
    Path.Combine(AppContext.BaseDirectory, "fraud_model.zip"),
    "/app/fraud_model.zip",
    Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "fraud_model.zip")),
    Path.GetFullPath("fraud_model.zip")
];
string modelPath = candidatePaths.FirstOrDefault(File.Exists) ?? candidatePaths.Last();
builder.Services.AddPredictionEnginePool<TransactionFeatures, TransactionPrediction>() //activate ML.NET PredictionEnginePool for fraud detection endpoints.
    .FromFile(filePath: modelPath, watchForChanges: true);


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "FraudDetection API", Version = "v1", Description = "REST API for the Hybrid Fraud Detection System" });
});
builder.Services.AddHealthChecks();

// CORS policy for Blazor Dashboard
builder.Services.AddCors(options =>
{
    options.AddPolicy("DashboardPolicy", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "FraudDetection API v1");
    c.RoutePrefix = "docs";
});

app.UseCors("DashboardPolicy");
app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
