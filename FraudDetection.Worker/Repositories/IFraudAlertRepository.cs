using FraudDetection.Worker.Models;

namespace FraudDetection.Worker.Repositories;

public interface IFraudAlertRepository
{
    Task AddAlertAsync(FraudAlert alert, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
