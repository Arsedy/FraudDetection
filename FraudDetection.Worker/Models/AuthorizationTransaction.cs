using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FraudDetectionWorker.Models;

[Table("authorizationtransactions")]
public class AuthorizationTransaction
{
    [Key]
    [Column("transactionid")]
    public Guid TransactionId { get; set; }

    [Column("mti")]
    [MaxLength(4)]
    public string Mti { get; set; } = null!;

    [Column("f2_pan")]
    [MaxLength(19)]
    public string F2_PAN { get; set; } = null!;

    [Column("f3_proccode")]
    [MaxLength(6)]
    public string F3_ProcCode { get; set; } = null!;

    [Column("f4_amounttxn")]
    public decimal F4_AmountTxn { get; set; }

    [Column("f7_txndatetime")]
    public DateTime F7_TxnDateTime { get; set; }

    [Column("f11_stan")]
    [MaxLength(6)]
    public string F11_STAN { get; set; } = null!;

    [Column("f12_localtime")]
    public TimeOnly F12_LocalTime { get; set; }

    [Column("f13_localdate")]
    public DateOnly F13_LocalDate { get; set; }

    [Column("f14_expdate")]
    [MaxLength(4)]
    public string F14_ExpDate { get; set; } = null!;

    [Column("f18_mcc")]
    [MaxLength(4)]
    public string F18_MCC { get; set; } = null!;

    [Column("f19_acqcountry")]
    [MaxLength(3)]
    public string F19_AcqCountry { get; set; } = null!;

    [Column("f22_posentrymode")]
    [MaxLength(3)]
    public string F22_POSEntryMode { get; set; } = null!;

    [Column("f37_rrn")]
    [MaxLength(12)]
    public string F37_RRN { get; set; } = null!;

    [Column("f38_authcode")]
    [MaxLength(6)]
    public string? F38_AuthCode { get; set; }

    [Column("f39_responsecode")]
    [MaxLength(2)]
    public string F39_ResponseCode { get; set; } = null!;

    [Column("f41_tid")]
    [MaxLength(8)]
    public string F41_TID { get; set; } = null!;

    [Column("f42_mid")]
    [MaxLength(15)]
    public string F42_MID { get; set; } = null!;

    [Column("f43_merchantloc")]
    [MaxLength(40)]
    public string F43_MerchantLoc { get; set; } = null!;

    [Column("f49_currencycode")]
    [MaxLength(3)]
    public string F49_CurrencyCode { get; set; } = null!;
}
