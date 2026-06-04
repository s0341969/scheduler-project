using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VulnScan.Web.Data;

namespace VulnScan.Web.Controllers;

[Authorize]
public sealed class VulnerabilitiesController(ApplicationDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var items = await dbContext.Vulnerabilities
            .AsNoTracking()
            .Include(item => item.Asset)
            .OrderByDescending(item => item.LastDetectedAt)
            .ToListAsync(cancellationToken);
        return View(items);
    }
}
