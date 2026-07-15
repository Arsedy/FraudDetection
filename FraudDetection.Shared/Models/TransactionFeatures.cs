using Microsoft.ML.Data;

namespace FraudDetection.Shared.Models;

/// <summary>
/// ML.NET input schema representing the feature columns extracted from an authorization transaction.
/// Column names must match the SQL aliases returned by the DatabaseLoader query.
/// </summary>
public class TransactionFeatures
{
    /// <summary>
    /// Transaction amount (ISO 8583 Field 4).
    /// </summary>
    [LoadColumn(0)]
    public float Amount { get; set; }

    /// <summary>
    /// Hour of the day the transaction occurred (0-23), extracted from F12_LocalTime.
    /// </summary>
    [LoadColumn(1)]
    public float LocalTimeHour { get; set; }

    /// <summary>
    /// Merchant Category Code (ISO 8583 Field 18). Categorical feature.
    /// </summary>
    [LoadColumn(2)]
    public string MCC { get; set; } = string.Empty;

    /// <summary>
    /// Acquirer Country Code (ISO 8583 Field 19). Categorical feature.
    /// </summary>
    [LoadColumn(3)]
    public string AcqCountry { get; set; } = string.Empty;

    /// <summary>
    /// Point-of-Sale Entry Mode (ISO 8583 Field 22). Categorical feature.
    /// </summary>
    [LoadColumn(4)]
    public string POSEntryMode { get; set; } = string.Empty;

    /// <summary>
    /// Response Code (ISO 8583 Field 39). Categorical feature.
    /// </summary>
    [LoadColumn(5)]
    public string ResponseCode { get; set; } = string.Empty;

    /// <summary>
    /// Currency Code (ISO 8583 Field 49). Categorical feature.
    /// </summary>
    [LoadColumn(6)]
    public string CurrencyCode { get; set; } = string.Empty;

    /// <summary>
    /// Binary label: true = fraud (transaction exists in fraudalerts), false = clean.
    /// </summary>
    [LoadColumn(7)]
    [ColumnName("Label")]
    public bool Label { get; set; }
}
