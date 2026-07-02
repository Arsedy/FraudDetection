using System.Threading.Tasks;
using System.Threading;
using FraudDetectionWorker.Models;
using FraudDetectionWorker.Rules;

namespace FraudDetectionWorker.Services;

public class FraudDetectionEngine : IFraudDetectionEngine
{
    private readonly ILogger<FraudDetectionEngine> _logger;

    public FraudDetectionEngine(ILogger<FraudDetectionEngine> logger)
    {
        _logger = logger;
    }

    public async Task ProcessTransactionAsync(IFraudRule rule, AuthorizationTransaction transaction, CancellationToken cancellationToken)
    {
        await Task.Yield(); // Simulate asynchronous operation
    }
}
