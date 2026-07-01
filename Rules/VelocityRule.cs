using System;
using System.Threading.Tasks;
using System.Threading;
using FraudDetectionWorker.Models;

namespace FraudDetectionWorker.Rules;

public class VelocityRule : IFraudRule
{
    public string Name => "Velocity Rule";
    public string Description => "This rule checks if the number of transactions from a single account exceeds a certain threshold within a specified time frame.";

    public async Task<RuleResult> IsRuleSatisfiedAsync(AuthorizationTransaction transaction, CancellationToken cancellationToken)
    {
        // Simulate some asynchronous operation, e.g., querying a database
        await Task.Delay(1000, cancellationToken);
        // For demonstration purposes, let's assume the rule is satisfied
        return new RuleResult(Name, Description);
    }
}