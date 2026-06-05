using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VulnScan.Web.Data;
using VulnScan.Web.Models;
using VulnScan.Web.Services;
using VulnScan.Web.ViewModels;

namespace VulnScan.Web.Controllers;

[Authorize]
public sealed class GreenboneController(
    ApplicationDbContext dbContext,
    IGreenboneSettingsService greenboneSettingsService,
    IGreenboneImportService greenboneImportService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(await BuildModelAsync(cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(GreenboneSettingsFormViewModel form, CancellationToken cancellationToken)
    {
        if (!form.HasStoredPassword && string.IsNullOrWhiteSpace(form.Password))
        {
            ModelState.AddModelError("Settings.Password", "首次啟用 Greenbone 設定時必須輸入密碼。");
        }

        if (!ModelState.IsValid)
        {
            var model = await BuildModelAsync(cancellationToken);
            form.HasStoredPassword = model.Settings.HasStoredPassword;
            model.Settings = form;
            return View("Index", model);
        }

        await greenboneSettingsService.SaveAsync(form, User.Identity?.Name ?? "system", cancellationToken);
        TempData["StatusMessage"] = "Greenbone 設定已更新。";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncNow(CancellationToken cancellationToken)
    {
        var result = await greenboneImportService.RunOnceAsync(User.Identity?.Name ?? "system", "Manual", cancellationToken);
        TempData["StatusMessage"] = result.ImportedCount > 0
            ? $"Greenbone 同步完成，本次匯入 {result.ImportedCount} 份新報表。"
            : "Greenbone 同步完成，目前沒有新的報表需要匯入。";
        return RedirectToAction(nameof(Index));
    }

    private async Task<GreenboneIndexViewModel> BuildModelAsync(CancellationToken cancellationToken)
    {
        var logs = await dbContext.Set<GreenboneSyncLog>()
            .AsNoTracking()
            .OrderByDescending(item => item.StartedAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        return new GreenboneIndexViewModel
        {
            Settings = await greenboneSettingsService.GetFormAsync(cancellationToken),
            RecentLogs = logs,
            SuccessCount = logs.Count(item => string.Equals(item.Status, "Completed", StringComparison.OrdinalIgnoreCase)),
            FailureCount = logs.Count(item => string.Equals(item.Status, "Failed", StringComparison.OrdinalIgnoreCase)),
        };
    }
}
