using System.Threading.Tasks;
using System.Threading;
using FraudDetectionWorker.Models;
using FraudDetectionWorker.Rules;

namespace FraudDetectionWorker.Services;
public interface IFraudDetectionEngine
{
    Task ProcessTransactionAsync(DateTime targetdate, CancellationToken cancellationToken);
}