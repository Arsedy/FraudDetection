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
        List<AuthorizationTransaction> transactions = await _transaction_repository.GetTransactionsByPansAsync(
                                uniquePans, startDate, endDate, cancellationToken);

        var group_by_pan = transactions.GroupBy(t => t.F2_PAN);
        foreach (var group in group_by_pan)
        {
            foreach (var rule in _rule)
            {
                var result = await rule.IsRuleSatisfiedAsync(group.ToList(), cancellationToken);
                if(result._ruleName != string.Empty)
                {
                    // Create a FraudAlert object and save it to the database
                    var alert = new FraudAlert
                    {
                        TransactionId = result.TransactionId,
                        F2_PAN = group.Key,
                        RuleName = result._ruleName,
                        Description = result._description
                    };
                    await _fraud_alert_repository.AddAlertAsync(alert, cancellationToken);
                    break; // Exit the loop after the first rule is not satisfied for this group of transactions
                }
            }
        }
        
    }
}
