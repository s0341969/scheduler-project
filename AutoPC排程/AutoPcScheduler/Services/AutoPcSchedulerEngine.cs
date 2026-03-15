using System.Globalization;

namespace AutoPcScheduler.Services;

public sealed class AutoPcSchedulerEngine
{
    private static readonly TimeOnly DayStartTime = new(8, 0);
    private const int ExtraSearchDays = 60;

    public SchedulingResult Schedule(SchedulingContext context, DateOnly planDate, int horizonDays, string assigner)
    {
        var machines = context.Machines
            .GroupBy(x => x.MachineId, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .ToList();

        var machineStates = machines.ToDictionary(
            x => x.MachineId,
            x => new MachineState(x, DayStartTime),
            StringComparer.OrdinalIgnoreCase);

        foreach (var existing in context.ExistingAssignments)
        {
            if (machineStates.TryGetValue(existing.MachineId, out var state))
            {
                state.Seed(existing);
            }
        }

        var works = context.Works
            .OrderBy(x => x.PriorityAvailableHours ?? decimal.MaxValue)
            .ThenBy(x => x.DueDate ?? DateTime.MaxValue)
            .ThenByDescending(x => x.RequiredHours)
            .ThenBy(x => x.OrdTp)
            .ThenBy(x => x.OrdNo)
            .ThenBy(x => x.OrdSq)
            .ThenBy(x => x.OrdSq1)
            .ToList();

        var machinesByProcess = machines
            .Where(x => !string.IsNullOrWhiteSpace(x.ProcessCode))
            .GroupBy(x => x.ProcessCode!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                x => x.Key,
                x => x.Select(y => y.MachineId).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                StringComparer.OrdinalIgnoreCase);

        var assignments = new List<PlannedAssignment>();
        var unscheduled = new List<UnscheduledWork>();

        foreach (var work in works)
        {
            if (work.RequiredHours <= 0)
            {
                unscheduled.Add(new UnscheduledWork(work, "工時小於等於 0。"));
                continue;
            }

            var candidateMachineIds = ResolveCandidateMachines(work, context.Routes, machinesByProcess, machineStates.Keys);
            if (candidateMachineIds.Count == 0)
            {
                unscheduled.Add(new UnscheduledWork(work, "找不到可排機台。"));
                continue;
            }

            AllocationCandidate? best = null;
            foreach (var machineId in candidateMachineIds)
            {
                if (!machineStates.TryGetValue(machineId, out var state))
                {
                    continue;
                }

                var simulatedState = state.Clone();
                var allocation = simulatedState.TryAllocate(work.RequiredHours, planDate, horizonDays + ExtraSearchDays);
                if (!allocation.Success)
                {
                    continue;
                }

                if (best is null || allocation.FinishTime < best.Allocation.FinishTime)
                {
                    best = new AllocationCandidate(machineId, allocation);
                }
            }

            if (best is null)
            {
                unscheduled.Add(new UnscheduledWork(work, "在可搜尋區間內無法找到足夠工時。"));
                continue;
            }

            var machineState = machineStates[best.MachineId];
            machineState.Commit(best.Allocation);

            foreach (var segment in best.Allocation.Segments)
            {
                assignments.Add(new PlannedAssignment(
                    work.OrdTp,
                    work.OrdNo,
                    work.OrdSq,
                    work.OrdSq1,
                    work.OrdSq2,
                    work.PartNo,
                    work.ProcessCode,
                    work.ProductName,
                    best.MachineId,
                    segment.StartTime,
                    segment.EndTime,
                    segment.Hours,
                    assigner,
                    BuildContent(work)));
            }
        }

        return new SchedulingResult(assignments, unscheduled);
    }

    private static List<string> ResolveCandidateMachines(
        SchedulableWork work,
        IReadOnlyList<WorkMachineRoute> routes,
        IReadOnlyDictionary<string, List<string>> machinesByProcess,
        IEnumerable<string> allMachineIds)
    {
        var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var matchedRoutes = routes.Where(route =>
            (!string.IsNullOrWhiteSpace(work.PartNo) && string.Equals(route.PartNo, work.PartNo, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrWhiteSpace(work.ProductName) && string.Equals(route.ProductName, work.ProductName, StringComparison.OrdinalIgnoreCase)));

        if (!string.IsNullOrWhiteSpace(work.ProcessCode))
        {
            matchedRoutes = matchedRoutes.Where(route =>
                string.IsNullOrWhiteSpace(route.ProcessCode) ||
                string.Equals(route.ProcessCode, work.ProcessCode, StringComparison.OrdinalIgnoreCase));
        }

        var matchedRouteList = matchedRoutes.ToList();

        foreach (var route in matchedRouteList)
        {
            if (!string.IsNullOrWhiteSpace(route.MachineId))
            {
                results.Add(route.MachineId);
            }
        }

        var matchedGroups = matchedRouteList
            .Where(route => !string.IsNullOrWhiteSpace(route.MachineGroup))
            .Select(route => route.MachineGroup!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (matchedGroups.Count > 0)
        {
            foreach (var route in routes)
            {
                if (!string.IsNullOrWhiteSpace(route.MachineId) &&
                    !string.IsNullOrWhiteSpace(route.MachineGroup) &&
                    matchedGroups.Contains(route.MachineGroup))
                {
                    results.Add(route.MachineId);
                }
            }
        }

        if (results.Count == 0 &&
            !string.IsNullOrWhiteSpace(work.ProcessCode) &&
            machinesByProcess.TryGetValue(work.ProcessCode, out var processMachines))
        {
            foreach (var machineId in processMachines)
            {
                results.Add(machineId);
            }
        }

        if (results.Count == 0)
        {
            foreach (var machineId in allMachineIds)
            {
                results.Add(machineId);
            }
        }

        return results.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string BuildContent(SchedulableWork work)
    {
        var process = string.IsNullOrWhiteSpace(work.ProcessCode) ? "NA" : work.ProcessCode;
        var partNo = string.IsNullOrWhiteSpace(work.PartNo) ? "NA" : work.PartNo;
        return FormattableString.Invariant($"AutoPc:{work.OrdTp}-{work.OrdNo}-{work.OrdSq}-{work.OrdSq1}|{partNo}|{process}");
    }

    private sealed record AllocationCandidate(string MachineId, AllocationResult Allocation);

    private sealed class MachineState
    {
        private readonly MachineCapacity _machine;
        private readonly TimeOnly _dayStartTime;
        private readonly Dictionary<DateOnly, MachineDayState> _dayStates;

        public MachineState(MachineCapacity machine, TimeOnly dayStartTime)
        {
            _machine = machine;
            _dayStartTime = dayStartTime;
            _dayStates = new Dictionary<DateOnly, MachineDayState>();
        }

        private MachineState(
            MachineCapacity machine,
            TimeOnly dayStartTime,
            Dictionary<DateOnly, MachineDayState> dayStates)
        {
            _machine = machine;
            _dayStartTime = dayStartTime;
            _dayStates = dayStates;
        }

        public MachineState Clone()
        {
            var snapshot = _dayStates.ToDictionary(
                x => x.Key,
                x => new MachineDayState(x.Value.UsedHours, x.Value.LatestEnd));

            return new MachineState(_machine, _dayStartTime, snapshot);
        }

        public void Seed(ExistingAssignment assignment)
        {
            if (assignment.EndTime <= assignment.StartTime)
            {
                return;
            }

            var day = DateOnly.FromDateTime(assignment.StartTime);
            var usedHours = Convert.ToDecimal((assignment.EndTime - assignment.StartTime).TotalHours, CultureInfo.InvariantCulture);
            var dayState = GetDayState(day);

            dayState.UsedHours += Math.Max(0m, usedHours);
            if (assignment.EndTime > dayState.LatestEnd)
            {
                dayState.LatestEnd = assignment.EndTime;
            }
        }

        public AllocationResult TryAllocate(decimal requiredHours, DateOnly fromDate, int searchDays)
        {
            if (_machine.DailyHours <= 0)
            {
                return AllocationResult.Fail("機台每日工時 <= 0。");
            }

            if (requiredHours <= 0)
            {
                return AllocationResult.Fail("工作工時 <= 0。");
            }

            var segments = new List<AllocationSegment>();
            var remaining = requiredHours;
            var finishTime = fromDate.ToDateTime(_dayStartTime);

            for (var offset = 0; offset < searchDays && remaining > 0; offset++)
            {
                var day = fromDate.AddDays(offset);
                var dayState = GetDayState(day);

                var dayStart = day.ToDateTime(_dayStartTime);
                var dayEnd = dayStart.AddHours(Convert.ToDouble(_machine.DailyHours, CultureInfo.InvariantCulture));

                var startByUsedHours = dayStart.AddHours(Convert.ToDouble(dayState.UsedHours, CultureInfo.InvariantCulture));
                var start = startByUsedHours > dayState.LatestEnd ? startByUsedHours : dayState.LatestEnd;

                if (start >= dayEnd)
                {
                    continue;
                }

                var availableByQuota = Math.Max(0m, _machine.DailyHours - dayState.UsedHours);
                var availableByClock = Math.Max(0m, Convert.ToDecimal((dayEnd - start).TotalHours, CultureInfo.InvariantCulture));
                var allocHours = Math.Min(remaining, Math.Min(availableByQuota, availableByClock));

                if (allocHours <= 0)
                {
                    continue;
                }

                var end = start.AddHours(Convert.ToDouble(allocHours, CultureInfo.InvariantCulture));

                dayState.UsedHours += allocHours;
                if (end > dayState.LatestEnd)
                {
                    dayState.LatestEnd = end;
                }

                segments.Add(new AllocationSegment(start, end, allocHours));
                finishTime = end;
                remaining -= allocHours;
            }

            if (remaining > 0)
            {
                return AllocationResult.Fail("工時不足");
            }

            return AllocationResult.Succeed(segments, finishTime);
        }

        public void Commit(AllocationResult allocation)
        {
            if (!allocation.Success)
            {
                return;
            }

            foreach (var segment in allocation.Segments)
            {
                var day = DateOnly.FromDateTime(segment.StartTime);
                var dayState = GetDayState(day);

                dayState.UsedHours += segment.Hours;
                if (segment.EndTime > dayState.LatestEnd)
                {
                    dayState.LatestEnd = segment.EndTime;
                }
            }
        }

        private MachineDayState GetDayState(DateOnly day)
        {
            if (!_dayStates.TryGetValue(day, out var dayState))
            {
                dayState = new MachineDayState(0m, day.ToDateTime(_dayStartTime));
                _dayStates[day] = dayState;
            }

            return dayState;
        }
    }

    private sealed class MachineDayState
    {
        public MachineDayState(decimal usedHours, DateTime latestEnd)
        {
            UsedHours = usedHours;
            LatestEnd = latestEnd;
        }

        public decimal UsedHours { get; set; }

        public DateTime LatestEnd { get; set; }
    }

    private sealed record AllocationSegment(DateTime StartTime, DateTime EndTime, decimal Hours);

    private sealed record AllocationResult(bool Success, IReadOnlyList<AllocationSegment> Segments, DateTime FinishTime, string? Reason)
    {
        public static AllocationResult Succeed(IReadOnlyList<AllocationSegment> segments, DateTime finishTime) =>
            new(true, segments, finishTime, null);

        public static AllocationResult Fail(string reason) =>
            new(false, Array.Empty<AllocationSegment>(), DateTime.MaxValue, reason);
    }
}


