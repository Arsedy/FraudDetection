using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FraudDetectionWorker.Models;
using FraudDetectionWorker.Rules;

namespace FraudDetectionWorker.Database;

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

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
