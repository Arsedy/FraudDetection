using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FraudDetection.Worker.Models;
using FraudDetection.Worker.Rules;

namespace FraudDetection.Worker.Database;

public static class RuleSyncHelper
{
    public static async Task SyncRulesAsync(AppDbContext dbContext, IEnumerable<IFraudRule> rules, CancellationToken cancellationToken = default)
    {
        var dbRules = await dbContext.Rules.ToListAsync(cancellationToken);
        var dbRulesDict = dbRules.ToDictionary(r => r.RuleId);

        foreach (var rule in rules)
        {
            if (dbRulesDict.TryGetValue(rule.Id, out var existingRule))
            {
                if (existingRule.RuleName != rule.Name || existingRule.RuleDescription != rule.Description)
                {
                    existingRule.RuleName = rule.Name;
                    existingRule.RuleDescription = rule.Description;
                    dbContext.Entry(existingRule).State = EntityState.Modified;
                }
            }
            else
            {
                var newRule = new Rule
                {
                    RuleId = rule.Id,
                    RuleName = rule.Name,
                    RuleDescription = rule.Description
                };
                await dbContext.Rules.AddAsync(newRule, cancellationToken);
            }
        }

        // Ensure the ML synthetic rule exists for high-confidence ML fraud alerts
        const string mlRuleId = "ML_HighConfidenceFraud";
        if (!dbRulesDict.ContainsKey(mlRuleId))
        {
            var mlRule = new Rule
            {
                RuleId = mlRuleId,
                RuleName = "ML High Confidence Fraud",
                RuleDescription = "Transaction flagged by the ML model with a fraud probability score >= 0.85, bypassing the rules engine."
            };
            await dbContext.Rules.AddAsync(mlRule, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
