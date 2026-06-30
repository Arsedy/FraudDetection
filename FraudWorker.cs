using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FraudDetectionWorker;

public class FraudWorker : BackgroundService
{
    private readonly ILogger<FraudWorker> _logger;
    private readonly IEnumerable<IFraudRule> _rules;

    public FraudWorker(ILogger<FraudWorker> logger, IEnumerable<IFraudRule> rules)
    {
        _logger = logger;
        _rules = rules;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FraudWorker background service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Running fraud detection rules check...");

            foreach (var rule in _rules)
            {
                try
                {
                    bool isSatisfied = await rule.IsRuleSatisfiedAsync(stoppingToken);
                    if (!isSatisfied)
                    {
                        _logger.LogWarning("Fraud rule '{RuleName}' was NOT satisfied!", rule.Name);
                    }
                }
                catch (System.Exception ex)
                {
                    _logger.LogError(ex, "Error executing rule '{RuleName}'", rule.Name);
                }
            }

            // Wait 24 hours between runs as specified in README
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}

