using System.ComponentModel.DataAnnotations;
using VulnScan.Web.Models;

namespace VulnScan.Web.ViewModels;

public sealed class GreenboneIndexViewModel
{
    public GreenboneSettingsFormViewModel Settings { get; set; } = new();

    public IReadOnlyList<GreenboneSyncLog> RecentLogs { get; set; } = Array.Empty<GreenboneSyncLog>();

    public int SuccessCount { get; set; }

    public int FailureCount { get; set; }
}

public sealed class GreenboneSettingsFormViewModel
{
    [Display(Name = "啟用同步")]
    public bool IsEnabled { get; set; }

    [Display(Name = "主機位址")]
    [Required(ErrorMessage = "請輸入 Greenbone 主機位址。")]
    [StringLength(200)]
    public string Host { get; set; } = string.Empty;

    [Display(Name = "Port")]
    [Range(1, 65535, ErrorMessage = "Port 必須介於 1 到 65535。")]
    public int Port { get; set; } = 9390;

    [Display(Name = "登入帳號")]
    [Required(ErrorMessage = "請輸入登入帳號。")]
    [StringLength(100)]
    public string Username { get; set; } = string.Empty;

    [Display(Name = "密碼")]
    [DataType(DataType.Password)]
    public string? Password { get; set; }

    [Display(Name = "忽略憑證錯誤")]
    public bool IgnoreCertificateErrors { get; set; }

    [Display(Name = "每次同步報表數")]
    [Range(1, 100, ErrorMessage = "同步報表數量請介於 1 到 100。")]
    public int SyncTopReports { get; set; } = 5;

    [Display(Name = "報表篩選條件")]
    [Required(ErrorMessage = "請輸入報表篩選條件。")]
    [StringLength(500)]
    public string ReportFilter { get; set; } = "rows=5 sort-reverse=date";

    [Display(Name = "結果篩選條件")]
    [Required(ErrorMessage = "請輸入結果篩選條件。")]
    [StringLength(500)]
    public string ResultFilter { get; set; } = "rows=-1 min_qod=0 apply_overrides=1 levels=hmlg";

    public bool HasStoredPassword { get; set; }
}
