using VulnScan.Web.ViewModels;

namespace VulnScan.Web.Services;

public interface ISystemCheckService
{
    Task<SystemCheckIndexViewModel> GetStatusAsync(CancellationToken cancellationToken = default);
}
