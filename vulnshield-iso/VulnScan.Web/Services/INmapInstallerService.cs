namespace VulnScan.Web.Services;

public interface INmapInstallerService
{
    Task<NmapInstallerResult> StartInstallAsync(string triggeredBy, CancellationToken cancellationToken = default);
}
