namespace VulnScan.Web.Services;

public interface IDependencyScanService
{
    bool IsSupported();
    Task<int> RunScanAsync(int runId, string targetDirectory, CancellationToken cancellationToken = default);
}
