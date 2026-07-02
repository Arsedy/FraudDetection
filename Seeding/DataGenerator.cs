using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace FraudDetectionWorker.Seeding;

public class DataGenerator
{
    private readonly Random _rand = new(42); // Seeded for reproducibility
    private readonly List<string> _cardPool = [];
    private readonly List<MerchantTemplate> _merchantPool = [];
    
    // ISO 3166 numeric country codes
    private static readonly string[] Countries = ["840", "792", "826", "250", "276", "380", "392", "036"]; // USA, Turkey, UK, France, Germany, Italy, Japan, Australia

    public DataGenerator(int cardPoolSize = 50000, int merchantPoolSize = 10000)
    {
        GenerateCardPool(cardPoolSize);
        GenerateMerchantPool(merchantPoolSize);
    }

    private void GenerateCardPool(int size)
    {
        for (int i = 0; i < size; i++)
        {
            _cardPool.Add(GenerateValidPan());
        }
    }

    private string GenerateValidPan()
    {
        int cardType = _rand.Next(1, 4);
        string bin = cardType switch
        {
            1 => "4" + _rand.Next(10000, 99999),
            2 => "5" + _rand.Next(10, 56) + _rand.Next(1000, 9999),
            _ => "37" + _rand.Next(1000, 9999) + _rand.Next(10, 99)
        };

        int length = cardType == 3 ? 15 : 16;
        var sb = new StringBuilder(bin);
        while (sb.Length < length - 1)
        {
            sb.Append(_rand.Next(0, 10));
        }

        int luhnDigit = CalculateLuhnDigit(sb.ToString());
        sb.Append(luhnDigit);
        return sb.ToString();
    }

    private int CalculateLuhnDigit(string number)
    {
        int sum = 0;
        bool alternate = true;
        for (int i = number.Length - 1; i >= 0; i--)
        {
            int n = number[i] - '0';
            if (alternate)
            {
                n *= 2;
                if (n > 9)
                {
                    n = (n % 10) + 1;
                }
            }
            sum += n;
            alternate = !alternate;
        }
        return (10 - (sum % 10)) % 10;
    }

    private void GenerateMerchantPool(int size)
    {
        var names = new[] { 
            "AMAZON.COM", "WAL-MART", "TARGET", "BEST BUY", "EXXONMOBIL", "SHELL OIL", "CHEVRON",
            "MARRIOTT HOTELS", "HILTON HOTELS", "DELTA AIRLINES", "STEAM GAMES", "APPLE STORE",
            "NETFLIX", "SPOTIFY", "UBER RIDE", "STARBUCKS", "MC DONALD'S", "CASINO GRANDE", 
            "GOLD JEWELERS", "BINANCE EXCH", "CASH APP", "PATEK PHILIPPE" 
        };

        var mccs = new Dictionary<string, string>
        {
            { "AMAZON.COM", "5942" },
            { "WAL-MART", "5311" },
            { "TARGET", "5311" },
            { "BEST BUY", "5732" },
            { "EXXONMOBIL", "5541" },
            { "SHELL OIL", "5541" },
            { "CHEVRON", "5541" },
            { "MARRIOTT HOTELS", "3501" },
            { "HILTON HOTELS", "3501" },
            { "DELTA AIRLINES", "4511" },
            { "STEAM GAMES", "5816" },
            { "APPLE STORE", "5732" },
            { "NETFLIX", "4899" },
            { "SPOTIFY", "4899" },
            { "UBER RIDE", "4121" },
            { "STARBUCKS", "5814" },
            { "MC DONALD'S", "5814" },
            { "CASINO GRANDE", "7995" },
            { "GOLD JEWELERS", "5944" },
            { "BINANCE EXCH", "6012" },
            { "CASH APP", "6012" },
            { "PATEK PHILIPPE", "5944" }
        };

        var locations = new[] { "SEATTLE      WA", "BENTONVILLE  AR", "MINNEAPOLIS  MN", "NEW YORK     NY", "HOUSTON      TX", "SAN JOSE     CA", "CHICAGO      IL", "LOS ANGELES  CA", "LONDON       GB", "PARIS        FR", "MUNICH       DE", "ISTANBUL     TR" };

        for (int i = 0; i < size; i++)
        {
            string name = names[_rand.Next(names.Length)];
            string mcc = mccs[name];
            string locName = locations[_rand.Next(locations.Length)];
            string country = locName.Contains("GB") ? "826" : 
                             locName.Contains("FR") ? "250" : 
                             locName.Contains("DE") ? "276" : 
                             locName.Contains("TR") ? "792" : "840";

            string f43Name = (name.Length > 22 ? name[..22] : name).PadRight(22);
            string f43Loc = (locName.Length > 18 ? locName[..18] : locName).PadRight(18);
            string merchantLocation = f43Name + f43Loc;

            _merchantPool.Add(new MerchantTemplate
            {
                MID = "MID" + _rand.Next(1000000, 9999999).ToString().PadLeft(12, '0'),
                TID = "TID" + _rand.Next(10000, 99999).ToString(),
                NameAndLocation = merchantLocation,
                MCC = mcc,
                Country = country
            });
        }
    }

    public static DataTable CreateDataTable()
    {
        var auths = new DataTable("AuthorizationTransactions");
        auths.Columns.Add("TransactionId", typeof(Guid));
        auths.Columns.Add("MTI", typeof(string));
        auths.Columns.Add("F2_PAN", typeof(string));
        auths.Columns.Add("F3_ProcCode", typeof(string));
        auths.Columns.Add("F4_AmountTxn", typeof(decimal));
        auths.Columns.Add("F7_TxnDateTime", typeof(DateTime));
        auths.Columns.Add("F11_STAN", typeof(string));
        auths.Columns.Add("F12_LocalTime", typeof(TimeSpan));
        auths.Columns.Add("F13_LocalDate", typeof(DateTime));
        auths.Columns.Add("F14_ExpDate", typeof(string));
        auths.Columns.Add("F18_MCC", typeof(string));
        auths.Columns.Add("F19_AcqCountry", typeof(string));
        auths.Columns.Add("F22_POSEntryMode", typeof(string));
        auths.Columns.Add("F37_RRN", typeof(string));
        auths.Columns.Add("F38_AuthCode", typeof(string));
        auths.Columns.Add("F39_ResponseCode", typeof(string));
        auths.Columns.Add("F41_TID", typeof(string));
        auths.Columns.Add("F42_MID", typeof(string));
        auths.Columns.Add("F43_MerchantLoc", typeof(string));
        auths.Columns.Add("F49_CurrencyCode", typeof(string));
        return auths;
    }

    public DataTable GenerateBatch(int count, ref long currentStan, ref long currentRrn, DateTime baseDate, double fraudRate = 0.015)
    {
        var auths = CreateDataTable();

        int fraudCountTarget = (int)(count * fraudRate);
        int generatedFraud = 0;

        int i = 0;
        while (i < count)
        {
            string pan = _cardPool[_rand.Next(_cardPool.Count)];
            DateTime txnDateTime = baseDate.AddSeconds(_rand.Next(0, 86400 * 90));

            bool generateFraudPattern = (generatedFraud < fraudCountTarget) && (_rand.NextDouble() < 0.1);

            if (generateFraudPattern)
            {
                int pattern = _rand.Next(1, 5);
                int injectedCount = pattern switch
                {
                    1 => GenerateCNPVelocityPattern(auths, pan, txnDateTime, ref currentStan, ref currentRrn),
                    2 => GenerateImpossibleTravelPattern(auths, pan, txnDateTime, ref currentStan, ref currentRrn),
                    3 => GenerateCardTestingPattern(auths, pan, txnDateTime, ref currentStan, ref currentRrn),
                    _ => GenerateHighRiskOffHoursPattern(auths, pan, txnDateTime, ref currentStan, ref currentRrn)
                };

                generatedFraud += injectedCount;
                i += injectedCount;
            }
            else
            {
                GenerateNormalTransaction(auths, pan, txnDateTime, ref currentStan, ref currentRrn);
                i++;
            }
        }

        return auths;
    }

    private void GenerateNormalTransaction(DataTable auths, string pan, DateTime txnTime, ref long stan, ref long rrn)
    {
        var merchant = _merchantPool[_rand.Next(_merchantPool.Count)];
        
        decimal amount = (decimal)(_rand.NextDouble() * 120 + 2.50);
        if (_rand.NextDouble() < 0.05) amount = (decimal)(_rand.NextDouble() * 800 + 100);

        string entryMode = _rand.Next(1, 4) switch
        {
            1 => "051",
            2 => "071",
            _ => "012"
        };

        string rrnStr = GetNextRrn(ref rrn);
        string stanStr = GetNextStan(ref stan);

        AddAuthRow(auths, 
            transactionId: Guid.NewGuid(),
            mti: "0100",
            pan: pan,
            procCode: "000000",
            amount: amount,
            txnTime: txnTime,
            stan: stanStr,
            rrn: rrnStr,
            authCode: _rand.Next(100000, 999999).ToString(),
            respCode: "00",
            mcc: merchant.MCC,
            country: merchant.Country,
            entryMode: entryMode,
            tid: merchant.TID,
            mid: merchant.MID,
            location: merchant.NameAndLocation,
            currency: "840"
        );
    }

    private int GenerateCNPVelocityPattern(DataTable auths, string pan, DateTime baseTime, ref long stan, ref long rrn)
    {
        int txCount = _rand.Next(5, 9);
        decimal baseAmount = (decimal)(_rand.NextDouble() * 300 + 150);
        
        for (int step = 0; step < txCount; step++)
        {
            DateTime txnTime = baseTime.AddMinutes(step * _rand.Next(1, 3));
            var merchant = _merchantPool.Find(m => m.MCC == "5942" || m.MCC == "5816" || m.MCC == "6012") ?? _merchantPool[0];
            decimal amount = baseAmount + (step * 50);

            string rrnStr = GetNextRrn(ref rrn);
            string stanStr = GetNextStan(ref stan);

            string respCode = step < 4 ? "00" : "51";
            string authCode = respCode == "00" ? _rand.Next(100000, 999999).ToString() : "";

            AddAuthRow(auths,
                transactionId: Guid.NewGuid(),
                mti: "0100",
                pan: pan,
                procCode: "000000",
                amount: amount,
                txnTime: txnTime,
                stan: stanStr,
                rrn: rrnStr,
                authCode: authCode,
                respCode: respCode,
                mcc: merchant.MCC,
                country: merchant.Country,
                entryMode: "012",
                tid: merchant.TID,
                mid: merchant.MID,
                location: merchant.NameAndLocation,
                currency: "840"
            );
        }

        return txCount;
    }

    private int GenerateImpossibleTravelPattern(DataTable auths, string pan, DateTime baseTime, ref long stan, ref long rrn)
    {
        var usaMerchant = _merchantPool.Find(m => m.Country == "840") ?? _merchantPool[0];
        string rrn1 = GetNextRrn(ref rrn);
        string stan1 = GetNextStan(ref stan);
        decimal amount1 = (decimal)(_rand.NextDouble() * 80 + 10);

        AddAuthRow(auths,
            transactionId: Guid.NewGuid(),
            mti: "0100",
            pan: pan,
            procCode: "000000",
            amount: amount1,
            txnTime: baseTime,
            stan: stan1,
            rrn: rrn1,
            authCode: _rand.Next(100000, 999999).ToString(),
            respCode: "00",
            mcc: usaMerchant.MCC,
            country: "840",
            entryMode: "051",
            tid: usaMerchant.TID,
            mid: usaMerchant.MID,
            location: usaMerchant.NameAndLocation,
            currency: "840"
        );

        DateTime travelTxnTime = baseTime.AddMinutes(_rand.Next(25, 45));
        var euroMerchant = _merchantPool.Find(m => m.Country == "792" || m.Country == "276") ?? _merchantPool[1];
        string rrn2 = GetNextRrn(ref rrn);
        string stan2 = GetNextStan(ref stan);
        decimal amount2 = (decimal)(_rand.NextDouble() * 300 + 50);

        AddAuthRow(auths,
            transactionId: Guid.NewGuid(),
            mti: "0100",
            pan: pan,
            procCode: "000000",
            amount: amount2,
            txnTime: travelTxnTime,
            stan: stan2,
            rrn: rrn2,
            authCode: _rand.Next(100000, 999999).ToString(),
            respCode: "00",
            mcc: euroMerchant.MCC,
            country: euroMerchant.Country,
            entryMode: "051",
            tid: euroMerchant.TID,
            mid: euroMerchant.MID,
            location: euroMerchant.NameAndLocation,
            currency: euroMerchant.Country == "792" ? "949" : "978"
        );

        return 2;
    }

    private int GenerateCardTestingPattern(DataTable auths, string pan, DateTime baseTime, ref long stan, ref long rrn)
    {
        int declineCount = _rand.Next(3, 6);
        var onlineMerchant = _merchantPool.Find(m => m.MCC == "5816" || m.MCC == "5942") ?? _merchantPool[0];

        for (int step = 0; step < declineCount; step++)
        {
            DateTime txnTime = baseTime.AddSeconds(step * _rand.Next(10, 40));
            decimal smallAmount = (decimal)(_rand.NextDouble() * 1.50 + 0.10);

            string rrnStr = GetNextRrn(ref rrn);
            string stanStr = GetNextStan(ref stan);

            AddAuthRow(auths,
                transactionId: Guid.NewGuid(),
                mti: "0100",
                pan: pan,
                procCode: "000000",
                amount: smallAmount,
                txnTime: txnTime,
                stan: stanStr,
                rrn: rrnStr,
                authCode: "",
                respCode: "05",
                mcc: onlineMerchant.MCC,
                country: onlineMerchant.Country,
                entryMode: "012",
                tid: onlineMerchant.TID,
                mid: onlineMerchant.MID,
                location: onlineMerchant.NameAndLocation,
                currency: "840"
            );
        }

        DateTime successTime = baseTime.AddSeconds(declineCount * 30 + 10);
        decimal bigAmount = (decimal)(_rand.NextDouble() * 500 + 400);
        string successRrn = GetNextRrn(ref rrn);
        string successStan = GetNextStan(ref stan);

        AddAuthRow(auths,
            transactionId: Guid.NewGuid(),
            mti: "0100",
            pan: pan,
            procCode: "000000",
            amount: bigAmount,
            txnTime: successTime,
            stan: successStan,
            rrn: successRrn,
            authCode: _rand.Next(100000, 999999).ToString(),
            respCode: "00",
            mcc: onlineMerchant.MCC,
            country: onlineMerchant.Country,
            entryMode: "012",
            tid: onlineMerchant.TID,
            mid: onlineMerchant.MID,
            location: onlineMerchant.NameAndLocation,
            currency: "840"
        );

        return declineCount + 1;
    }

    private int GenerateHighRiskOffHoursPattern(DataTable auths, string pan, DateTime baseTime, ref long stan, ref long rrn)
    {
        DateTime local3Am = new(baseTime.Year, baseTime.Month, baseTime.Day, 3, _rand.Next(0, 60), _rand.Next(0, 60));
        var highRiskMerch = _merchantPool.Find(m => m.MCC == "7995" || m.MCC == "5944") ?? _merchantPool[0];

        decimal amount = (decimal)(_rand.NextDouble() * 1500 + 800);

        string rrnStr = GetNextRrn(ref rrn);
        string stanStr = GetNextStan(ref stan);

        AddAuthRow(auths,
            transactionId: Guid.NewGuid(),
            mti: "0100",
            pan: pan,
            procCode: "000000",
            amount: amount,
            txnTime: local3Am,
            stan: stanStr,
            rrn: rrnStr,
            authCode: _rand.Next(100000, 999999).ToString(),
            respCode: "00",
            mcc: highRiskMerch.MCC,
            country: highRiskMerch.Country,
            entryMode: "012",
            tid: highRiskMerch.TID,
            mid: highRiskMerch.MID,
            location: highRiskMerch.NameAndLocation,
            currency: "840"
        );

        return 1;
    }

    private static void AddAuthRow(DataTable table, Guid transactionId, string mti, string pan, string procCode, 
        decimal amount, DateTime txnTime, string stan, string rrn, string? authCode, string respCode, 
        string mcc, string country, string entryMode, string tid, string mid, string location, string currency)
    {
        table.Rows.Add(
            transactionId,
            mti,
            pan,
            procCode,
            amount,
            txnTime,
            stan,
            txnTime.TimeOfDay,
            txnTime.Date,
            "2912",
            mcc,
            country,
            entryMode,
            rrn,
            string.IsNullOrEmpty(authCode) ? DBNull.Value : authCode,
            respCode,
            tid,
            mid,
            location,
            currency
        );
    }

    private static string GetNextStan(ref long stan)
    {
        stan = (stan % 999999) + 1;
        return stan.ToString().PadLeft(6, '0');
    }

    private static string GetNextRrn(ref long rrn)
    {
        if (rrn >= 999999999999)
        {
            rrn = 100000000000;
        }
        else
        {
            rrn++;
        }
        return rrn.ToString();
    }
}

public class MerchantTemplate
{
    public string MID { get; set; } = "";
    public string TID { get; set; } = "";
    public string NameAndLocation { get; set; } = "";
    public string MCC { get; set; } = "";
    public string Country { get; set; } = "";
}
