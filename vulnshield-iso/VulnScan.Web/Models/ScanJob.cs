using System.ComponentModel.DataAnnotations;

namespace VulnScan.Web.Models;

public sealed class ScanJob
{
    [Key]
    public int JobId { get; set; }

    [Required]
    [StringLength(100)]
    public string JobName { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string TargetRange { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string ScanType { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string ScanTool { get; set; } = string.Empty;

    [StringLength(50)]
    public string? ScanProfile { get; set; }

    [StringLength(50)]
    public string? ScheduleType { get; set; }

    public TimeOnly? ScheduleTime { get; set; }

    [StringLength(100)]
    public string? CronExpression { get; set; }

    public bool IsEnabled { get; set; } = true;

    [StringLength(50)]
    public string? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<ScanRun> ScanRuns { get; set; } = new List<ScanRun>();
}
