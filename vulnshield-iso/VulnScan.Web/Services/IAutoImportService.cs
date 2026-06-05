using VulnScan.Web.ViewModels;

namespace VulnScan.Web.Services;

public interface IAutoImportService
{
    Task EnsureFoldersAsync(CancellationToken cancellationToken = default);

    Task<int> RunOnceAsync(CancellationToken cancellationToken = default);

    Task<AutoImportIndexViewModel> BuildDashboardAsync(CancellationToken cancellationToken = default);
}
