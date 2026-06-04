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

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var asset = await dbContext.Assets.FirstOrDefaultAsync(item => item.AssetId == id, cancellationToken);
        if (asset is null)
        {
            return NotFound();
        }

        var model = new AssetViewModel
        {
            AssetId = asset.AssetId,
            AssetName = asset.AssetName,
            HostName = asset.HostName,
            IPAddress = asset.IPAddress,
            AssetType = asset.AssetType,
            OwnerDept = asset.OwnerDept,
            OwnerUser = asset.OwnerUser,
            Importance = asset.Importance,
            IsActive = asset.IsActive,
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AssetViewModel form, CancellationToken cancellationToken)
    {
        if (id != form.AssetId)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var asset = await dbContext.Assets.FirstOrDefaultAsync(item => item.AssetId == id, cancellationToken);
        if (asset is null)
        {
            return NotFound();
        }

        asset.AssetName = form.AssetName.Trim();
        asset.HostName = string.IsNullOrWhiteSpace(form.HostName) ? null : form.HostName.Trim();
        asset.IPAddress = form.IPAddress.Trim();
        asset.AssetType = string.IsNullOrWhiteSpace(form.AssetType) ? null : form.AssetType.Trim();
        asset.OwnerDept = string.IsNullOrWhiteSpace(form.OwnerDept) ? null : form.OwnerDept.Trim();
        asset.OwnerUser = string.IsNullOrWhiteSpace(form.OwnerUser) ? null : form.OwnerUser.Trim();
        asset.Importance = string.IsNullOrWhiteSpace(form.Importance) ? null : form.Importance.Trim();
        asset.IsActive = form.IsActive;
        asset.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["StatusMessage"] = $"已更新資產：{asset.AssetName}";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var asset = await dbContext.Assets.FirstOrDefaultAsync(item => item.AssetId == id, cancellationToken);
        if (asset is null)
        {
            TempData["StatusMessage"] = "資產已不存在。";
            return RedirectToAction(nameof(Index));
        }

        dbContext.Assets.Remove(asset);
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["StatusMessage"] = $"已刪除資產：{asset.AssetName}";
        return RedirectToAction(nameof(Index));
    }
}
