using System.ComponentModel.DataAnnotations;

namespace VulnScan.Web.Models;

public sealed class GreenboneIntegrationSetting
{
    [Key]
    public int SettingId { get; set; }

    [Required]
    [StringLength(200)]
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 9390;

    [Required]
    [StringLength(100)]
    public string Username { get; set; } = string.Empty;

    public string? ProtectedPassword { get; set; }

    public bool IgnoreCertificateErrors { get; set; }

    public int SyncTopReports { get; set; } = 5;

    [StringLength(500)]
    public string ReportFilter { get; set; } = "rows=5 sort-reverse=date";

    [StringLength(500)]
    public string ResultFilter { get; set; } = "rows=-1 min_qod=0 apply_overrides=1 levels=hmlg";

    public bool IsEnabled { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(100)]
    public string? UpdatedBy { get; set; }
}
