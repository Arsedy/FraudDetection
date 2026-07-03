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

        await Task.Yield(); // Simulate asynchronous operation
        // Check if the cancellation has been requested
        cancellationToken.ThrowIfCancellationRequested();
        if (transactions == null || transactions.Count <= 5)
        {
            return new RuleResult(string.Empty, string.Empty , null);
        }

        // 1. Sort the transactions chronologically to make the window movement accurate
        var sortedTxns = transactions.OrderBy(t => t.F7_TxnDateTime).ToList();

        // 2. Set up the sliding window configurations
        var window = new Queue<DateTime>();
        const double windowMinutes = 10.0; 
        const int threshold = 5;           // Trigger when count is strictly > 5

        foreach (var txn in sortedTxns)
        {
            var currentTxnTime = txn.F7_TxnDateTime;

            // 3. "Pop" (Dequeue) timestamps that have fallen out of the rolling time window
            while (window.Count > 0 && (currentTxnTime - window.Peek()).TotalMinutes > windowMinutes)
            {
                window.Dequeue();
            }

            // 4. Push (Enqueue) the current transaction's timestamp into the window
            window.Enqueue(currentTxnTime);

            // 5. Evaluate if the window size has exceeded the threshold
            if (window.Count > threshold)
            {
                return new RuleResult(Name, Description , txn.TransactionId); // Return the transaction ID of the transaction that triggered the rule
            }
        }
        return new RuleResult(string.Empty, string.Empty , null); // Return an empty RuleResult if the rule is satisfied
    }
}   