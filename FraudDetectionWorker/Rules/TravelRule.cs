using System;
using System.Threading.Tasks;
using System.Threading;
using FraudDetectionWorker.Models;
using System.Transactions;

namespace FraudDetectionWorker.Rules;

public class TravelRule : IFraudRule
{
    public string Name => "Travel Rule";
    public string Description => "This rule checks if a transaction occurs in a location that is significantly different from the cardholder's usual location.";

    public async Task<RuleResult> IsRuleSatisfiedAsync(List<AuthorizationTransaction> transactions, CancellationToken cancellationToken)
    {
        await Task.Yield(); // Simulate asynchronous operation
        HashSet<string> locations = new HashSet<string>();
        // Check if the cancellation has been requested
        cancellationToken.ThrowIfCancellationRequested();

        // Implement the logic to check if the transaction satisfies the travel rule
        // For example, you might check if the transaction location is significantly different
        // from the cardholder's usual location. This is just a placeholder for demonstration.
        for(int i = 0; i < transactions.Count; i++)
        {
            var location = transactions[i].F19_AcqCountry;
            if(!locations.Contains(location))
            {
                if(locations.Count > 0 && transactions[i].F7_TxnDateTime-transactions[i-1].F7_TxnDateTime < TimeSpan.FromHours(2))
                {
                    return new RuleResult(Name, Description , transactions[i].TransactionId);
                }
                else
                {
                    locations.Add(location);
                }  
            }

        }

        return new RuleResult(string.Empty, string.Empty , null); // Return an empty RuleResult if the rule is satisfied
    }
}