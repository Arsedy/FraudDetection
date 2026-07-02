using System;
using System.Collections.Generic;
using FraudDetectionWorker.Models;

namespace Tests;

public static class FraudTestData
{
    private static readonly DateTime BaseTime = DateTime.UtcNow;

    public static List<AuthorizationTransaction> GetCnpVelocityPattern(string pan = "4111222233334444")
    {
        var list = new List<AuthorizationTransaction>();
        for (int i = 0; i < 6; i++)
        {
            var time = BaseTime.AddMinutes(i * 1.5);
            list.Add(new AuthorizationTransaction
            {
                TransactionId = Guid.NewGuid(),
                Mti = "0100",
                F2_PAN = pan,
                F3_ProcCode = "000000",
                F4_AmountTxn = 150.00m + (i * 20.00m),
                F7_TxnDateTime = time,
                F11_STAN = $"0000{i + 1}",
                F12_LocalTime = TimeOnly.FromDateTime(time),
                F13_LocalDate = DateOnly.FromDateTime(time),
                F14_ExpDate = "2912",
                F18_MCC = "5942", // Online Bookstore
                F19_AcqCountry = "840",
                F22_POSEntryMode = "012", // CNP
                F37_RRN = $"2000000000{i + 1}",
                F38_AuthCode = "123456",
                F39_ResponseCode = "00",
                F41_TID = "TID00001",
                F42_MID = "MID0000001",
                F43_MerchantLoc = "AMAZON.COM            SEATTLE      WA",
                F49_CurrencyCode = "840"
            });
        }
        return list;
    }

    public static List<AuthorizationTransaction> GetImpossibleTravelPattern(string pan = "4111222233335555")
    {
        var time1 = BaseTime;
        var time2 = BaseTime.AddMinutes(20); // 20 minutes interval between US and Turkey transactions

        return new List<AuthorizationTransaction>
        {
            new() {
                TransactionId = Guid.NewGuid(),
                Mti = "0100",
                F2_PAN = pan,
                F3_ProcCode = "000000",
                F4_AmountTxn = 45.00m,
                F7_TxnDateTime = time1,
                F11_STAN = "000010",
                F12_LocalTime = TimeOnly.FromDateTime(time1),
                F13_LocalDate = DateOnly.FromDateTime(time1),
                F14_ExpDate = "2912",
                F18_MCC = "5814", // Starbucks
                F19_AcqCountry = "840", // USA
                F22_POSEntryMode = "051",
                F37_RRN = "200000000010",
                F38_AuthCode = "123456",
                F39_ResponseCode = "00",
                F41_TID = "TID00002",
                F42_MID = "MID0000002",
                F43_MerchantLoc = "STARBUCKS             SEATTLE      WA",
                F49_CurrencyCode = "840"
            },
            new() {
                TransactionId = Guid.NewGuid(),
                Mti = "0100",
                F2_PAN = pan,
                F3_ProcCode = "000000",
                F4_AmountTxn = 120.00m,
                F7_TxnDateTime = time2,
                F11_STAN = "000011",
                F12_LocalTime = TimeOnly.FromDateTime(time2),
                F13_LocalDate = DateOnly.FromDateTime(time2),
                F14_ExpDate = "2912",
                F18_MCC = "5814",
                F19_AcqCountry = "792", // Turkey
                F22_POSEntryMode = "051",
                F37_RRN = "200000000011",
                F38_AuthCode = "654321",
                F39_ResponseCode = "00",
                F41_TID = "TID00003",
                F42_MID = "MID0000003",
                F43_MerchantLoc = "STARBUCKS             ISTANBUL     TR",
                F49_CurrencyCode = "949"
            }
        };
    }

    public static List<AuthorizationTransaction> GetCardTestingPattern(string pan = "4111222233336666")
    {
        var list = new List<AuthorizationTransaction>();
        
        // 3 small declines
        for (int i = 0; i < 3; i++)
        {
            var time = BaseTime.AddSeconds(i * 15);
            list.Add(new AuthorizationTransaction
            {
                TransactionId = Guid.NewGuid(),
                Mti = "0100",
                F2_PAN = pan,
                F3_ProcCode = "000000",
                F4_AmountTxn = 1.05m + (i * 0.10m), // Small amounts under $2.00
                F7_TxnDateTime = time,
                F11_STAN = $"00002{i}",
                F12_LocalTime = TimeOnly.FromDateTime(time),
                F13_LocalDate = DateOnly.FromDateTime(time),
                F14_ExpDate = "2912",
                F18_MCC = "5816", // Online Gaming
                F19_AcqCountry = "840",
                F22_POSEntryMode = "012", // CNP
                F37_RRN = $"20000000002{i}",
                F38_AuthCode = "",
                F39_ResponseCode = "05", // Declined (Do Not Honor)
                F41_TID = "TID00004",
                F42_MID = "MID0000004",
                F43_MerchantLoc = "STEAM GAMES           SEATTLE      WA",
                F49_CurrencyCode = "840"
            });
        }

        // 1 big success
        var successTime = BaseTime.AddSeconds(60);
        list.Add(new AuthorizationTransaction
        {
            TransactionId = Guid.NewGuid(),
            Mti = "0100",
            F2_PAN = pan,
            F3_ProcCode = "000000",
            F4_AmountTxn = 850.00m, // Large amount
            F7_TxnDateTime = successTime,
            F11_STAN = "000023",
            F12_LocalTime = TimeOnly.FromDateTime(successTime),
            F13_LocalDate = DateOnly.FromDateTime(successTime),
            F14_ExpDate = "2912",
            F18_MCC = "5816",
            F19_AcqCountry = "840",
            F22_POSEntryMode = "012",
            F37_RRN = "200000000023",
            F38_AuthCode = "998877",
            F39_ResponseCode = "00", // Approved
            F41_TID = "TID00004",
            F42_MID = "MID0000004",
            F43_MerchantLoc = "STEAM GAMES           SEATTLE      WA",
            F49_CurrencyCode = "840"
        });

        return list;
    }

    public static List<AuthorizationTransaction> GetHighRiskOffHoursPattern(string pan = "4111222233337777")
    {
        // 3:15 AM local time
        var offHoursDateTime = new DateTime(BaseTime.Year, BaseTime.Month, BaseTime.Day, 3, 15, 0, DateTimeKind.Utc);

        return new List<AuthorizationTransaction>
        {
            new() {
                TransactionId = Guid.NewGuid(),
                Mti = "0100",
                F2_PAN = pan,
                F3_ProcCode = "000000",
                F4_AmountTxn = 1200.00m, // Large amount
                F7_TxnDateTime = offHoursDateTime,
                F12_LocalTime = new TimeOnly(3, 15, 0),
                F13_LocalDate = DateOnly.FromDateTime(offHoursDateTime),
                F14_ExpDate = "2912",
                F18_MCC = "7995", // High-risk category (Casinos/Betting)
                F19_AcqCountry = "840",
                F22_POSEntryMode = "012",
                F37_RRN = "200000000030",
                F38_AuthCode = "112233",
                F39_ResponseCode = "00",
                F41_TID = "TID00005",
                F42_MID = "MID0000005",
                F43_MerchantLoc = "CASINO GRANDE         LAS VEGAS    NV",
                F49_CurrencyCode = "840"
            }
        };
    }
}
