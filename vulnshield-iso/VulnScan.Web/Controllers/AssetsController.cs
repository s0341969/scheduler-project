using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VulnScan.Web.Data;
using VulnScan.Web.Models;
using VulnScan.Web.ViewModels;

namespace VulnScan.Web.Controllers;

[Authorize]
public sealed class AssetsController(ApplicationDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(new AssetsIndexViewModel
        {
            Items = await dbContext.Assets
                .AsNoTracking()
                .OrderBy(item => item.AssetName)
                .ToListAsync(cancellationToken),
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AssetViewModel form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", new AssetsIndexViewModel
            {
                Form = form,
                Items = await dbContext.Assets.AsNoTracking().OrderBy(item => item.AssetName).ToListAsync(cancellationToken),
            });
        }

        var asset = new Asset
        {
            AssetName = form.AssetName.Trim(),
            HostName = string.IsNullOrWhiteSpace(form.HostName) ? null : form.HostName.Trim(),
            IPAddress = form.IPAddress.Trim(),
            AssetType = string.IsNullOrWhiteSpace(form.AssetType) ? null : form.AssetType.Trim(),
            OwnerDept = string.IsNullOrWhiteSpace(form.OwnerDept) ? null : form.OwnerDept.Trim(),
            OwnerUser = string.IsNullOrWhiteSpace(form.OwnerUser) ? null : form.OwnerUser.Trim(),
            Importance = string.IsNullOrWhiteSpace(form.Importance) ? null : form.Importance.Trim(),
            IsActive = form.IsActive,
            CreatedAt = DateTime.UtcNow,
        };

        dbContext.Assets.Add(asset);
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["StatusMessage"] = $"已建立資產：{asset.AssetName}";
        return RedirectToAction(nameof(Index));
    }
}
