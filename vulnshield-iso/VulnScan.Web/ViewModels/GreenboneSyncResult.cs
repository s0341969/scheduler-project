namespace VulnScan.Web.ViewModels;

public sealed class GreenboneReportSummary
{
    public string ReportId { get; set; } = string.Empty;

    public string TaskName { get; set; } = string.Empty;

    public string ScanStatus { get; set; } = string.Empty;

    public DateTime? ScanDate { get; set; }

    public string Severity { get; set; } = string.Empty;

    public int ImportedCount { get; set; }
}

public sealed class GreenboneSyncResult
{
    public int ImportedCount { get; set; }

    public IReadOnlyList<GreenboneReportSummary> ImportedReports { get; set; } = Array.Empty<GreenboneReportSummary>();
}
