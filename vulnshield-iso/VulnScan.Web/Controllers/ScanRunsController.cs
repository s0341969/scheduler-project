using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VulnScan.Web.Data;

namespace VulnScan.Web.Controllers;

[Authorize]
public sealed class ScanRunsController(ApplicationDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var items = await dbContext.ScanRuns
            .AsNoTracking()
            .Include(item => item.ScanJob)
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);
        return View(items);
    }
}
