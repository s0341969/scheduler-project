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
            ? $"自動匯入完成，本次處理 {importedCount} 個檔案。"
            : "自動匯入完成，目前沒有待處理檔案。";
        return RedirectToAction(nameof(Index));
    }
}
