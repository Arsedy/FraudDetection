using System.Threading.Tasks;
using System.Threading;
using FraudDetection.Worker.Models;

namespace FraudDetection.Worker.Rules;

public interface IFraudRule
{
    string Id => GetType().Name;
    String Name { get; } // each rule will have a unique name to identify it in the system
    String Description { get; } // each rule will have a description to explain what it does and how it works

    Task<RuleResult> IsRuleSatisfiedAsync(List<AuthorizationTransaction> transactions, CancellationToken cancellationToken);
    //if the rule is satisfied, it will return empty RuleResult, 
    // otherwise it will return RuleResult with the rule name and description, and the worker 
    //will flagg the transaction as suspicious and send it to the fraud detection team for further investigation

}

// return type class 
public class RuleResult
{
    public string RuleName { get; set; }
    public string Description { get; set; }
    public Guid? TransactionId { get; set; } // The ID of the transaction flagged as fraud
    public bool IsSatisfied => string.IsNullOrEmpty(RuleName); //if the rule is satisfied, it will return true, otherwise it will return false
    public RuleResult(string ruleName, string description, Guid? transactionId) //consturctor to initialize the properties
    {
        RuleName = ruleName;
        Description = description;
        TransactionId = transactionId;
    }
}