using VulnScan.Web.Models;

namespace VulnScan.Web.ViewModels;

public sealed class VulnerabilitiesIndexViewModel
{
    public IReadOnlyList<Vulnerability> Items { get; set; } = Array.Empty<Vulnerability>();
}
