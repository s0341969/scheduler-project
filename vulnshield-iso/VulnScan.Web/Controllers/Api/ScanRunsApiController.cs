using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VulnScan.Web.Data;

namespace VulnScan.Web.Controllers.Api;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public sealed class ScanRunsApiController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = dbContext.ScanRuns
            .Include(r => r.ScanJob)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(r => r.Status == status);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.StartTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new
            {
                r.RunId,
                JobName = r.ScanJob != null ? r.ScanJob.JobName : null,
                TargetRange = r.ScanJob != null ? r.ScanJob.TargetRange : null,
                r.Status,
                r.StartTime,
                r.EndTime,
                r.TotalVulnerabilities,
                r.ErrorMessage,
                r.CreatedBy
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }
}
