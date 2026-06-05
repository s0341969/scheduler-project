namespace VulnScan.Web.Services;

public interface INmapService
{
    NmapInstallationStatus GetInstallationStatus();

    Task<string> RunNmapAsync(string target, string outputPath, string scanProfile, CancellationToken cancellationToken = default);
}
