using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FraudDetectionWorker.Database;

namespace FraudDetection.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MetricsController : ControllerBase
{
    private readonly AppDbContext _db;

    public MetricsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// GET /api/metrics — Aggregated dashboard metrics for the fraud detection system.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMetrics()
    {
        // Total transaction count
        long totalTransactions = await _db.AuthorizationTransactions
            .AsNoTracking()
            .LongCountAsync();

        // Total fraud alerts
        long totalAlerts = await _db.FraudAlerts
            .AsNoTracking()
            .LongCountAsync();

        // Pending (unreviewed) alerts
        long pendingAlerts = await _db.FraudAlerts
            .AsNoTracking()
            .Where(a => !a.IsReviewed)
            .LongCountAsync();

        // Reviewed alerts
        long reviewedAlerts = totalAlerts - pendingAlerts;

        // Distinct cards flagged as fraud
        long compromisedCards = await _db.FraudAlerts
            .AsNoTracking()
            .Select(a => a.F2_PAN)
            .Distinct()
            .LongCountAsync();

        // Fraud rate (alerts / total transactions)
        double fraudRate = totalTransactions > 0
            ? (double)totalAlerts / totalTransactions * 100
            : 0;

        // Alerts grouped by rule
        var alertsByRule = await _db.FraudAlerts
            .AsNoTracking()
            .Include(a => a.Rule)
            .GroupBy(a => new { a.RuleId, RuleName = a.Rule != null ? a.Rule.RuleName : a.RuleId })
            .Select(g => new
            {
                RuleId = g.Key.RuleId,
                RuleName = g.Key.RuleName,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToListAsync();

        // Alerts over the last 30 days (daily breakdown)
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var dailyAlerts = await _db.FraudAlerts
            .AsNoTracking()
            .Where(a => a.FlaggedAt >= thirtyDaysAgo)
            .GroupBy(a => a.FlaggedAt.Date)
            .Select(g => new
            {
                Date = g.Key,
                Count = g.Count()
            })
            .OrderBy(x => x.Date)
            .ToListAsync();

        return Ok(new
        {
            TotalTransactions = totalTransactions,
            TotalAlerts = totalAlerts,
            PendingAlerts = pendingAlerts,
            ReviewedAlerts = reviewedAlerts,
            CompromisedCards = compromisedCards,
            FraudRate = Math.Round(fraudRate, 4),
            AlertsByRule = alertsByRule,
            DailyAlerts = dailyAlerts
        });
    }
}
