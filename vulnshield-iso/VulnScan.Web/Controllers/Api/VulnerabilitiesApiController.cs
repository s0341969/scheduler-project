using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VulnScan.Web.Data;

namespace VulnScan.Web.Controllers.Api;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public sealed class VulnerabilitiesApiController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? severity = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = dbContext.Vulnerabilities
            .Include(v => v.Asset)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(severity))
            query = query.Where(v => v.Severity == severity);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(v => v.Status == status);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(v => v.FirstDetectedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => new
            {
                v.VulnId,
                AssetName = v.Asset != null ? v.Asset.AssetName : null,
                AssetIp = v.Asset != null ? v.Asset.IPAddress : null,
                v.VulnName,
                v.Severity,
                v.Status,
                PortNumber = v.PortNumber,
                v.Protocol,
                v.FirstDetectedAt
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var groups = await dbContext.Vulnerabilities
            .GroupBy(v => v.Severity)
            .Select(g => new { Severity = g.Key, Count = g.Count() })
            .ToListAsync();

        return Ok(groups);
    }
}
