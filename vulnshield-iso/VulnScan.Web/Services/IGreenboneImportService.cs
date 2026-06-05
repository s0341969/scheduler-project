using VulnScan.Web.ViewModels;

namespace VulnScan.Web.Services;

public interface IGreenboneImportService
{
    Task<GreenboneSyncResult> RunOnceAsync(string triggeredBy, string triggerMode, CancellationToken cancellationToken = default);
}
