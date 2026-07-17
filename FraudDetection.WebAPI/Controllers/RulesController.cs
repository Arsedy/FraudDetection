using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FraudDetectionWorker.Database;

namespace FraudDetection.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RulesController : ControllerBase
{
    private readonly AppDbContext _db;

    public RulesController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// GET /api/rules — List all active fraud detection rules.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetRules()
    {
        var rules = await _db.Rules
            .AsNoTracking()
            .Select(r => new
            {
                r.RuleId,
                r.RuleName,
                r.RuleDescription
            })
            .OrderBy(r => r.RuleName)
            .ToListAsync();

        return Ok(rules);
    }
}
