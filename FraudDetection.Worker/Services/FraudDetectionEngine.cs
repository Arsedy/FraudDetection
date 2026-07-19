using System.Threading.Tasks;
using System.Threading;
using FraudDetection.Worker.Models;
using FraudDetection.Worker.Rules;
using FraudDetection.Worker.Repositories;
using FraudDetection.Shared.Models;
using Microsoft.Extensions.ML;


namespace FraudDetection.Worker.Services;

public class FraudDetectionEngine : IFraudDetectionEngine
{
    private readonly ILogger<FraudDetectionEngine> _logger;
    private readonly IEnumerable<IFraudRule> _rule;
    private readonly ITransactionRepository _transaction_repository;
    private readonly IFraudAlertRepository _fraud_alert_repository;
    private readonly PredictionEnginePool<TransactionFeatures, TransactionPrediction>? _predictionPool;
    private readonly bool _mlEnabled;

    // Threshold constants for the hybrid routing logic
    private const float CleanThreshold = 0.30f;
    private const float ImmediateFraudThreshold = 0.85f;
    private const string MlHighConfidenceRuleId = "ML_HighConfidenceFraud";

    public FraudDetectionEngine(
        ILogger<FraudDetectionEngine> logger,
        IEnumerable<IFraudRule> rules,
        ITransactionRepository repository,
        IFraudAlertRepository alertRepository,
        PredictionEnginePool<TransactionFeatures, TransactionPrediction>? predictionPool = null,
        bool mlEnabled = true)
    {
        _logger = logger;
        _rule = rules;
        _transaction_repository = repository;
        _fraud_alert_repository = alertRepository;
        _predictionPool = predictionPool;
        _mlEnabled = mlEnabled && _predictionPool != null;
    }

    public async Task ProcessTransactionAsync(DateTime targetdate,  CancellationToken cancellationToken)
    {
        DateTime startDate = targetdate.Date;
        DateTime endDate = targetdate.Date.AddDays(1);
        List<string> uniquePans = await _transaction_repository.GetUniquePansWithMultipleTransactionsAsync(
                                startDate, endDate, cancellationToken); // returns list of unique PANs with multiple transactions in the specified date range
        
        _logger.LogInformation($"Found {uniquePans.Count} unique PANs with multiple transactions between {startDate} and {endDate}.");
        
        List<AuthorizationTransaction> transactions = await _transaction_repository.GetTransactionsByPansAsync(
                                uniquePans, startDate, endDate, cancellationToken);

        _logger.LogInformation($"Retrieved {transactions.Count} transactions for the unique PANs between {startDate} and {endDate}.");

        var group_by_pan = transactions.GroupBy(t => t.F2_PAN);
        int totalalerts = 0;
        int mlCleanSkipped = 0;
        int mlImmediateFraud = 0;
        int greyAreaProcessed = 0;

        foreach (var group in group_by_pan)
        {
            var transactions_list = group.ToList();

            // ────────────────────────────────────────────────────────
            // HYBRID ML SCORING: Pre-filter each transaction in the group
            // ────────────────────────────────────────────────────────
            if (_mlEnabled)
            {
                bool allClean = true;
                bool anyImmediateFraud = false;
                Guid? immediateFraudTxnId = null;

                foreach (var txn in transactions_list)
                {
                    var features = MapToFeatures(txn);
                    var prediction = _predictionPool!.Predict(features);

                    if (prediction.Probability >= ImmediateFraudThreshold)
                    {
                        // Immediate fraud — flag this transaction without running rules
                        anyImmediateFraud = true;
                        immediateFraudTxnId = txn.TransactionId;
                        allClean = false;
                        break; // One high-confidence fraud in the group is enough to flag
                    }
                    else if (prediction.Probability >= CleanThreshold)
                    {
                        // Grey area — at least one transaction needs deep-dive
                        allClean = false;
                    }
                    // else: clean (score < 0.30), continue checking other txns in group
                }

                // Decision: skip rules if ALL transactions in the group are clean
                if (allClean)
                {
                    mlCleanSkipped++;
                    continue; // Skip this PAN group entirely — saves DB/CPU
                }

                // Decision: immediate fraud — log alert and skip rules
                if (anyImmediateFraud && immediateFraudTxnId.HasValue)
                {
                    mlImmediateFraud++;
                    var alert = new FraudAlert
                    {
                        TransactionId = immediateFraudTxnId.Value,
                        F2_PAN = group.Key,
                        RuleId = MlHighConfidenceRuleId
                    };
                    await _fraud_alert_repository.AddAlertAsync(alert, cancellationToken);
                    totalalerts++;
                    continue; // Skip rules — ML is confident
                }

                // Fall through to grey area: run the rules engine
                greyAreaProcessed++;
            }

            // ────────────────────────────────────────────────────────
            // RULES ENGINE: Deep-dive analysis for grey area transactions
            // ────────────────────────────────────────────────────────
            foreach (var rule in _rule)
            {
                var result = await rule.IsRuleSatisfiedAsync(transactions_list, cancellationToken);
                if(result.IsSatisfied == false)
                {
                    // Create a FraudAlert object and save it to the database
                    var alert = new FraudAlert
                    {
                        TransactionId = result.TransactionId,
                        F2_PAN = group.Key,
                        RuleId = rule.Id
                    };
                    await _fraud_alert_repository.AddAlertAsync(alert, cancellationToken);
                    totalalerts++;
                }
            }
        }
        await _fraud_alert_repository.SaveChangesAsync(cancellationToken);
        _logger.LogInformation($"Total fraud alerts generated: {totalalerts}");

        if (_mlEnabled)
        {
            _logger.LogInformation(
                "ML Hybrid Stats — Clean (skipped): {Clean}, Immediate Fraud: {ImmediateFraud}, Grey Area (rules executed): {GreyArea}",
                mlCleanSkipped, mlImmediateFraud, greyAreaProcessed);
        }
    }

    /// <summary>
    /// Maps an EF Core AuthorizationTransaction entity to the ML.NET feature input schema.
    /// </summary>
    private static TransactionFeatures MapToFeatures(AuthorizationTransaction txn)
    {
        return new TransactionFeatures
        {
            Amount = (float)txn.F4_AmountTxn,
            LocalTimeHour = txn.F12_LocalTime.Hour,
            MCC = txn.F18_MCC.Trim(),
            AcqCountry = txn.F19_AcqCountry.Trim(),
            POSEntryMode = txn.F22_POSEntryMode.Trim(),
            ResponseCode = txn.F39_ResponseCode.Trim(),
            CurrencyCode = txn.F49_CurrencyCode.Trim(),
            Label = false // Label is not used during inference
        };
    }
}
