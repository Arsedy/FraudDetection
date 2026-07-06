using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FraudDetectionWorker.Models;

[Table("fraudalerts")]
public class FraudAlert
{
    [Key]
    [Column("alertid")]
    public Guid AlertId { get; set; } = Guid.NewGuid();

    [Column("transactionid")]
    public Guid? TransactionId { get; set; }

    [Column("f2_pan")]
    [MaxLength(19)]
    public string F2_PAN { get; set; } = null!;

    [Column("ruleid")]
    [MaxLength(50)]
    public string RuleId { get; set; } = null!;

    [Column("isreviewed")]
    public bool IsReviewed { get; set; } = false;

    [Column("flaggedat")]
    public DateTime FlaggedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(TransactionId))]
    public AuthorizationTransaction? Transaction { get; set; }

    [ForeignKey(nameof(RuleId))]
    public Rule? Rule { get; set; }
}
