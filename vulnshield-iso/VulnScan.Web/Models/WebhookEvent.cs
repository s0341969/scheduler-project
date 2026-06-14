namespace VulnScan.Web.Models;

public sealed class WebhookEvent
{
    public string EventType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int RunId { get; set; }
    public int JobId { get; set; }
    public string JobName { get; set; } = string.Empty;
    public string TargetRange { get; set; } = string.Empty;
    public string? ScanTool { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public int? TotalVulnerabilities { get; set; }
    public string? RunUrl { get; set; }
}
