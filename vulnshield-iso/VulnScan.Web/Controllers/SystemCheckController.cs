using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VulnScan.Web.Services;

namespace VulnScan.Web.Controllers;

[Authorize]
public sealed class SystemCheckController(
    ISystemCheckService systemCheckService,
    INmapInstallerService nmapInstallerService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "系統檢查";
        return View(await systemCheckService.GetStatusAsync(cancellationToken));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,SecurityManager")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> InstallNmap(string? returnUrl, CancellationToken cancellationToken)
    {
        try
        {
            var result = await nmapInstallerService.StartInstallAsync(User.Identity?.Name ?? "system", cancellationToken);
            TempData["StatusMessage"] = result.Message;
        }
        catch (Exception exception)
        {
            TempData["ErrorMessage"] = exception.Message;
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }
}
