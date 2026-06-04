using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VulnScan.Web.Data;

namespace VulnScan.Web.Controllers;

[Authorize(Roles = "Admin,Auditor")]
public sealed class AuditLogsController(ApplicationDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var items = await dbContext.AuditLogs
            .AsNoTracking()
            .OrderByDescending(item => item.CreatedAt)
            .Take(500)
            .ToListAsync(cancellationToken);
        return View(items);
    }
}
