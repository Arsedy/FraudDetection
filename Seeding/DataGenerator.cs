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
    // ISO 4217 numeric currency codes
    private static readonly string[] Currencies = ["840", "978", "826", "392", "036"]; // USD, EUR, GBP, JPY, AUD

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
        // 1 = Visa (starts with 4), 2 = Mastercard (starts with 51-55), 3 = Amex (starts with 37)
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
            { "AMAZON.COM", "5942" }, // Bookstores/Retail
            { "WAL-MART", "5311" },   // Department Stores
            { "TARGET", "5311" },
            { "BEST BUY", "5732" },   // Electronics
            { "EXXONMOBIL", "5541" }, // Service Stations
            { "SHELL OIL", "5541" },
            { "CHEVRON", "5541" },
            { "MARRIOTT HOTELS", "3501" }, // Hotel
            { "HILTON HOTELS", "3501" },
            { "DELTA AIRLINES", "4511" },  // Airlines
            { "STEAM GAMES", "5816" },     // Digital Goods
            { "APPLE STORE", "5732" },
            { "NETFLIX", "4899" },         // Cable/Streaming
            { "SPOTIFY", "4899" },
            { "UBER RIDE", "4121" },       // Taxicabs/Limousines
            { "STARBUCKS", "5814" },       // Fast Food
            { "MC DONALD'S", "5814" },
            { "CASINO GRANDE", "7995" },   // Betting/Gambling (High Risk)
            { "GOLD JEWELERS", "5944" },   // Jewelry (High Risk)
            { "BINANCE EXCH", "6012" },    // Financial (High Risk)
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
                             locName.Contains("TR") ? "792" : "840"; // USA by default

            // Standardize merchant name to fit F43 (40 chars)
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

    public static (DataTable auths, DataTable clearings) CreateDataTables()
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
        auths.Columns.Add("IsFraud", typeof(bool));
        auths.Columns.Add("FraudRuleReason", typeof(string));

        var clearings = new DataTable("ClearingTransactions");
        clearings.Columns.Add("ClearingId", typeof(Guid));
        clearings.Columns.Add("F2_PAN", typeof(string));
        clearings.Columns.Add("F37_RRN", typeof(string));
        clearings.Columns.Add("SettlementAmount", typeof(decimal));
        clearings.Columns.Add("SettlementCurrency", typeof(string));
        clearings.Columns.Add("InterchangeFee", typeof(decimal));
        clearings.Columns.Add("SettlementDate", typeof(DateTime));
        clearings.Columns.Add("ClearingStatus", typeof(string));
        clearings.Columns.Add("ReconciliationStatus", typeof(string));

        return (auths, clearings);
    }

    public (DataTable auths, DataTable clearings) GenerateBatch(int count, ref long currentStan, ref long currentRrn, DateTime baseDate, double fraudRate = 0.015)
    {
        var (auths, clearings) = CreateDataTables();

        int fraudCountTarget = (int)(count * fraudRate);
        int generatedFraud = 0;

        int i = 0;
        while (i < count)
        {
            // Pick a random card
            string pan = _cardPool[_rand.Next(_cardPool.Count)];
            
            // Random timestamp within the window
            DateTime txnDateTime = baseDate.AddSeconds(_rand.Next(0, 86400 * 90)); // spread over 90 days

            // Determine if this card will trigger a fraud pattern
            bool generateFraudPattern = (generatedFraud < fraudCountTarget) && (_rand.NextDouble() < 0.1);

            if (generateFraudPattern)
            {
                int pattern = _rand.Next(1, 5);
                int injectedCount = pattern switch
                {
                    1 => GenerateCNPVelocityPattern(auths, clearings, pan, txnDateTime, ref currentStan, ref currentRrn),
                    2 => GenerateImpossibleTravelPattern(auths, clearings, pan, txnDateTime, ref currentStan, ref currentRrn),
                    3 => GenerateCardTestingPattern(auths, clearings, pan, txnDateTime, ref currentStan, ref currentRrn),
                    _ => GenerateHighRiskOffHoursPattern(auths, clearings, pan, txnDateTime, ref currentStan, ref currentRrn)
                };

                generatedFraud += injectedCount;
                i += injectedCount;
            }
            else
            {
                // Generate a normal legitimate transaction
                GenerateNormalTransaction(auths, clearings, pan, txnDateTime, ref currentStan, ref currentRrn);
                i++;
            }
        }

        return (auths, clearings);
    }

    private void GenerateNormalTransaction(DataTable auths, DataTable clearings, string pan, DateTime txnTime, ref long stan, ref long rrn)
    {
        var merchant = _merchantPool[_rand.Next(_merchantPool.Count)];
        
        decimal amount = (decimal)(_rand.NextDouble() * 120 + 2.50); // typical $2.50 to $122.50 transaction
        if (_rand.NextDouble() < 0.05) amount = (decimal)(_rand.NextDouble() * 800 + 100); // 5% high value

        string entryMode = _rand.Next(1, 4) switch
        {
            1 => "051", // EMV Chip (very common, low risk)
            2 => "071", // Contactless (low risk)
            _ => "012"  // Online/CNP (medium risk)
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
            respCode: "00", // approved
            mcc: merchant.MCC,
            country: merchant.Country,
            entryMode: entryMode,
            tid: merchant.TID,
            mid: merchant.MID,
            location: merchant.NameAndLocation,
            currency: "840", // USD
            isFraud: false,
            fraudReason: null
        );

        // Generate normal clearing
        if (_rand.NextDouble() < 0.98) // 98% settlement rate for approved txns
        {
            DateTime settleDate = txnTime.Date.AddDays(_rand.Next(1, 3));
            decimal interchange = amount * 0.015m; // 1.5% interchange fee
            
            AddClearingRow(clearings,
                clearingId: Guid.NewGuid(),
                pan: pan,
                rrn: rrnStr,
                settleAmount: amount,
                currency: "840",
                interchange: interchange,
                settleDate: settleDate,
                status: "Settled",
                recon: "Matched"
            );
        }
    }

    private int GenerateCNPVelocityPattern(DataTable auths, DataTable clearings, string pan, DateTime baseTime, ref long stan, ref long rrn)
    {
        // 5 to 8 rapid online transactions at different merchants in minutes
        int txCount = _rand.Next(5, 9);
        decimal baseAmount = (decimal)(_rand.NextDouble() * 300 + 150); // E.g., $150 to $450
        
        for (int step = 0; step < txCount; step++)
        {
            DateTime txnTime = baseTime.AddMinutes(step * _rand.Next(1, 3));
            // Find a high-risk online merchant
            var merchant = _merchantPool.Find(m => m.MCC == "5942" || m.MCC == "5816" || m.MCC == "6012") ?? _merchantPool[0];
            decimal amount = baseAmount + (step * 50); // increasing amounts

            string rrnStr = GetNextRrn(ref rrn);
            string stanStr = GetNextStan(ref stan);

            // Set response code: first few succeed, then decline on limit
            string respCode = step < 4 ? "00" : "51"; // 51 = insufficient funds/limits
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
                entryMode: "012", // CNP
                tid: merchant.TID,
                mid: merchant.MID,
                location: merchant.NameAndLocation,
                currency: "840",
                isFraud: true,
                fraudReason: "CNP Velocity Attack"
            );

            if (respCode == "00")
            {
                // Most fraud results in a chargeback in clearing
                DateTime settleDate = txnTime.Date.AddDays(_rand.Next(1, 3));
                bool isChargedback = _rand.NextDouble() < 0.80; // 80% chance of chargeback
                
                AddClearingRow(clearings,
                    clearingId: Guid.NewGuid(),
                    pan: pan,
                    rrn: rrnStr,
                    settleAmount: amount,
                    currency: "840",
                    interchange: amount * 0.015m,
                    settleDate: settleDate,
                    status: isChargedback ? "ChargedBack" : "Settled",
                    recon: "Matched"
                );
            }
        }

        return txCount;
    }

    private int GenerateImpossibleTravelPattern(DataTable auths, DataTable clearings, string pan, DateTime baseTime, ref long stan, ref long rrn)
    {
        // Two transactions: one in USA (840), next in Germany (276) or Turkey (792) 30 minutes later
        // First txn: USA
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
            entryMode: "051", // Chip
            tid: usaMerchant.TID,
            mid: usaMerchant.MID,
            location: usaMerchant.NameAndLocation,
            currency: "840",
            isFraud: true,
            fraudReason: "Impossible Travel Anomaly"
        );

        DateTime settleDate1 = baseTime.Date.AddDays(_rand.Next(1, 3));
        AddClearingRow(clearings,
            clearingId: Guid.NewGuid(),
            pan: pan,
            rrn: rrn1,
            settleAmount: amount1,
            currency: "840",
            interchange: amount1 * 0.015m,
            settleDate: settleDate1,
            status: "Settled",
            recon: "Matched"
        );

        // Second txn: Europe/Turkey, 40 minutes later
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
            currency: euroMerchant.Country == "792" ? "949" : "978", // TRY or EUR
            isFraud: true,
            fraudReason: "Impossible Travel Anomaly"
        );

        DateTime settleDate2 = travelTxnTime.Date.AddDays(_rand.Next(1, 3));
        bool isChargedback = _rand.NextDouble() < 0.70;
        AddClearingRow(clearings,
            clearingId: Guid.NewGuid(),
            pan: pan,
            rrn: rrn2,
            settleAmount: amount2,
            currency: euroMerchant.Country == "792" ? "949" : "978",
            interchange: amount2 * 0.015m,
            settleDate: settleDate2,
            status: isChargedback ? "ChargedBack" : "Settled",
            recon: "Matched"
        );

        return 2;
    }

    private int GenerateCardTestingPattern(DataTable auths, DataTable clearings, string pan, DateTime baseTime, ref long stan, ref long rrn)
    {
        // 4 quick small amount declines followed by a large success
        int declineCount = _rand.Next(3, 6);
        var onlineMerchant = _merchantPool.Find(m => m.MCC == "5816" || m.MCC == "5942") ?? _merchantPool[0];

        for (int step = 0; step < declineCount; step++)
        {
            DateTime txnTime = baseTime.AddSeconds(step * _rand.Next(10, 40));
            decimal smallAmount = (decimal)(_rand.NextDouble() * 1.50 + 0.10); // $0.10 - $1.60

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
                respCode: "05", // 05 = Do not honor (decline)
                mcc: onlineMerchant.MCC,
                country: onlineMerchant.Country,
                entryMode: "012", // CNP
                tid: onlineMerchant.TID,
                mid: onlineMerchant.MID,
                location: onlineMerchant.NameAndLocation,
                currency: "840",
                isFraud: true,
                fraudReason: "Card Testing Pattern"
            );
        }

        // The final large success
        DateTime successTime = baseTime.AddSeconds(declineCount * 30 + 10);
        decimal bigAmount = (decimal)(_rand.NextDouble() * 500 + 400); // $400 - $900
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
            respCode: "00", // approved
            mcc: onlineMerchant.MCC,
            country: onlineMerchant.Country,
            entryMode: "012",
            tid: onlineMerchant.TID,
            mid: onlineMerchant.MID,
            location: onlineMerchant.NameAndLocation,
            currency: "840",
            isFraud: true,
            fraudReason: "Card Testing Pattern"
        );

        DateTime settleDate = successTime.Date.AddDays(_rand.Next(1, 3));
        AddClearingRow(clearings,
            clearingId: Guid.NewGuid(),
            pan: pan,
            rrn: successRrn,
            settleAmount: bigAmount,
            currency: "840",
            interchange: bigAmount * 0.015m,
            settleDate: settleDate,
            status: "ChargedBack", // almost guaranteed chargeback on success card testing
            recon: "Matched"
        );

        return declineCount + 1;
    }

    private int GenerateHighRiskOffHoursPattern(DataTable auths, DataTable clearings, string pan, DateTime baseTime, ref long stan, ref long rrn)
    {
        // Transaction at high-risk MCC (7995 betting or 5944 jewelry) at 3 AM local time with high amount
        // Set local time to 3 AM by shifting baseTime
        DateTime local3Am = new(baseTime.Year, baseTime.Month, baseTime.Day, 3, _rand.Next(0, 60), _rand.Next(0, 60));
        var highRiskMerch = _merchantPool.Find(m => m.MCC == "7995" || m.MCC == "5944") ?? _merchantPool[0];

        decimal amount = (decimal)(_rand.NextDouble() * 1500 + 800); // $800 - $2300

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
            entryMode: "012", // Online/CNP
            tid: highRiskMerch.TID,
            mid: highRiskMerch.MID,
            location: highRiskMerch.NameAndLocation,
            currency: "840",
            isFraud: true,
            fraudReason: "High-Risk MCC Off-Hours Velocity"
        );

        DateTime settleDate = local3Am.Date.AddDays(_rand.Next(1, 3));
        AddClearingRow(clearings,
            clearingId: Guid.NewGuid(),
            pan: pan,
            rrn: rrnStr,
            settleAmount: amount,
            currency: "840",
            interchange: amount * 0.015m,
            settleDate: settleDate,
            status: "ChargedBack",
            recon: "Matched"
        );

        return 1;
    }

    private static void AddAuthRow(DataTable table, Guid transactionId, string mti, string pan, string procCode, 
        decimal amount, DateTime txnTime, string stan, string rrn, string? authCode, string respCode, 
        string mcc, string country, string entryMode, string tid, string mid, string location, string currency, 
        bool isFraud, string? fraudReason)
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
            "2912", // F14: Expiry Dec 2029 (YYMM)
            mcc,
            country,
            entryMode,
            rrn,
            string.IsNullOrEmpty(authCode) ? DBNull.Value : authCode,
            respCode,
            tid,
            mid,
            location,
            currency,
            isFraud,
            string.IsNullOrEmpty(fraudReason) ? DBNull.Value : fraudReason
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

    private static void AddClearingRow(DataTable table, Guid clearingId, string pan, string rrn, 
        decimal settleAmount, string currency, decimal interchange, DateTime settleDate, string status, string recon)
    {
        table.Rows.Add(
            clearingId,
            pan,
            rrn,
            settleAmount,
            currency,
            interchange,
            settleDate,
            status,
            recon
        );
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
