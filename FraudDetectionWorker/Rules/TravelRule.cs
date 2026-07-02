using System;
using System.Threading.Tasks;
using System.Threading;
using FraudDetectionWorker.Models;

namespace FraudDetectionWorker.Rules;

public class TravelRule : IFraudRule
{
    public string Name => "Travel Rule";
    public string Description => "This rule checks if a transaction occurs in a location that is significantly different from the cardholder's usual location.";

    public async Task<RuleResult> IsRuleSatisfiedAsync(List<AuthorizationTransaction> transactions, CancellationToken cancellationToken)
    {
        await Task.Yield(); // Simulate asynchronous operation

        // Check if the cancellation has been requested
        cancellationToken.ThrowIfCancellationRequested();

        // Implement the logic to check if the transaction satisfies the travel rule
        // For example, you might check if the transaction location is significantly different
        // from the cardholder's usual location. This is just a placeholder for demonstration.
        if (transactions.Count > 0)
        {
            var firstTransaction = transactions[0];
            var lastTransaction = transactions[^1];

            // Placeholder logic: Check if the locations are different
            if (firstTransaction.F19_AcqCountry != lastTransaction.F19_AcqCountry)
            {
                return new RuleResult(Name, Description);
            }
        }

        return new RuleResult(string.Empty, string.Empty); // Return an empty RuleResult if the rule is satisfied
    }
}