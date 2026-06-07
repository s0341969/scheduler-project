using Hangfire;
using Microsoft.EntityFrameworkCore;
using VulnScan.Web.Data;

namespace VulnScan.Web.Services;

public sealed class ScanScheduleService(
    ApplicationDbContext dbContext,
    IRecurringJobManager recurringJobManager) : IScanScheduleService
{
    public async Task SyncRecurringJobsAsync(CancellationToken cancellationToken = default)
    {
        var jobs = await dbContext.ScanJobs
            .AsNoTracking()
            .Where(item => item.IsEnabled && !string.IsNullOrWhiteSpace(item.CronExpression))
            .ToListAsync(cancellationToken);

        foreach (var job in jobs)
        {
            var cron = job.CronExpression!.Trim();
            recurringJobManager.AddOrUpdate<IScanJobService>(
                ScanJobRecurringId(job.JobId),
                service => service.CreateRunAsync(job.JobId, "schedule", CancellationToken.None),
                cron);
        }
    }

    public Task AddOrUpdateJobAsync(int jobId, string cronExpression, CancellationToken cancellationToken = default)
    {
        recurringJobManager.AddOrUpdate<IScanJobService>(
            ScanJobRecurringId(jobId),
            service => service.CreateRunAsync(jobId, "schedule", CancellationToken.None),
            cronExpression);
        return Task.CompletedTask;
    }

    public Task RemoveJobAsync(int jobId, CancellationToken cancellationToken = default)
    {
        recurringJobManager.RemoveIfExists(ScanJobRecurringId(jobId));
        return Task.CompletedTask;
    }

    private static string ScanJobRecurringId(int jobId) => $"scan-job-{jobId}";
}
