using System;
using System.Threading.Tasks;
using System.Threading;
using FraudDetection.Worker.Models;
using FraudDetection.Worker.Rules;

namespace FraudDetection.Worker.Rules;
public class SpikeRule : IFraudRule
{
    public string Name => "Spike Rule";
    public string Description => "This rule checks for patterns indicative of spike transactions, such as a sudden increase in transaction amount.";

    public async Task<RuleResult> IsRuleSatisfiedAsync(List<AuthorizationTransaction> transactions, CancellationToken cancellationToken)
    {
        await Task.Yield(); // Simulate asynchronous operation

        if (transactions == null || !transactions.Any())
        {
            return new RuleResult(string.Empty, string.Empty , null);
        }

        var sortedTransactions = transactions.OrderBy(t => t.F4_AmountTxn).ToList();
        int count = sortedTransactions.Count;
        decimal medianAmount;

        // 2. Medyan (Median) Hesaplama (Hatasız decimal bölmesi için 2.0M kullanıldı)
        if (count % 2 == 0)
        {
            medianAmount = (sortedTransactions[(count / 2) - 1].F4_AmountTxn + sortedTransactions[count / 2].F4_AmountTxn) / 2.0M;
        }
        else
        {
            medianAmount = sortedTransactions[count / 2].F4_AmountTxn;
        }

        // 3. Eşik Değeri Belirleme (Örn: Medyanın 5 katı)
        decimal multiplier = 5.0M; 
        decimal threshold = medianAmount * multiplier;


        // 4. Get the chronologically latest transaction (sort by time, not by amount)
        var latestTransaction = transactions.OrderByDescending(t => t.F7_TxnDateTime).First();

        if (latestTransaction.F4_AmountTxn > threshold)
        {
            return new RuleResult(Name, Description , latestTransaction.TransactionId);
        }
        

        return new RuleResult(string.Empty, string.Empty , null); // Return an empty RuleResult if the rule is satisfied
    }
}