using System.Threading.Tasks;
using System.Threading;
using FraudDetectionWorker.Models;
using FraudDetectionWorker.Rules;
using FraudDetectionWorker.Repositories;


namespace FraudDetectionWorker.Services;

public class FraudDetectionEngine : IFraudDetectionEngine
{
    private readonly ILogger<FraudDetectionEngine> _logger;
    private readonly IFraudRule _rule;
    private readonly ITransactionRepository _repository;

    public FraudDetectionEngine(ILogger<FraudDetectionEngine> logger , IFraudRule rule , ITransactionRepository repository)
    {
        _logger = logger;
        _rule = rule;
        _repository = repository;
    }

    public async Task ProcessTransactionAsync(DateTime targetdate,  CancellationToken cancellationToken)
    {
        await Task.Yield(); // Simulate asynchronous operation
    }
}
