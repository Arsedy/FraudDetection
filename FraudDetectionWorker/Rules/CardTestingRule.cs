using System;
using System.Threading.Tasks;
using System.Threading;
using FraudDetectionWorker.Models;
using FraudDetectionWorker.Rules;

namespace FraudDetectionWorker.Rules;
public class CardTestingRule : IFraudRule
{
    public string Name => "Card Testing Rule";
    public string Description => "This rule checks for patterns indicative of card testing, such as multiple small transactions in a short period.";

    public async Task<RuleResult> IsRuleSatisfiedAsync(List<AuthorizationTransaction> transactions, CancellationToken cancellationToken)
    {
        await Task.Yield(); // Simulate asynchronous operation

        // Check if the cancellation has been requested
        cancellationToken.ThrowIfCancellationRequested();
        
        var decline = 0.0m;
        var accepted = 0.0m;

        var decline_count = 0;
        var accepted_count = 0;

        foreach (var transaction in transactions)
        {
            // Placeholder logic: Check if the transaction amount is below a certain threshold
            if(transaction.F39_ResponseCode == "05") // "05" indicates a declined transaction "Do Not Honor"
            {
                decline += transaction.F4_AmountTxn;
                decline_count++;
            }
            else
            {
                accepted += transaction.F4_AmountTxn;
                accepted_count++;
            }
        }
        var decline_avg = decline_count > 0 ? decline / decline_count : 0;
        var accepted_avg = accepted_count > 0 ? accepted / accepted_count : 0;
        if (accepted_avg > decline_avg * 5m && decline_count> accepted_count) // if the average accepted transaction amount is more than 5 times the average declined transaction amount, we consider it a card testing pattern
        {
            return new RuleResult(Name, Description);
        }

        return new RuleResult(string.Empty, string.Empty); // Return an empty RuleResult if the rule is satisfied

    }
    
}
     