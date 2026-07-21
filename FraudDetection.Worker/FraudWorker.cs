using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using FraudDetection.Worker.Services;
using FraudDetection.Worker.Database;
using FraudDetection.Worker.Models;

namespace FraudDetection.Worker;

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

        // 1. Initialize checkpoint state on startup
        int currentDayOffset = -30;
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var savedState = await db.FraudCheckStates.FirstOrDefaultAsync(x => x.Id == 1, stoppingToken);

            if (savedState != null)
            {
                currentDayOffset = savedState.LastProcessedDayOffset;
                _logger.LogInformation("Resuming fraud check from saved checkpoint: Day {Offset} (Last Txn Date: {TxnDate:yyyy-MM-dd HH:mm:ss})",
                    currentDayOffset, savedState.LastProcessedTxnDateTime);
            }
            else
            {
                _logger.LogInformation("No saved checkpoint found. Starting initial historical scan from Day -30.");
            }
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var targetDate = DateTime.UtcNow.Date.AddDays(currentDayOffset);
            _logger.LogInformation("Running fraud detection rules check for {TargetDate:yyyy-MM-dd} (Day {Offset})...", targetDate, currentDayOffset);

            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<IFraudDetectionEngine>();
                await engine.ProcessTransactionAsync(targetDate, stoppingToken);

                // 2. Persist high-watermark checkpoint after evaluating the day
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var state = await db.FraudCheckStates.FirstOrDefaultAsync(x => x.Id == 1, stoppingToken);

                if (state == null)
                {
                    db.FraudCheckStates.Add(new FraudCheckState
                    {
                        Id = 1,
                        LastProcessedDayOffset = currentDayOffset,
                        LastProcessedTxnDateTime = targetDate,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    state.LastProcessedDayOffset = currentDayOffset;
                    state.LastProcessedTxnDateTime = targetDate;
                    state.UpdatedAt = DateTime.UtcNow;
                }

                await db.SaveChangesAsync(stoppingToken);
            }

            if (currentDayOffset < -1)
            {
                _logger.LogInformation("Historical cycle complete for Day {Offset}. Checkpoint saved. Sleeping for 5 seconds...", currentDayOffset);
                currentDayOffset++;
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            else
            {
                _logger.LogInformation("Caught up to current day (Day -1). Checkpoint saved. Sleeping for 24 hours...");
                // Keep it at -1 so the next time it wakes up (tomorrow), it processes the new yesterday.
                currentDayOffset = -1;
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
    }
}
