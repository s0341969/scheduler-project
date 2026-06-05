namespace VulnScan.Web.Services;

public interface IScanJobService
{
    Task<int> CreateRunAsync(int jobId, string userAccount, CancellationToken cancellationToken = default);

    Task RunScanAsync(int runId, CancellationToken cancellationToken = default);

    NmapInstallationStatus GetNmapInstallationStatus();
}
