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

        int currentDayOffset = -30;

        while (!stoppingToken.IsCancellationRequested)
        {
            var targetDate = DateTime.UtcNow.Date.AddDays(currentDayOffset);
            _logger.LogInformation("Running fraud detection rules check for {TargetDate:yyyy-MM-dd} (Day {Offset})...", targetDate, currentDayOffset);

            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<IFraudDetectionEngine>();
                await engine.ProcessTransactionAsync(targetDate, stoppingToken);
            }

            if (currentDayOffset < -1)
            {
                _logger.LogInformation("Historical cycle complete for Day {Offset}. Sleeping for 5 seconds...", currentDayOffset);
                currentDayOffset++;
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            else
            {
                _logger.LogInformation("Caught up to current day (Day -1). Sleeping for 24 hours...");
                // Keep it at -1 so the next time it wakes up (tomorrow), it processes the new yesterday.
                currentDayOffset = -1; 
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
    }
}
