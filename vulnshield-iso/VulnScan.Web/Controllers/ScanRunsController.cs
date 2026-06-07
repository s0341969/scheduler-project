using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VulnScan.Web.Data;
using VulnScan.Web.Models;
using VulnScan.Web.ViewModels;

namespace VulnScan.Web.Controllers;

[Authorize]
public sealed class ScanRunsController(ApplicationDbContext dbContext) : Controller
{
    private const int PageSize = 20;

    public async Task<IActionResult> Index(string? search, string? status, CancellationToken cancellationToken, int page = 1)
    {
        IQueryable<ScanRun> query = dbContext.ScanRuns
            .AsNoTracking()
            .Include(item => item.ScanJob);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(item =>
                (item.ScanJob != null && item.ScanJob.JobName.Contains(term)) ||
                (item.ErrorMessage != null && item.ErrorMessage.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(status) && status != "All")
        {
            query = query.Where(item => item.Status == status);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)PageSize));
        page = Math.Clamp(page, 1, totalPages);

        var items = await query
            .OrderByDescending(item => item.CreatedAt)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync(cancellationToken);

        var model = new ScanRunsIndexViewModel
        {
            Items = items,
            SearchTerm = search,
            StatusFilter = status,
            Page = page,
            TotalPages = totalPages,
            TotalCount = totalCount,
        };

        return View(model);
    }
}
