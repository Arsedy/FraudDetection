using System;
using System.Threading.Tasks;
using System.Threading;
using FraudDetection.Worker.Models;

namespace FraudDetection.Worker.Rules;
public class CardTestingRule : IFraudRule
{
    public string Name => "Card Testing Rule";
    public string Description => "This rule checks for patterns indicative of card testing, such as multiple small declines followed by a large approved purchase.";

    public async Task<RuleResult> IsRuleSatisfiedAsync(List<AuthorizationTransaction> transactions, CancellationToken cancellationToken)
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();

        // 1. Sort chronologically
        var sortedTxns = transactions.OrderBy(t => t.F7_TxnDateTime).ToList();

        // 2. Separate declines and approvals
        var declines = sortedTxns.Where(t => t.F39_ResponseCode != "00").ToList();

        // Need at least 3 declines to consider it card testing
        if (declines.Count < 3)
        {
            return new RuleResult(string.Empty, string.Empty, null);
        }

        // 3. Calculate the average declined amount
        var declineAvg = declines.Average(t => t.F4_AmountTxn);

        // 4. Find the first approved transaction AFTER the last decline 
        //    whose amount is suspiciously larger (5x the average decline amount)
        var lastDeclineTime = declines.Last().F7_TxnDateTime;
        var suspiciousApproval = sortedTxns
            .Where(t => t.F39_ResponseCode == "00" 
                     && t.F7_TxnDateTime >= lastDeclineTime 
                     && t.F4_AmountTxn > declineAvg * 5m)
            .FirstOrDefault();

        if (suspiciousApproval != null)
        {
            return new RuleResult(Name, Description, suspiciousApproval.TransactionId);
        }

        return new RuleResult(string.Empty, string.Empty, null);
    }
}