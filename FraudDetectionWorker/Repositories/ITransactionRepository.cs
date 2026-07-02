using FraudDetectionWorker.Models;

namespace FraudDetectionWorker.Repositories;

public interface ITransactionRepository
{
    Task<List<AuthorizationTransaction>> GetCardHistoryAsync(
        string pan,
        DateTime targetTime, 
        TimeSpan window
    );
}