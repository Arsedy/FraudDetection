using System.Threading.Tasks;
using System.Threading;

public interface IFraudRule
{
    String Name { get; } // each rule will have a unique name to identify it in the system
    String Description { get; } // each rule will have a description to explain what it does and how it works

    Task<bool> IsRuleSatisfiedAsync(CancellationToken cancellationToken);
    //if the rule is satisfied, it will return true, otherwise it will return false
    //if the rule is not satisfied, it will return false, and the worker 
    //will flagg the transaction as suspicious and send it to the fraud detection team for further investigation



}