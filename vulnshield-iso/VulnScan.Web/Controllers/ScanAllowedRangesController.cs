using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VulnScan.Web.Data;
using VulnScan.Web.Models;
using VulnScan.Web.ViewModels;

namespace VulnScan.Web.Controllers;

[Authorize(Roles = "Admin")]
public sealed class ScanAllowedRangesController(ApplicationDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(new ScanAllowedRangesIndexViewModel
        {
            Items = await dbContext.ScanAllowedRanges
                .AsNoTracking()
                .OrderBy(item => item.RangeName)
                .ToListAsync(cancellationToken),
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ScanAllowedRange form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", new ScanAllowedRangesIndexViewModel
            {
                Form = form,
                Items = await dbContext.ScanAllowedRanges.AsNoTracking().OrderBy(item => item.RangeName).ToListAsync(cancellationToken),
            });
        }

        form.RangeName = form.RangeName.Trim();
        form.Cidr = form.Cidr.Trim();
        form.CreatedAt = DateTime.UtcNow;

        dbContext.ScanAllowedRanges.Add(form);
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["StatusMessage"] = $"已新增白名單範圍：{form.RangeName}";
        return RedirectToAction(nameof(Index));
    }
}
