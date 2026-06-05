namespace VulnScan.Web.Models;

public sealed class GreenboneOptions
{
    public const string SectionName = "Greenbone";

    public bool Enabled { get; set; }

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 9390;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public bool IgnoreCertificateErrors { get; set; } = true;

    public int SyncTopReports { get; set; } = 5;

    public string ReportFilter { get; set; } = "rows=5 sort-reverse=date";

    public string ResultFilter { get; set; } = "rows=-1 min_qod=0 apply_overrides=1 levels=hmlg";
}
