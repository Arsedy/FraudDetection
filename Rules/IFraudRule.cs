using System.Threading.Tasks;
using System.Threading;
using FraudDetectionWorker.Models;

namespace FraudDetectionWorker.Rules;

public interface IFraudRule
{
    String Name { get; } // each rule will have a unique name to identify it in the system
    String Description { get; } // each rule will have a description to explain what it does and how it works

    Task<RuleResult> IsRuleSatisfiedAsync(AuthorizationTransaction transaction, CancellationToken cancellationToken);
    //if the rule is satisfied, it will return empty RuleResult, 
    // otherwise it will return RuleResult with the rule name and description, and the worker 
    //will flagg the transaction as suspicious and send it to the fraud detection team for further investigation

}

// return type class 
public class RuleResult
{
    public string _ruleName { get;} //immutable property to hold the rule name
    public string _description { get;} //immutable property to hold the rule description 
    public bool IsSatisfied => string.IsNullOrEmpty(_ruleName); //if the rule is satisfied, it will return true, otherwise it will return false
    public RuleResult(string ruleName, string description) //consturctor to initialize the properties
    {
        _ruleName = ruleName;
        _description = description;
    }
}