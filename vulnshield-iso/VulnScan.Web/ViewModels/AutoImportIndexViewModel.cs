namespace VulnScan.Web.ViewModels;

public sealed class AutoImportIndexViewModel
{
    public bool Enabled { get; set; }

    public int PollIntervalSeconds { get; set; }

    public IReadOnlyList<AutoImportSourceViewModel> Sources { get; set; } = Array.Empty<AutoImportSourceViewModel>();

    public IReadOnlyList<AutoImportRunViewModel> RecentRuns { get; set; } = Array.Empty<AutoImportRunViewModel>();

    public IReadOnlyList<AutoImportFileViewModel> RecentProcessedFiles { get; set; } = Array.Empty<AutoImportFileViewModel>();

    public IReadOnlyList<AutoImportFileViewModel> RecentFailedFiles { get; set; } = Array.Empty<AutoImportFileViewModel>();
}

public sealed class AutoImportSourceViewModel
{
    public string SourceName { get; set; } = string.Empty;

    public string IncomingPath { get; set; } = string.Empty;

    public int PendingFileCount { get; set; }

    public string SampleExtensions { get; set; } = string.Empty;
}

public sealed class AutoImportRunViewModel
{
    public int RunId { get; set; }

    public string ScanTool { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public int TotalVulnerabilities { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }
}

public sealed class AutoImportFileViewModel
{
    public string FileName { get; set; } = string.Empty;

    public string FullPath { get; set; } = string.Empty;

    public DateTime LastWriteTime { get; set; }
}
