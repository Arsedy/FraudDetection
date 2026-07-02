using System;
using System.Threading.Tasks;
using System.Threading;
using FraudDetectionWorker.Models;

namespace FraudDetectionWorker.Rules;

public class VelocityRule : IFraudRule
{
    public string Name => "Velocity Rule";
    public string Description => "This rule checks if the number of transactions from a single account exceeds a certain threshold within a specified time frame.";

    public async Task<RuleResult> IsRuleSatisfiedAsync(List<AuthorizationTransaction> transactions, CancellationToken cancellationToken)
    {
        foreach (var transaction in transactions)
        {
            // Check if the cancellation has been requested
            cancellationToken.ThrowIfCancellationRequested();

            // Implement the logic to check if the transaction satisfies the velocity rule
            // For example, you might check if the number of transactions exceeds a threshold
            // within a certain time frame. This is just a placeholder for demonstration.
            if (transactions.Count > 5) // Example threshold
            {
                return new RuleResult(Name, Description);
            }
        }

        return new RuleResult(string.Empty, string.Empty); // Return an empty RuleResult if the rule is satisfied
    }
}