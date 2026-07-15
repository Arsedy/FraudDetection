using Microsoft.ML.Data;

namespace FraudDetection.Shared.Models;

/// <summary>
/// ML.NET output schema containing the binary classification prediction result.
/// </summary>
public class TransactionPrediction
{
    /// <summary>
    /// The predicted label (true = fraud, false = clean).
    /// </summary>
    [ColumnName("PredictedLabel")]
    public bool PredictedLabel { get; set; }

    /// <summary>
    /// Probability score between 0.0 (clean) and 1.0 (fraud).
    /// This is the primary value used for threshold-based routing:
    ///   - Less than 0.30 → Clean (skip rules)
    ///   - 0.30 to 0.84 → Grey Area (run rules engine)
    ///   - 0.85 or above → Immediate Fraud (skip rules, alert immediately)
    /// </summary>
    [ColumnName("Probability")]
    public float Probability { get; set; }

    /// <summary>
    /// Raw score from the classifier (log-odds). Used internally by ML.NET.
    /// </summary>
    [ColumnName("Score")]
    public float Score { get; set; }
}
