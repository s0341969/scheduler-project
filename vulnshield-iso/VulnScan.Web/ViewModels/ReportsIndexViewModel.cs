using VulnScan.Web.Models;

namespace VulnScan.Web.ViewModels;

public sealed class ReportsIndexViewModel
{
    public int VulnerabilityCount { get; set; }

    public int HighRiskCount { get; set; }

    public int ExportCount { get; set; }

    public IReadOnlyList<ReportExport> RecentExports { get; set; } = Array.Empty<ReportExport>();
}
