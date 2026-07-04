using System.Threading.Tasks;
using System.Threading;
using FraudDetectionWorker.Models;
using FraudDetectionWorker.Rules;
using FraudDetectionWorker.Repositories;


namespace FraudDetectionWorker.Services;

public class FraudDetectionEngine : IFraudDetectionEngine
{
    private readonly ILogger<FraudDetectionEngine> _logger;
    private readonly IEnumerable<IFraudRule> _rule;
    private readonly ITransactionRepository _transaction_repository;
    private readonly IFraudAlertRepository _fraud_alert_repository;
    public FraudDetectionEngine(ILogger<FraudDetectionEngine> logger , IEnumerable<IFraudRule> rules , ITransactionRepository repository, IFraudAlertRepository alertRepository)
    {
        _logger = logger;
        _rule = rules;
        _transaction_repository = repository;
        _fraud_alert_repository = alertRepository;
    }

    public async Task ProcessTransactionAsync(DateTime targetdate,  CancellationToken cancellationToken)
    {
        DateTime startDate = targetdate.Date;
        DateTime endDate = targetdate.Date.AddDays(1);
        List<string> uniquePans = await _transaction_repository.GetUniquePansWithMultipleTransactionsAsync(
                                startDate, endDate, cancellationToken); // returns list of unique PANs with multiple transactions in the specified date range
        
        _logger.LogInformation($"Found {uniquePans.Count} unique PANs with multiple transactions between {startDate} and {endDate}.");
        
        List<AuthorizationTransaction> transactions = await _transaction_repository.GetTransactionsByPansAsync(
                                uniquePans, startDate, endDate, cancellationToken);

        _logger.LogInformation($"Retrieved {transactions.Count} transactions for the unique PANs between {startDate} and {endDate}.");

        var group_by_pan = transactions.GroupBy(t => t.F2_PAN);
        int totalalerts = 0;

        foreach (var group in group_by_pan)
        {
            var transactions_list = group.ToList();
            foreach (var rule in _rule)
            {
                var result = await rule.IsRuleSatisfiedAsync(transactions_list, cancellationToken);
                if(result.IsSatisfied == false)
                {
                    // Create a FraudAlert object and save it to the database
                    var alert = new FraudAlert
                    {
                        TransactionId = result.TransactionId,
                        F2_PAN = group.Key,
                        RuleName = result.RuleName,
                        Description = result.Description
                    };
                    await _fraud_alert_repository.AddAlertAsync(alert, cancellationToken);
                    totalalerts++;
                    break; // Exit the loop after the first rule is not satisfied for this group of transactions
                }
            }
        }
        await _fraud_alert_repository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation($"Total fraud alerts generated: {totalalerts}");
    }
}
