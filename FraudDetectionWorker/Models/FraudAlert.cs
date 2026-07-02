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
    public Guid TransactionId { get; set; }

    [Column("f2_pan")]
    [MaxLength(19)]
    public string F2_PAN { get; set; } = null!;

    [Column("userid")]
    [MaxLength(50)]
    public string? UserId { get; set; }

    [Column("rulename")]
    [MaxLength(50)]
    public string RuleName { get; set; } = null!;

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "Open"; // Open, FalsePositive, ConfirmedFraud

    [Column("flaggedat")]
    public DateTime FlaggedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(TransactionId))]
    public AuthorizationTransaction? Transaction { get; set; }
}
