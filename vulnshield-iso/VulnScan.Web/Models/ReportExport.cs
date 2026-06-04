using System.ComponentModel.DataAnnotations;

namespace VulnScan.Web.Models;

public sealed class ReportExport
{
    [Key]
    public int ExportId { get; set; }

    [Required]
    [StringLength(200)]
    public string ReportName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string ReportType { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string FilePath { get; set; } = string.Empty;

    [StringLength(50)]
    public string? ExportedBy { get; set; }

    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
}
