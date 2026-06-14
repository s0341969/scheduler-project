using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VulnScan.Web.Data;

namespace VulnScan.Web.Controllers.Api;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public sealed class ScanJobsApiController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpGet("running")]
    public async Task<IActionResult> GetRunningScans()
    {
        var runs = await dbContext.ScanRuns
            .Include(r => r.ScanJob)
            .Where(r => r.Status == "Pending" || r.Status == "Running")
            .Select(r => new
            {
                r.RunId,
                JobName = r.ScanJob != null ? r.ScanJob.JobName : null,
                r.Status,
                r.StartTime,
                r.CreatedBy
            })
            .ToListAsync();

        return Ok(runs);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = dbContext.ScanJobs.Include(j => j.ScanRuns).AsQueryable();
        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(j => j.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(j => new
            {
                j.JobId,
                j.JobName,
                j.TargetRange,
                j.ScanTool,
                j.ScanProfile,
                j.IsEnabled,
                j.CreatedAt,
                RunCount = j.ScanRuns.Count
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }
}
