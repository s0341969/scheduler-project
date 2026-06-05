using VulnScan.Web.ViewModels;

namespace VulnScan.Web.Services;

public interface IGreenboneImportService
{
    Task<GreenboneSyncResult> RunOnceAsync(CancellationToken cancellationToken = default);
}
