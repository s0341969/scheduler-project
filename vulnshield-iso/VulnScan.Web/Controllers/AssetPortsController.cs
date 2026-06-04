using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VulnScan.Web.Data;

namespace VulnScan.Web.Controllers;

[Authorize]
public sealed class AssetPortsController(ApplicationDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var items = await dbContext.AssetPorts
            .AsNoTracking()
            .Include(item => item.Asset)
            .OrderByDescending(item => item.DetectedAt)
            .ToListAsync(cancellationToken);
        return View(items);
    }
}
