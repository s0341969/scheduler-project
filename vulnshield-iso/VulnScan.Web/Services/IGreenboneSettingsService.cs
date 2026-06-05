using VulnScan.Web.Models;
using VulnScan.Web.ViewModels;

namespace VulnScan.Web.Services;

public interface IGreenboneSettingsService
{
    Task<GreenboneOptions> GetEffectiveOptionsAsync(CancellationToken cancellationToken = default);

    Task<GreenboneSettingsFormViewModel> GetFormAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(GreenboneSettingsFormViewModel form, string updatedBy, CancellationToken cancellationToken = default);
}
