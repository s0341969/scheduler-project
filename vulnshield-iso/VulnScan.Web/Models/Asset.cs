using System.ComponentModel.DataAnnotations;

namespace VulnScan.Web.Models;

public sealed class Asset
{
    [Key]
    public int AssetId { get; set; }

    [Required]
    [StringLength(100)]
    public string AssetName { get; set; } = string.Empty;

    [StringLength(100)]
    public string? HostName { get; set; }

    [Required]
    [StringLength(50)]
    public string IPAddress { get; set; } = string.Empty;

    [StringLength(50)]
    public string? AssetType { get; set; }

    [StringLength(200)]
    public string? OSInfo { get; set; }

    [StringLength(100)]
    public string? DeviceBrand { get; set; }

    [StringLength(100)]
    public string? DeviceModel { get; set; }

    [StringLength(100)]
    public string? OwnerDept { get; set; }

    [StringLength(100)]
    public string? OwnerUser { get; set; }

    [StringLength(20)]
    public string? Importance { get; set; }

    [StringLength(50)]
    public string? NetworkZone { get; set; }

    public bool IsExternalFacing { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? LastScanTime { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<AssetPort> AssetPorts { get; set; } = new List<AssetPort>();

    public ICollection<Vulnerability> Vulnerabilities { get; set; } = new List<Vulnerability>();

    public ICollection<ScanJobAsset> ScanJobAssets { get; set; } = new List<ScanJobAsset>();
}
