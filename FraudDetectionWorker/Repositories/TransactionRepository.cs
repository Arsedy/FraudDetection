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
    public async Task<List<string>> GetUniquePansWithMultipleTransactionsAsync(
        DateTime startDate,
        DateTime endDate, 
        CancellationToken cancellationToken)
    {
        return await _dbContext.AuthorizationTransactions
            .AsNoTracking()
            .Where(t => t.F7_TxnDateTime >= startDate && t.F7_TxnDateTime <= endDate)
            .GroupBy(t => t.F2_PAN)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AuthorizationTransaction>> GetTransactionsByPansAsync(
        List<string> pans, 
        DateTime startDate, 
        DateTime endDate, 
        CancellationToken cancellationToken)
    {
        return await _dbContext.AuthorizationTransactions
            .AsNoTracking()
            .Where(t => pans.Contains(t.F2_PAN) && 
                t.F7_TxnDateTime >= startDate &&
                t.F7_TxnDateTime <= endDate)
            .OrderBy(t => t.F7_TxnDateTime)
            .ToListAsync(cancellationToken);
    }

}