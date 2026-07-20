using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using FraudDetection.Worker.Models;

namespace FraudDetection.Worker.Rules;

public class ExpiryDateRule : IFraudRule
{
    public string Name => "Expiry Date Rule";
    public string Description => "This rule checks for patterns indicative of expiry date brute-forcing, such as multiple authorization attempts on the same card using different expiration dates within a short timeframe.";

    public async Task<RuleResult> IsRuleSatisfiedAsync(List<AuthorizationTransaction> transactions, CancellationToken cancellationToken)
    {
        await Task.Yield(); // Simulate asynchronous operation
        cancellationToken.ThrowIfCancellationRequested();

        if (transactions == null || transactions.Count < 3)
        {
            return new RuleResult(string.Empty, string.Empty, null);
        }

        // 1. Filter for Authorization Requests and sort chronologically
        var authRequests = transactions
            .Where(t => t.Mti == "0100") // ->  0100 represents an Authorization Request.
            .OrderBy(t => t.F7_TxnDateTime)
            .ToList();

        if (authRequests.Count < 3)
        {
            return new RuleResult(string.Empty, string.Empty, null);
        }

        // 2. Sliding window to detect distinct expiration dates
        const double windowMinutes = 5.0;
        const int threshold = 3; // Trigger if 3 or more distinct expiration dates are seen

        for (int i = 0; i < authRequests.Count; i++)
        {
            var currentTxn = authRequests[i];
            var windowStartTime = currentTxn.F7_TxnDateTime.AddMinutes(-windowMinutes);

            var distinctExpDates = new HashSet<string>();

            // Look backwards to find all transactions within the last 'windowMinutes'
            for (int j = i; j >= 0; j--)
            {
                var prevTxn = authRequests[j];

                // If we fall out of the time window, stop checking backwards
                if (prevTxn.F7_TxnDateTime < windowStartTime)
                {
                    break;
                }

                distinctExpDates.Add(prevTxn.F14_ExpDate);
            }

            // Evaluate if the number of unique expiration dates reached the threshold
            if (distinctExpDates.Count >= threshold)
            {
                return new RuleResult(Name, Description, currentTxn.TransactionId);
            }
        }

        // Return an empty RuleResult if the rule is satisfied (no fraud detected)
        return new RuleResult(string.Empty, string.Empty, null);
    }
}