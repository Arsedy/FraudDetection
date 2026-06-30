using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FraudDetectionWorker.Models;

[Table("clearingtransactions")]
public class ClearingTransaction
{
    [Key]
    [Column("clearingid")]
    public Guid ClearingId { get; set; }

    [Column("f2_pan")]
    [MaxLength(19)]
    public string F2_PAN { get; set; } = null!;

    [Column("f37_rrn")]
    [MaxLength(12)]
    public string F37_RRN { get; set; } = null!;

    [Column("settlementamount")]
    public decimal SettlementAmount { get; set; }

    [Column("settlementcurrency")]
    [MaxLength(3)]
    public string SettlementCurrency { get; set; } = null!;

    [Column("interchangefee")]
    public decimal InterchangeFee { get; set; }

    [Column("settlementdate")]
    public DateOnly SettlementDate { get; set; }

    [Column("clearingstatus")]
    [MaxLength(20)]
    public string ClearingStatus { get; set; } = null!;

    [Column("reconciliationstatus")]
    [MaxLength(20)]
    public string ReconciliationStatus { get; set; } = null!;
}
