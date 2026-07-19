using System.Threading.Tasks;
using System.Threading;
using FraudDetection.Worker.Models;
using FraudDetection.Worker.Rules;

namespace FraudDetection.Worker.Services;
public interface IFraudDetectionEngine
{
    Task ProcessTransactionAsync(DateTime targetdate, CancellationToken cancellationToken);
}