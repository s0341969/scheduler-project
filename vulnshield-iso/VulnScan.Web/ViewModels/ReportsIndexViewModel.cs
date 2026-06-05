using VulnScan.Web.Models;

namespace VulnScan.Web.ViewModels;

public sealed class ReportsIndexViewModel
{
    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int VulnerabilityCount { get; set; }

    public int HighRiskCount { get; set; }

    public int ExportCount { get; set; }

    public IReadOnlyList<ReportExport> RecentExports { get; set; } = Array.Empty<ReportExport>();

    public IReadOnlyList<Vulnerability> RecentFindings { get; set; } = Array.Empty<Vulnerability>();
}
