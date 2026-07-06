using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FraudDetectionWorker.Models;

[Table("rules")]
public class Rule
{
    [Key]
    [Column("ruleid")]
    [MaxLength(50)]
    public string RuleId { get; set; } = null!;

    [Column("rulename")]
    [MaxLength(100)]
    public string RuleName { get; set; } = null!;

    [Column("ruledescription")]
    [MaxLength(500)]
    public string RuleDescription { get; set; } = null!;
}
