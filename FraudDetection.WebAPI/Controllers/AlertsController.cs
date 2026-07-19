using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FraudDetection.Worker.Database;

namespace FraudDetection.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AlertsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AlertsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// GET /api/alerts — Paginated fraud alerts with optional filters.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAlerts(
        [FromQuery] string? ruleId = null,
        [FromQuery] bool? isReviewed = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string? pan = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = _db.FraudAlerts
            .AsNoTracking()
            .Include(a => a.Transaction)
            .Include(a => a.Rule)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(ruleId))
            query = query.Where(a => a.RuleId == ruleId);

        if (isReviewed.HasValue)
            query = query.Where(a => a.IsReviewed == isReviewed.Value);

        if (from.HasValue)
            query = query.Where(a => a.FlaggedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(a => a.FlaggedAt <= to.Value);

        if (!string.IsNullOrWhiteSpace(pan))
            query = query.Where(a => a.F2_PAN.Contains(pan));

        int totalCount = await query.CountAsync();

        var alerts = await query
            .OrderByDescending(a => a.FlaggedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.AlertId,
                a.TransactionId,
                PAN = a.F2_PAN,
                a.RuleId,
                RuleName = a.Rule != null ? a.Rule.RuleName : a.RuleId,
                a.Score,
                a.IsReviewed,
                a.FlaggedAt,
                Amount = a.Transaction != null ? a.Transaction.F4_AmountTxn : (decimal?)null,
                MerchantLocation = a.Transaction != null ? a.Transaction.F43_MerchantLoc : null,
                Country = a.Transaction != null ? a.Transaction.F19_AcqCountry : null
            })
            .ToListAsync();

        return Ok(new
        {
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
            Data = alerts
        });
    }

    /// <summary>
    /// GET /api/alerts/{id} — Single alert detail with full transaction data.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetAlert(Guid id)
    {
        var alert = await _db.FraudAlerts
            .AsNoTracking()
            .Include(a => a.Transaction)
            .Include(a => a.Rule)
            .Where(a => a.AlertId == id)
            .Select(a => new
            {
                a.AlertId,
                a.TransactionId,
                PAN = a.F2_PAN,
                a.RuleId,
                RuleName = a.Rule != null ? a.Rule.RuleName : a.RuleId,
                RuleDescription = a.Rule != null ? a.Rule.RuleDescription : null,
                a.Score,
                a.IsReviewed,
                a.FlaggedAt,
                Transaction = a.Transaction != null ? new
                {
                    a.Transaction.TransactionId,
                    a.Transaction.Mti,
                    PAN = a.Transaction.F2_PAN,
                    Amount = a.Transaction.F4_AmountTxn,
                    TxnDateTime = a.Transaction.F7_TxnDateTime,
                    MCC = a.Transaction.F18_MCC,
                    Country = a.Transaction.F19_AcqCountry,
                    POSEntryMode = a.Transaction.F22_POSEntryMode,
                    ResponseCode = a.Transaction.F39_ResponseCode,
                    MerchantLocation = a.Transaction.F43_MerchantLoc,
                    Currency = a.Transaction.F49_CurrencyCode
                } : null
            })
            .FirstOrDefaultAsync();

        if (alert == null)
            return NotFound();

        return Ok(alert);
    }

    /// <summary>
    /// PUT /api/alerts/{id}/review — Toggle the reviewed status of an alert.
    /// </summary>
    [HttpPut("{id:guid}/review")]
    public async Task<IActionResult> ToggleReview(Guid id)
    {
        var alert = await _db.FraudAlerts.FindAsync(id);
        if (alert == null)
            return NotFound();

        alert.IsReviewed = !alert.IsReviewed;
        await _db.SaveChangesAsync();

        return Ok(new { alert.AlertId, alert.IsReviewed });
    }
}
