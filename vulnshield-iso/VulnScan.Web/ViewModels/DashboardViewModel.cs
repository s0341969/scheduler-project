namespace VulnScan.Web.ViewModels;

public sealed class DashboardViewModel
{
    public int AssetCount { get; set; }

    public int HighRiskVulnerabilityCount { get; set; }

    public int OpenVulnerabilityCount { get; set; }

    public int OverdueVulnerabilityCount { get; set; }

    public int AllowedRangeCount { get; set; }

    public int ScanJobCount { get; set; }

    public int RunningScanCount { get; set; }

    public DateTime? LastScanTime { get; set; }

    public IReadOnlyList<DashboardRecentRunViewModel> RecentRuns { get; set; } = Array.Empty<DashboardRecentRunViewModel>();

    public IReadOnlyList<DashboardRecentVulnerabilityViewModel> RecentVulnerabilities { get; set; } = Array.Empty<DashboardRecentVulnerabilityViewModel>();
}

public sealed class DashboardRecentRunViewModel
{
    public int RunId { get; set; }

    public string JobName { get; set; } = string.Empty;

    public string TargetRange { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }
}

public sealed class DashboardRecentVulnerabilityViewModel
{
    public int VulnId { get; set; }

    public string AssetName { get; set; } = string.Empty;

    public string VulnName { get; set; } = string.Empty;

    public string Severity { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
}
