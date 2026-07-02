using System;
using System.Threading.Tasks;
using System.Threading;
using FraudDetectionWorker.Models;
using FraudDetectionWorker.Rules;
using Xunit;

namespace Tests;

public class UnitTest1
{
    [Fact]
    public async Task TestVelocityRule_ShouldTrigger_WhenTransactionCountExceedsThreshold()
    {
        // Arrange 
        var rule = new VelocityRule();
        var transactions = FraudTestData.GetCnpVelocityPattern();

        // Act
        var result = await rule.IsRuleSatisfiedAsync(transactions, CancellationToken.None);

        // Assert
        //Should return a RuleResult with the rule name and description, indicating that the rule is not satisfied
        Assert.False(result._ruleName == string.Empty);

    }
}
