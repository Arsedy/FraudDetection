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

    public async Task<List<AuthorizationTransaction>> GetCardHistoryAsync( //returns a list of transactions for a specific card number within a specified time window
        string cardNo, //F2_PAN ?
        DateTime targetTime, 
        TimeSpan windowBefore, 
        TimeSpan windowAfter
    )
    {
        var startTime = targetTime - windowBefore;
        var endTime = targetTime + windowAfter;

        return await _dbContext.AuthorizationTransactions
            .Where(t => t.F2_PAN == cardNo && t.F7_TxnDateTime >= startTime && t.F7_TxnDateTime <= endTime)
            .OrderBy(t => t.F7_TxnDateTime)
            .ToListAsync();
    }
}