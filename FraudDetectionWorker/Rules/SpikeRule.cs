using System;
using System.Threading.Tasks;
using System.Threading;
using FraudDetectionWorker.Models;
using FraudDetectionWorker.Rules;

namespace FraudDetectionWorker.Rules;
public class SpikeRule : IFraudRule
{
    public string Name => "Card Testing Rule";
    public string Description => "This rule checks for patterns indicative of card testing, such as multiple small transactions in a short period.";

    public async Task<RuleResult> IsRuleSatisfiedAsync(List<AuthorizationTransaction> transactions, CancellationToken cancellationToken)
    {
        await Task.Yield(); // Simulate asynchronous operation

        if (transactions == null || !transactions.Any())
        {
            return new RuleResult(string.Empty, string.Empty);
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


        var latestTransaction = transactions.LastOrDefault(); // Veyahut tarihe göre sıralayıp en sonuncusunu alabilirsiniz
        
        if (latestTransaction != null && latestTransaction.F4_AmountTxn > threshold)
        {
            return new RuleResult(Name, Description);
        }
        

        return new RuleResult(string.Empty, string.Empty); // Return an empty RuleResult if the rule is satisfied
    }
}