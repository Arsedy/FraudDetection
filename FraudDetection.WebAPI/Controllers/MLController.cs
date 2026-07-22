using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FraudDetection.Worker.Database;
using FraudDetection.Worker.Models;
using FraudDetection.Worker.Services;
using Microsoft.Extensions.ML;
using FraudDetection.Shared.Models;

namespace FraudDetection.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MLController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly PredictionEnginePool<TransactionFeatures, TransactionPrediction> _mlModelService;
    public MLController(AppDbContext db , PredictionEnginePool<TransactionFeatures, TransactionPrediction> mlModelService)
    {
        _db = db;
        _mlModelService = mlModelService;
    }

    /// <summary>
    /// post /api/ml/predict — Get machine learning model predictions.
    /// </summary>
    [HttpPost("predict")]
    public async Task<IActionResult> Predict([FromBody] AuthorizationTransaction transaction)
    {
        if (transaction == null)
        {
            return BadRequest("Transaction data is required.");
        }
        var features = MapToFeatures(transaction);
        var prediction = _mlModelService.Predict(features);

        return Ok(new
        {
            features,  
            prediction.Probability
        });
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
