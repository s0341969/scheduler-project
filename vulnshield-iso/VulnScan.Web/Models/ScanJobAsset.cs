using System.ComponentModel.DataAnnotations;

namespace VulnScan.Web.Models;

public sealed class ScanJobAsset
{
    [Key]
    public int ScanJobAssetId { get; set; }

    public int ScanJobId { get; set; }

    public ScanJob ScanJob { get; set; } = null!;

    public int AssetId { get; set; }

    public Asset Asset { get; set; } = null!;
}
