using FraudDetectionWorker.Database;
using FraudDetectionWorker.Models;

namespace FraudDetectionWorker.Repositories;

public class FraudAlertRepository : IFraudAlertRepository
{
    private readonly AppDbContext _dbContext;

    public FraudAlertRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAlertAsync(FraudAlert alert, CancellationToken cancellationToken)
    {
        await _dbContext.FraudAlerts.AddAsync(alert, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
