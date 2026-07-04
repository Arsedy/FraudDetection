using System;
using System.Threading.Tasks;
using System.Threading;
using FraudDetectionWorker.Models;

namespace FraudDetectionWorker.Rules;

public class TravelRule : IFraudRule
{
    public string Name => "Travel Rule";
    public string Description => "This rule checks if a card was used in two different countries within a time window too short for physical travel.";

    public async Task<RuleResult> IsRuleSatisfiedAsync(List<AuthorizationTransaction> transactions, CancellationToken cancellationToken)
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();

        if (transactions == null || transactions.Count < 2)
        {
            return new RuleResult(string.Empty, string.Empty, null);
        }

        // 1. Sort chronologically so consecutive comparisons are meaningful
        var sortedTxns = transactions.OrderBy(t => t.F7_TxnDateTime).ToList();

        // 2. Walk through consecutive pairs — if country changed within 1 hour, flag it
        for (int i = 1; i < sortedTxns.Count; i++)
        {
            if (sortedTxns[i].F19_AcqCountry != sortedTxns[i - 1].F19_AcqCountry &&
                sortedTxns[i].F7_TxnDateTime - sortedTxns[i - 1].F7_TxnDateTime < TimeSpan.FromHours(1))
            {
                return new RuleResult(Name, Description, sortedTxns[i].TransactionId);
            }
        }

        return new RuleResult(string.Empty, string.Empty, null);
    }
}