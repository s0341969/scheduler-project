using System.ComponentModel.DataAnnotations;

namespace VulnScan.Web.ViewModels;

public sealed class AssetViewModel
{
    public int AssetId { get; set; }

    [Required]
    [Display(Name = "資產名稱")]
    public string AssetName { get; set; } = string.Empty;

    [Display(Name = "主機名稱")]
    public string? HostName { get; set; }

    [Required]
    [Display(Name = "IP 位址")]
    public string IPAddress { get; set; } = string.Empty;

    [Display(Name = "資產類型")]
    public string? AssetType { get; set; }

    [Display(Name = "負責單位")]
    public string? OwnerDept { get; set; }

    [Display(Name = "負責人")]
    public string? OwnerUser { get; set; }

    [Display(Name = "重要性")]
    public string? Importance { get; set; }

    public bool IsActive { get; set; } = true;
}
