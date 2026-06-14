using System.ComponentModel.DataAnnotations;

namespace VulnScan.Web.ViewModels;

public sealed class ScanJobViewModel
{
    public int JobId { get; set; }

    [Required]
    [Display(Name = "任務名稱")]
    public string JobName { get; set; } = string.Empty;

    [Display(Name = "掃描目標（自動從選取資產產生）")]
    public string TargetRange { get; set; } = string.Empty;

    [Display(Name = "選取資產")]
    public List<int> SelectedAssetIds { get; set; } = new();

    [Required]
    [Display(Name = "掃描類型")]
    public string ScanType { get; set; } = "PortScan";

    [Required]
    [Display(Name = "掃描工具")]
    public string ScanTool { get; set; } = "Nmap";

    [Display(Name = "掃描強度 / 範本")]
    public string? ScanProfile { get; set; } = "Standard";

    [Display(Name = "排程類型")]
    public string? ScheduleType { get; set; }

    [Display(Name = "排程時間")]
    public TimeOnly? ScheduleTime { get; set; }

    [Display(Name = "Cron 表達式")]
    public string? CronExpression { get; set; }

    public bool IsEnabled { get; set; } = true;
}
