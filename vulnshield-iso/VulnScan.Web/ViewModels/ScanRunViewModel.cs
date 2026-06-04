namespace VulnScan.Web.ViewModels;

public sealed class ScanRunViewModel
{
    public int RunId { get; set; }

    public int JobId { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public int TotalHosts { get; set; }

    public int TotalOpenPorts { get; set; }

    public int TotalVulnerabilities { get; set; }

    public string? ErrorMessage { get; set; }
}
