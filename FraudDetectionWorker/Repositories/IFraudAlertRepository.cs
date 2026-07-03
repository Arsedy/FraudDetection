using FraudDetectionWorker.Models;

namespace FraudDetectionWorker.Repositories;

public interface IFraudAlertRepository
{
    Task AddAlertAsync(FraudAlert alert, CancellationToken cancellationToken);
}
