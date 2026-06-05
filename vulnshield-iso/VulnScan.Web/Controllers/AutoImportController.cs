using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VulnScan.Web.Services;

namespace VulnScan.Web.Controllers;

[Authorize]
public sealed class AutoImportController(IAutoImportService autoImportService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(await autoImportService.BuildDashboardAsync(cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RunNow(CancellationToken cancellationToken)
    {
        var importedCount = await autoImportService.RunOnceAsync(cancellationToken);
        TempData["StatusMessage"] = importedCount > 0
            ? $"自動匯入完成，本次同步 {importedCount} 個來源項目。"
            : "自動匯入完成，目前沒有待處理檔案或可同步的 Greenbone 新報表。";
        return RedirectToAction(nameof(Index));
    }
}
