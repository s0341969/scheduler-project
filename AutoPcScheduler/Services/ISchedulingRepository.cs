namespace AutoPcScheduler.Services;

public interface ISchedulingRepository
{
    Task<SchedulingContext> LoadSchedulingContextAsync(DateOnly planDate, int horizonDays, CancellationToken cancellationToken);

    Task<int> SaveAssignmentsAsync(IReadOnlyList<PlannedAssignment> assignments, CancellationToken cancellationToken);
}
