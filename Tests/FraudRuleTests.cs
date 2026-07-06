using System;
using System.Threading.Tasks;
using System.Threading;
using FraudDetectionWorker.Models;
using FraudDetectionWorker.Rules;
using Xunit;

namespace Tests;

public class FraudRuleTests
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
        Assert.False(result.IsSatisfied);
    }
    [Fact]
    public async Task TestVelocityRule_ShouldNotTrigger_WhenTransactionCountIsBelowThreshold()
    {
        // Arrange
        var rule = new VelocityRule();
        var transactions = FraudTestData.GetNormalTransactions();

        // Act
        var result = await rule.IsRuleSatisfiedAsync(transactions, CancellationToken.None);

        // Assert
        Assert.True(result.IsSatisfied);
    }




    [Fact]
    public async Task TestTravelRule_ShouldTrigger_WhenTransactionLocationsDiffer()
    {
        // Arrange
        var rule = new TravelRule();
        var transactions = FraudTestData.GetImpossibleTravelPattern();

        // Act
        var result = await rule.IsRuleSatisfiedAsync(transactions, CancellationToken.None);

        // Assert
        Assert.False(result.IsSatisfied);
    }
    [Fact]
    public async Task TestTravelRule_ShouldNotTrigger_WhenTransactionLocationsAreSame()
    {
        // Arrange
        var rule = new TravelRule();
        var transactions = FraudTestData.GetNormalTransactions();

        // Act
        var result = await rule.IsRuleSatisfiedAsync(transactions, CancellationToken.None);

        // Assert
        Assert.True(result.IsSatisfied);
    }





    [Fact]
    public async Task CardTestingRule_ShouldTrigger_WhenAcceptedAvgExceedsDeclinedAvg()
    {
        // Arrange
        var rule = new CardTestingRule();
        var transactions = FraudTestData.GetCardTestingPattern();

        // Act
        var result = await rule.IsRuleSatisfiedAsync(transactions, CancellationToken.None);

        // Assert
        Assert.False(result.IsSatisfied);

    }
    [Fact]
    public async Task CardTestingRule_ShouldNotTrigger_WhenAcceptedAvgIsLessThanDeclinedAvg()
    {
        // Arrange
        var rule = new CardTestingRule();
        var transactions = FraudTestData.GetNormalTransactions();

        // Act
        var result = await rule.IsRuleSatisfiedAsync(transactions, CancellationToken.None);

        // Assert
        Assert.True(result.IsSatisfied);
    }





    [Fact]
    public async Task SpikeRule_ShouldTrigger_WhenLatestTransactionExceedsThreshold()
    {
        // Arrange
        var rule = new SpikeRule();
        var transactions = FraudTestData.GetSpikePattern();

        // Act
        var result = await rule.IsRuleSatisfiedAsync(transactions, CancellationToken.None);

        // Assert
        Assert.False(result.IsSatisfied);
    }

    [Fact]
    public async Task SpikeRule_ShouldNotTrigger_WhenLatestTransactionIsBelowThreshold()
    {
        // Arrange
        var rule = new SpikeRule();
        var transactions = FraudTestData.GetNormalTransactions();

        // Act
        var result = await rule.IsRuleSatisfiedAsync(transactions, CancellationToken.None);

        // Assert
        Assert.True(result.IsSatisfied);
    }

    [Fact]
    public async Task ExpiryDateRule_ShouldTrigger_WhenMultipleDistinctExpiryDatesAreUsed()
    {
        // Arrange
        var rule = new ExpiryDateRule();
        var transactions = FraudTestData.GetExpiryDateBruteForcePattern();

        // Act
        var result = await rule.IsRuleSatisfiedAsync(transactions, CancellationToken.None);

        // Assert
        Assert.False(result.IsSatisfied);
    }

    [Fact]
    public async Task ExpiryDateRule_ShouldNotTrigger_WhenNormalTransactionsAreProcessed()
    {
        // Arrange
        var rule = new ExpiryDateRule();
        var transactions = FraudTestData.GetNormalTransactions();

        // Act
        var result = await rule.IsRuleSatisfiedAsync(transactions, CancellationToken.None);

        // Assert
        Assert.True(result.IsSatisfied);
    }
}
