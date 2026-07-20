using System;
using System.Collections.Generic;
using FraudDetection.Worker.Models;

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

    public static List<AuthorizationTransaction> GetSpikePattern(string pan = "4111222233337777")
    {
        var time1 = BaseTime;
        var time2 = BaseTime.AddHours(1);
        var time3 = BaseTime.AddHours(2);
        var time4 = BaseTime.AddHours(3);

        return new List<AuthorizationTransaction>
        {
            new() {
                TransactionId = Guid.NewGuid(),
                Mti = "0100",
                F2_PAN = pan,
                F3_ProcCode = "000000",
                F4_AmountTxn = 25.00m, // Normal transaction amount
                F7_TxnDateTime = time1,
                F11_STAN = "000051",
                F12_LocalTime = TimeOnly.FromDateTime(time1),
                F13_LocalDate = DateOnly.FromDateTime(time1),
                F14_ExpDate = "2912",
                F18_MCC = "5814", // Starbucks
                F19_AcqCountry = "840",
                F22_POSEntryMode = "051",
                F37_RRN = "200000000051",
                F38_AuthCode = "111111",
                F39_ResponseCode = "00",
                F41_TID = "TID00051",
                F42_MID = "MID000051",
                F43_MerchantLoc = "STARBUCKS             SEATTLE      WA",
                F49_CurrencyCode = "840"
            },
            new() {
                TransactionId = Guid.NewGuid(),
                Mti = "0100",
                F2_PAN = pan,
                F3_ProcCode = "000000",
                F4_AmountTxn = 45.50m, // Normal transaction amount
                F7_TxnDateTime = time2,
                F11_STAN = "000052",
                F12_LocalTime = TimeOnly.FromDateTime(time2),
                F13_LocalDate = DateOnly.FromDateTime(time2),
                F14_ExpDate = "2912",
                F18_MCC = "5311", // Target
                F19_AcqCountry = "840",
                F22_POSEntryMode = "051",
                F37_RRN = "200000000052",
                F38_AuthCode = "222222",
                F39_ResponseCode = "00",
                F41_TID = "TID00052",
                F42_MID = "MID000052",
                F43_MerchantLoc = "TARGET                SEATTLE      WA",
                F49_CurrencyCode = "840"
            },
            new() {
                TransactionId = Guid.NewGuid(),
                Mti = "0100",
                F2_PAN = pan,
                F3_ProcCode = "000000",
                F4_AmountTxn = 30.00m, // Normal transaction amount
                F7_TxnDateTime = time3,
                F11_STAN = "000053",
                F12_LocalTime = TimeOnly.FromDateTime(time3),
                F13_LocalDate = DateOnly.FromDateTime(time3),
                F14_ExpDate = "2912",
                F18_MCC = "5814",
                F19_AcqCountry = "840",
                F22_POSEntryMode = "051",
                F37_RRN = "200000000053",
                F38_AuthCode = "333333",
                F39_ResponseCode = "00",
                F41_TID = "TID00053",
                F42_MID = "MID000053",
                F43_MerchantLoc = "STARBUCKS             SEATTLE      WA",
                F49_CurrencyCode = "840"
            },
            new() {
                TransactionId = Guid.NewGuid(),
                Mti = "0100",
                F2_PAN = pan,
                F3_ProcCode = "000000",
                F4_AmountTxn = 2500.00m, // MASSIVE SPIKE!
                F7_TxnDateTime = time4,
                F11_STAN = "000054",
                F12_LocalTime = TimeOnly.FromDateTime(time4),
                F13_LocalDate = DateOnly.FromDateTime(time4),
                F14_ExpDate = "2912",
                F18_MCC = "5732", // Apple Store
                F19_AcqCountry = "840",
                F22_POSEntryMode = "012",
                F37_RRN = "200000000054",
                F38_AuthCode = "444444",
                F39_ResponseCode = "00",
                F41_TID = "TID00054",
                F42_MID = "MID000054",
                F43_MerchantLoc = "APPLE STORE           SEATTLE      WA",
                F49_CurrencyCode = "840"
            }
        };
    }


    public static List<AuthorizationTransaction> GetNormalTransactions(string pan = "4111222233338888")
    {
        var time1 = BaseTime;
        var time2 = BaseTime.AddHours(2.5);
        var time3 = BaseTime.AddHours(6);

        return new List<AuthorizationTransaction>
        {
            new() {
                TransactionId = Guid.NewGuid(),
                Mti = "0100",
                F2_PAN = pan,
                F3_ProcCode = "000000",
                F4_AmountTxn = 15.50m,
                F7_TxnDateTime = time1,
                F11_STAN = "000101",
                F12_LocalTime = new TimeOnly(10, 0, 0), // 10:00 AM
                F13_LocalDate = DateOnly.FromDateTime(time1),
                F14_ExpDate = "2912",
                F18_MCC = "5814", // Starbucks
                F19_AcqCountry = "840", // USA
                F22_POSEntryMode = "051",
                F37_RRN = "300000000101",
                F38_AuthCode = "123456",
                F39_ResponseCode = "00", // Approved
                F41_TID = "TID00011",
                F42_MID = "MID0000101",
                F43_MerchantLoc = "STARBUCKS             SEATTLE      WA",
                F49_CurrencyCode = "840"
            },
            new() {
                TransactionId = Guid.NewGuid(),
                Mti = "0100",
                F2_PAN = pan,
                F3_ProcCode = "000000",
                F4_AmountTxn = 85.00m,
                F7_TxnDateTime = time2,
                F11_STAN = "000102",
                F12_LocalTime = new TimeOnly(12, 30, 0), // 12:30 PM
                F13_LocalDate = DateOnly.FromDateTime(time2),
                F14_ExpDate = "2912",
                F18_MCC = "5311", // Target
                F19_AcqCountry = "840", // USA
                F22_POSEntryMode = "051",
                F37_RRN = "300000000102",
                F38_AuthCode = "654321",
                F39_ResponseCode = "00", // Approved
                F41_TID = "TID00012",
                F42_MID = "MID0000102",
                F43_MerchantLoc = "TARGET                SEATTLE      WA",
                F49_CurrencyCode = "840"
            },
            new() {
                TransactionId = Guid.NewGuid(),
                Mti = "0100",
                F2_PAN = pan,
                F3_ProcCode = "000000",
                F4_AmountTxn = 120.00m,
                F7_TxnDateTime = time3,
                F11_STAN = "000103",
                F12_LocalTime = new TimeOnly(16, 0, 0), // 4:00 PM
                F13_LocalDate = DateOnly.FromDateTime(time3),
                F14_ExpDate = "2912",
                F18_MCC = "5942", // Amazon
                F19_AcqCountry = "840", // USA
                F22_POSEntryMode = "012", // CNP
                F37_RRN = "300000000103",
                F38_AuthCode = "987654",
                F39_ResponseCode = "00", // Approved
                F41_TID = "TID00013",
                F42_MID = "MID0000103",
                F43_MerchantLoc = "AMAZON.COM            SEATTLE      WA",
                F49_CurrencyCode = "840"
            }
        };
    }

    public static List<AuthorizationTransaction> GetExpiryDateBruteForcePattern(string pan = "4111222233339999")
    {
        var list = new List<AuthorizationTransaction>();
        var baseTime = BaseTime;

        // 4 transactions in 2 minutes, with different expiration dates
        for (int i = 0; i < 4; i++)
        {
            var time = baseTime.AddSeconds(i * 30);
            var expMonth = (i + 1).ToString("D2"); // "01", "02", "03", "04"

            list.Add(new AuthorizationTransaction
            {
                TransactionId = Guid.NewGuid(),
                Mti = "0100",
                F2_PAN = pan,
                F3_ProcCode = "000000",
                F4_AmountTxn = 1.05m, // Micro-transaction typical for card testing
                F7_TxnDateTime = time,
                F11_STAN = $"00020{i}",
                F12_LocalTime = TimeOnly.FromDateTime(time),
                F13_LocalDate = DateOnly.FromDateTime(time),
                F14_ExpDate = $"{expMonth}28", // Changing expiration dates
                F18_MCC = "5816",
                F19_AcqCountry = "840",
                F22_POSEntryMode = "012",
                F37_RRN = $"20000000003{i}",
                F38_AuthCode = "",
                F39_ResponseCode = i == 3 ? "00" : "54", // 54 = Expired Card, 00 = Success on last try
                F41_TID = "TID00005",
                F42_MID = "MID0000005",
                F43_MerchantLoc = "ONLINE STORE          SEATTLE      WA",
                F49_CurrencyCode = "840"
            });
        }
        return list;
    }
}
