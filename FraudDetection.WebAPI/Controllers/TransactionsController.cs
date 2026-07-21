using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FraudDetection.Worker.Database;

namespace FraudDetection.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly AppDbContext _db;

    public TransactionsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// GET /api/transactions — Paginated transaction log with optional filters and alert annotations.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] string? pan = null,
        [FromQuery] string? responseCode = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = _db.AuthorizationTransactions
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(pan))
            query = query.Where(t => t.F2_PAN.Contains(pan));

        if (!string.IsNullOrWhiteSpace(responseCode))
            query = query.Where(t => t.F39_ResponseCode == responseCode);

        if (from.HasValue)
            query = query.Where(t => t.F7_TxnDateTime >= from.Value);

        if (to.HasValue)
            query = query.Where(t => t.F7_TxnDateTime <= to.Value);

        int totalCount = await query.CountAsync();

        var rawTransactions = await query
            .OrderByDescending(t => t.F7_TxnDateTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new
            {
                t.TransactionId,
                PAN = t.F2_PAN,
                Amount = t.F4_AmountTxn,
                TxnDateTime = t.F7_TxnDateTime,
                MCC = t.F18_MCC,
                Country = t.F19_AcqCountry,
                POSEntryMode = t.F22_POSEntryMode,
                ResponseCode = t.F39_ResponseCode,
                MerchantLocation = t.F43_MerchantLoc,
                Currency = t.F49_CurrencyCode
            })
            .ToListAsync();

        // Cross-reference alerts to highlight transactions that triggered rules/ML
        var txnIds = rawTransactions.Select(t => (Guid?)t.TransactionId).ToList();
        var matchingAlerts = await _db.FraudAlerts
            .AsNoTracking()
            .Include(a => a.Rule)
            .Where(a => a.TransactionId != null && txnIds.Contains(a.TransactionId))
            .ToListAsync();

        var annotatedData = rawTransactions.Select(t =>
        {
            var alert = matchingAlerts.FirstOrDefault(a => a.TransactionId == t.TransactionId);
            return new
            {
                t.TransactionId,
                t.PAN,
                t.Amount,
                t.TxnDateTime,
                t.MCC,
                t.Country,
                t.POSEntryMode,
                t.ResponseCode,
                t.MerchantLocation,
                t.Currency,
                IsFlagged = alert != null,
                TriggeredRuleId = alert?.RuleId,
                TriggeredRuleName = alert?.Rule != null ? alert.Rule.RuleName : alert?.RuleId,
                AlertScore = alert?.Score ?? 0,
                AlertId = alert?.AlertId
            };
        });

        return Ok(new
        {
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
            Data = annotatedData
        });
    }
}
