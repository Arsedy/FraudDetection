using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using FraudDetectionWorker.Services;

namespace FraudDetectionWorker;

public class FraudWorker : BackgroundService
{
    private readonly ILogger<FraudWorker> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    public FraudWorker(ILogger<FraudWorker> logger, IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FraudWorker background service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Running fraud detection rules check...");

            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<IFraudDetectionEngine>();
                var targetDate = DateTime.UtcNow.Date.AddDays(-1); // Process yesterday's transactions
                await engine.ProcessTransactionAsync(targetDate, stoppingToken);
            }

            _logger.LogInformation("Fraud detection cycle complete. Sleeping for 24 hours...");
            // Wait 24 hours between runs as specified
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
