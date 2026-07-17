using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FraudDetectionWorker.Database;

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
    /// GET /api/transactions — Paginated transaction log with optional filters.
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

        var transactions = await query
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

        return Ok(new
        {
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
            Data = transactions
        });
    }
}
