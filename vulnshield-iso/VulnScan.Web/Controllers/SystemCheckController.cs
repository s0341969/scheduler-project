using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VulnScan.Web.Services;

namespace VulnScan.Web.Controllers;

[Authorize]
public sealed class SystemCheckController(ISystemCheckService systemCheckService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "系統檢查";
        return View(await systemCheckService.GetStatusAsync(cancellationToken));
    }
}
