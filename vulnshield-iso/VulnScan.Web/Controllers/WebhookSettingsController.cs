using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VulnScan.Web.Models;
using VulnScan.Web.Services;

namespace VulnScan.Web.Controllers;

[Authorize(Roles = "Admin")]
public sealed class WebhookSettingsController(IWebhookService webhookService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var settings = await webhookService.GetSettingsAsync();
        return View(settings);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new WebhookSetting());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(WebhookSetting model)
    {
        if (!ModelState.IsValid)
            return View(model);

        await webhookService.AddSettingAsync(model);
        TempData["StatusMessage"] = "Webhook 設定已新增。";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var setting = await webhookService.GetSettingByIdAsync(id);
        if (setting is null)
            return NotFound();

        return View(setting);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(WebhookSetting model)
    {
        if (!ModelState.IsValid)
            return View(model);

        await webhookService.UpdateSettingAsync(model);
        TempData["StatusMessage"] = "Webhook 設定已更新。";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await webhookService.DeleteSettingAsync(id);
        TempData["StatusMessage"] = "Webhook 設定已刪除。";
        return RedirectToAction(nameof(Index));
    }
}
