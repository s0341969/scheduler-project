using System.ComponentModel.DataAnnotations;

namespace VulnScan.Web.Models;

public sealed class AuditLog
{
    [Key]
    public int LogId { get; set; }

    [StringLength(50)]
    public string? UserAccount { get; set; }

    [Required]
    [StringLength(100)]
    public string ActionType { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string TargetType { get; set; } = string.Empty;

    public int? TargetId { get; set; }

    [StringLength(50)]
    public string? SourceIPAddress { get; set; }

    public string? LogMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
