using Microsoft.EntityFrameworkCore;
using FraudDetectionWorker.Database;
using FraudDetectionWorker.Models;
namespace FraudDetectionWorker.Repositories;
public class TransactionRepository : ITransactionRepository
{
    private readonly AppDbContext _dbContext;

    public TransactionRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<AuthorizationTransaction>> GetCardHistoryAsync(
        string pan,
        DateTime targetTime, 
        TimeSpan window
    )
    {
        var startTime = targetTime - window;
        var endTime = targetTime;

        return await _dbContext.AuthorizationTransactions
            .Where(t => t.F2_PAN == pan && t.F7_TxnDateTime >= startTime && t.F7_TxnDateTime <= endTime)
            .OrderBy(t => t.F7_TxnDateTime)
            .ToListAsync();
    }
}