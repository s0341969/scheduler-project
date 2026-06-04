using System.ComponentModel.DataAnnotations;

namespace VulnScan.Web.ViewModels;

public sealed class ScanJobViewModel
{
    public int JobId { get; set; }

    [Required]
    [Display(Name = "任務名稱")]
    public string JobName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "掃描目標")]
    public string TargetRange { get; set; } = string.Empty;

    [Required]
    [Display(Name = "掃描類型")]
    public string ScanType { get; set; } = "PortScan";

    [Required]
    [Display(Name = "掃描工具")]
    public string ScanTool { get; set; } = "Nmap";

    [Display(Name = "掃描強度")]
    public string? ScanProfile { get; set; } = "Normal";
}
