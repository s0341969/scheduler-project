namespace AutoPcScheduler.Services;

public sealed record MachineCapacity(
    string MachineId,
    string? MachineGroup,
    string? ProcessCode,
    decimal DailyHours);

public sealed record WorkMachineRoute(
    string? PartNo,
    string? ProductName,
    string? ProcessCode,
    string? MachineId,
    string? MachineGroup);

public sealed record SchedulableWork(
    string OrdTp,
    string OrdNo,
    int OrdSq,
    int OrdSq1,
    int? OrdSq2,
    string? PartNo,
    string? ProductName,
    string? ProcessCode,
    decimal RequiredHours,
    DateTime? DueDate,
    decimal? PriorityAvailableHours);

public sealed record ExistingAssignment(
    string MachineId,
    DateTime StartTime,
    DateTime EndTime);

public sealed record PlannedAssignment(
    string OrdTp,
    string OrdNo,
    int OrdSq,
    int OrdSq1,
    int? OrdSq2,
    string? InPart,
    string? ProcessCode,
    string? ProductName,
    string MachineId,
    DateTime StartTime,
    DateTime EndTime,
    decimal WorkHours,
    string Assigner,
    string Content);

public sealed record UnscheduledWork(SchedulableWork Work, string Reason);

public sealed record SchedulingContext(
    IReadOnlyList<MachineCapacity> Machines,
    IReadOnlyList<WorkMachineRoute> Routes,
    IReadOnlyList<SchedulableWork> Works,
    IReadOnlyList<ExistingAssignment> ExistingAssignments);

public sealed record SchedulingResult(
    IReadOnlyList<PlannedAssignment> Assignments,
    IReadOnlyList<UnscheduledWork> Unscheduled);
