using FraudDetection.Worker.Models;

namespace FraudDetection.Worker.Repositories;

public interface ITransactionRepository
{
    Task<List<string>> GetUniquePansWithMultipleTransactionsAsync(
        DateTime startDate,
        DateTime endDate, 
        CancellationToken cancellationToken);
    
    //specific method to get transactions by pans and date range
    Task<List<AuthorizationTransaction>> GetTransactionsByPansAsync(
        List<string> pans, 
        DateTime startDate, 
        DateTime endDate, 
        CancellationToken cancellationToken);
}