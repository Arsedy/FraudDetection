using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FraudDetectionWorker.Models;

[Table("lastfraudchecktime")]
public class LastFraudCheckTime
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("lastchecktime")]
    public DateTime LastCheckTime { get; set; }
}   