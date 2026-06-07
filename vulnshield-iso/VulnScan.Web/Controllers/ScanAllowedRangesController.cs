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
    private const int PageSize = 20;

    public async Task<IActionResult> Index(string? search, CancellationToken cancellationToken, int page = 1)
    {
        var query = dbContext.ScanAllowedRanges.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(item =>
                item.RangeName.Contains(term) ||
                item.Cidr.Contains(term) ||
                (item.Description != null && item.Description.Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)PageSize));
        page = Math.Clamp(page, 1, totalPages);

        return View(new ScanAllowedRangesIndexViewModel
        {
            Items = await query
                .OrderBy(item => item.RangeName)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync(cancellationToken),
            SearchTerm = search,
            Page = page,
            TotalPages = totalPages,
            TotalCount = totalCount,
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

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var range = await dbContext.ScanAllowedRanges.FirstOrDefaultAsync(item => item.RangeId == id, cancellationToken);
        if (range is null)
        {
            return NotFound();
        }

        return View(range);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ScanAllowedRange form, CancellationToken cancellationToken)
    {
        if (id != form.RangeId)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var range = await dbContext.ScanAllowedRanges.FirstOrDefaultAsync(item => item.RangeId == id, cancellationToken);
        if (range is null)
        {
            return NotFound();
        }

        range.RangeName = form.RangeName.Trim();
        range.Cidr = form.Cidr.Trim();
        range.Description = string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim();
        range.IsEnabled = form.IsEnabled;
        range.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["StatusMessage"] = $"已更新白名單範圍：{range.RangeName}";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var range = await dbContext.ScanAllowedRanges.FirstOrDefaultAsync(item => item.RangeId == id, cancellationToken);
        if (range is null)
        {
            TempData["StatusMessage"] = "白名單範圍已不存在。";
            return RedirectToAction(nameof(Index));
        }

        dbContext.ScanAllowedRanges.Remove(range);
        await dbContext.SaveChangesAsync(cancellationToken);
        TempData["StatusMessage"] = $"已刪除白名單範圍：{range.RangeName}";
        return RedirectToAction(nameof(Index));
    }
}
