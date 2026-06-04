using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VulnScan.Web.Models;

public sealed class AssetPort
{
    [Key]
    public int PortId { get; set; }

    [Required]
    public int AssetId { get; set; }

    [ForeignKey(nameof(AssetId))]
    public Asset? Asset { get; set; }

    [Required]
    public int RunId { get; set; }

    [ForeignKey(nameof(RunId))]
    public ScanRun? ScanRun { get; set; }

    [StringLength(50)]
    public string? IPAddress { get; set; }

    public int? PortNumber { get; set; }

    [StringLength(10)]
    public string? Protocol { get; set; }

    [StringLength(100)]
    public string? ServiceName { get; set; }

    [StringLength(200)]
    public string? ServiceProduct { get; set; }

    [StringLength(200)]
    public string? ServiceVersion { get; set; }

    [StringLength(50)]
    public string? State { get; set; }

    [StringLength(500)]
    public string? RiskHint { get; set; }

    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
}
