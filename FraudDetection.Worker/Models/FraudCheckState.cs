using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FraudDetection.Worker.Models;

[Table("fraudcheckstate")]
public class FraudCheckState
{
    [Key]
    [Column("id")]
    public int Id { get; set; } = 1;

    [Column("lastprocesseddayoffset")]
    public int LastProcessedDayOffset { get; set; } = -30;

    [Column("lastprocessedtxndatetime")]
    public DateTime LastProcessedTxnDateTime { get; set; } = DateTime.UtcNow;

    [Column("updatedat")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
