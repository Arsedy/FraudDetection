namespace FraudDetectionWorker;


public class FraudWorker : BackgroundService
{
    private readonly ILogger<FraudWorker> _logger;
    private readonly IRulesRepository _rules;

    public FraudWorker(ILogger<FraudWorker> logger, IRulesRepository rulesRepository)
    {
        _logger = logger;
        _rules = rulesRepository;
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        //Fraud rules will be checked as foreac _rulesRepository in the system, 
        //and if any of the rules are satisfied, the transaction will be flagged as suspicious and 
        //sent to the fraud detection team for further investigation.


        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }
            await Task.Delay(1000, stoppingToken);
        }
    }
}
