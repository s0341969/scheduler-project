namespace VulnScan.Web.Services;

public interface IScanScheduleService
{
    Task SyncRecurringJobsAsync(CancellationToken cancellationToken = default);

    Task AddOrUpdateJobAsync(int jobId, string cronExpression, CancellationToken cancellationToken = default);

    Task RemoveJobAsync(int jobId, CancellationToken cancellationToken = default);
}
