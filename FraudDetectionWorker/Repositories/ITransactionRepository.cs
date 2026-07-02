using FraudDetectionWorker.Models;

namespace FraudDetectionWorker.Repositories;

public interface ITransactionRepository
{
    Task<List<AuthorizationTransaction>> GetCardHistoryAsync(
        string cardNo, //F2_PAN ?
        DateTime targetTime, 
        TimeSpan windowBefore, 
        TimeSpan windowAfter
    );
}