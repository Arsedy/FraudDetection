using FraudDetectionWorker.Models;

namespace FraudDetectionWorker.Repositories;

public interface IFraudRepository
{
    Task PutPanToFraudTableAsync(string pan, CancellationToken cancellationToken);
}
