using System.ComponentModel.DataAnnotations;

namespace VulnScan.Web.Models;

public sealed class GreenboneSyncLog
{
    [Key]
    public int LogId { get; set; }

    [StringLength(50)]
    public string TriggerMode { get; set; } = "Auto";

    [StringLength(50)]
    public string Status { get; set; } = "Pending";

    [StringLength(200)]
    public string Endpoint { get; set; } = string.Empty;

    [StringLength(120)]
    public string? ReportId { get; set; }

    [StringLength(200)]
    public string? TaskName { get; set; }

    public int ImportedCount { get; set; }

    public int? ScanRunId { get; set; }

    [StringLength(1000)]
    public string? Message { get; set; }

    [StringLength(100)]
    public string TriggeredBy { get; set; } = "system";

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }
}
