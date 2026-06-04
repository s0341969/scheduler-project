using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VulnScan.Web.Models;

public sealed class ScanRun
{
    [Key]
    public int RunId { get; set; }

    [Required]
    public int JobId { get; set; }

    [ForeignKey(nameof(JobId))]
    public ScanJob? ScanJob { get; set; }

    [Required]
    public DateTime StartTime { get; set; } = DateTime.UtcNow;

    public DateTime? EndTime { get; set; }

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Pending";

    public int TotalHosts { get; set; }

    public int TotalOpenPorts { get; set; }

    public int TotalVulnerabilities { get; set; }

    public string? ErrorMessage { get; set; }

    [StringLength(500)]
    public string? RawResultPath { get; set; }

    [StringLength(50)]
    public string? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<AssetPort> AssetPorts { get; set; } = new List<AssetPort>();

    public ICollection<Vulnerability> Vulnerabilities { get; set; } = new List<Vulnerability>();
}
