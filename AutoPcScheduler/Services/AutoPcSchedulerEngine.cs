using System.Globalization;

namespace AutoPcScheduler.Services;

public sealed class AutoPcSchedulerEngine
{
    private static readonly TimeOnly DayStartTime = new(8, 0);
    private const int ExtraSearchDays = 60;
    private const string WorkfixMachineType = "1";

    public SchedulingResult Schedule(
        SchedulingContext context,
        DateOnly planDate,
        int horizonDays,
        string assigner,
        IProgress<SchedulingProgress>? progress = null)
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

        var machinesByGroup = machines
            .Where(x => !string.IsNullOrWhiteSpace(x.MachineGroup))
            .GroupBy(x => x.MachineGroup!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                x => x.Key,
                x => x.Select(y => y.MachineId).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                StringComparer.OrdinalIgnoreCase);

        var planStartTime = planDate.ToDateTime(DayStartTime);

        var assignments = new List<PlannedAssignment>();
        var unscheduled = new List<UnscheduledWork>();
        var totalWorks = works.Count;
        var processedWorks = 0;
        var scheduledWorks = 0;
        var unscheduledWorks = 0;

        progress?.Report(BuildProgress(totalWorks, processedWorks, scheduledWorks, unscheduledWorks, null));

        foreach (var work in works)
        {
            if (work.RequiredHours <= 0)
            {
                unscheduled.Add(new UnscheduledWork(work, "工時小於等於 0。"));
                processedWorks++;
                unscheduledWorks++;
                progress?.Report(BuildProgress(totalWorks, processedWorks, scheduledWorks, unscheduledWorks, work));
                continue;
            }

            var routeSelection = ResolveCandidateMachines(work, context.Routes, machinesByGroup);
            if (routeSelection.MachineIds.Count == 0)
            {
                unscheduled.Add(new UnscheduledWork(work, "找不到 WORKFIXM 對應機台（需 MTYPE=1 且符合 INDWG/PRDNAME）。"));
                processedWorks++;
                unscheduledWorks++;
                progress?.Report(BuildProgress(totalWorks, processedWorks, scheduledWorks, unscheduledWorks, work));
                continue;
            }

            AllocationCandidate? best = null;
            foreach (var machineId in routeSelection.MachineIds)
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

                var loadScore = state.GetLoadScore(planStartTime);
                if (IsBetterCandidate(routeSelection.Mode, best, machineId, loadScore, allocation.FinishTime))
                {
                    best = new AllocationCandidate(machineId, allocation, loadScore);
                }
            }

            if (best is null)
            {
                unscheduled.Add(new UnscheduledWork(work, "在可搜尋區間內無法找到足夠工時。"));
                processedWorks++;
                unscheduledWorks++;
                progress?.Report(BuildProgress(totalWorks, processedWorks, scheduledWorks, unscheduledWorks, work));
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

            processedWorks++;
            scheduledWorks++;
            progress?.Report(BuildProgress(totalWorks, processedWorks, scheduledWorks, unscheduledWorks, work));
        }

        return new SchedulingResult(assignments, unscheduled);
    }

    private static SchedulingProgress BuildProgress(
        int totalWorks,
        int processedWorks,
        int scheduledWorks,
        int unscheduledWorks,
        SchedulableWork? currentWork)
    {
        return new SchedulingProgress(
            totalWorks,
            processedWorks,
            scheduledWorks,
            unscheduledWorks,
            currentWork is null ? null : BuildOrderNo(currentWork),
            currentWork?.ProcessCode,
            currentWork?.PartNo);
    }

    private static bool IsBetterCandidate(
        MachineSelectionMode mode,
        AllocationCandidate? currentBest,
        string machineId,
        decimal loadScore,
        DateTime finishTime)
    {
        if (currentBest is null)
        {
            return true;
        }

        if (mode == MachineSelectionMode.GroupLeastLoad)
        {
            if (loadScore != currentBest.LoadScore)
            {
                return loadScore < currentBest.LoadScore;
            }

            if (finishTime != currentBest.Allocation.FinishTime)
            {
                return finishTime < currentBest.Allocation.FinishTime;
            }

            return string.Compare(machineId, currentBest.MachineId, StringComparison.OrdinalIgnoreCase) < 0;
        }

        if (finishTime != currentBest.Allocation.FinishTime)
        {
            return finishTime < currentBest.Allocation.FinishTime;
        }

        if (loadScore != currentBest.LoadScore)
        {
            return loadScore < currentBest.LoadScore;
        }

        return string.Compare(machineId, currentBest.MachineId, StringComparison.OrdinalIgnoreCase) < 0;
    }

    private static RouteSelection ResolveCandidateMachines(
        SchedulableWork work,
        IReadOnlyList<WorkMachineRoute> routes,
        IReadOnlyDictionary<string, List<string>> machinesByGroup)
    {
        var matchedRoutes = ResolveMatchedRoutes(work, routes);

        var fixedMachines = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var route in matchedRoutes)
        {
            if (!string.IsNullOrWhiteSpace(route.MachineId))
            {
                fixedMachines.Add(route.MachineId.Trim());
            }
        }

        if (fixedMachines.Count > 0)
        {
            return new RouteSelection(
                MachineSelectionMode.FixedMachine,
                fixedMachines.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList());
        }

        var groupMachines = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var route in matchedRoutes)
        {
            if (string.IsNullOrWhiteSpace(route.MachineGroup))
            {
                continue;
            }

            var groupName = route.MachineGroup.Trim();
            if (!machinesByGroup.TryGetValue(groupName, out var machineIds))
            {
                continue;
            }

            foreach (var machineId in machineIds)
            {
                groupMachines.Add(machineId);
            }
        }

        if (groupMachines.Count > 0)
        {
            return new RouteSelection(
                MachineSelectionMode.GroupLeastLoad,
                groupMachines.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList());
        }

        return new RouteSelection(MachineSelectionMode.FixedMachine, Array.Empty<string>());
    }

    private static List<WorkMachineRoute> ResolveMatchedRoutes(SchedulableWork work, IReadOnlyList<WorkMachineRoute> routes)
    {
        var workfixRoutes = routes
            .Where(route => string.Equals(route.ProcessCode?.Trim(), WorkfixMachineType, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (!string.IsNullOrWhiteSpace(work.PartNo))
        {
            var byPart = workfixRoutes
                .Where(route => !string.IsNullOrWhiteSpace(route.PartNo)
                    && string.Equals(route.PartNo.Trim(), work.PartNo.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (byPart.Count > 0)
            {
                return byPart;
            }
        }

        if (!string.IsNullOrWhiteSpace(work.ProductName))
        {
            var byProduct = workfixRoutes
                .Where(route => !string.IsNullOrWhiteSpace(route.ProductName)
                    && string.Equals(route.ProductName.Trim(), work.ProductName.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (byProduct.Count > 0)
            {
                return byProduct;
            }
        }

        return [];
    }

    private static string BuildOrderNo(SchedulableWork work)
    {
        return FormattableString.Invariant($"{work.OrdTp}-{work.OrdNo}-{work.OrdSq}-{work.OrdSq1}");
    }

    private static string BuildContent(SchedulableWork work)
    {
        var process = string.IsNullOrWhiteSpace(work.ProcessCode) ? "NA" : work.ProcessCode;
        var partNo = string.IsNullOrWhiteSpace(work.PartNo) ? "NA" : work.PartNo;
        return FormattableString.Invariant($"AutoPc:{work.OrdTp}-{work.OrdNo}-{work.OrdSq}-{work.OrdSq1}|{partNo}|{process}");
    }

    private enum MachineSelectionMode
    {
        FixedMachine,
        GroupLeastLoad
    }

    private sealed record RouteSelection(MachineSelectionMode Mode, IReadOnlyList<string> MachineIds);

    private sealed record AllocationCandidate(string MachineId, AllocationResult Allocation, decimal LoadScore);

    private sealed class MachineState
    {
        private readonly MachineCapacity _machine;
        private readonly TimeOnly _dayStartTime;
        private readonly Dictionary<DateOnly, MachineDayState> _dayStates;
        private DateTime _latestEndTime;

        public MachineState(MachineCapacity machine, TimeOnly dayStartTime)
        {
            _machine = machine;
            _dayStartTime = dayStartTime;
            _dayStates = new Dictionary<DateOnly, MachineDayState>();
            _latestEndTime = DateTime.MinValue;
        }

        private MachineState(
            MachineCapacity machine,
            TimeOnly dayStartTime,
            Dictionary<DateOnly, MachineDayState> dayStates,
            DateTime latestEndTime)
        {
            _machine = machine;
            _dayStartTime = dayStartTime;
            _dayStates = dayStates;
            _latestEndTime = latestEndTime;
        }

        public MachineState Clone()
        {
            var snapshot = _dayStates.ToDictionary(
                x => x.Key,
                x => new MachineDayState(x.Value.UsedHours, x.Value.LatestEnd));

            return new MachineState(_machine, _dayStartTime, snapshot, _latestEndTime);
        }

        public void Seed(ExistingAssignment assignment)
        {
            if (assignment.EndTime < assignment.StartTime)
            {
                return;
            }

            var day = DateOnly.FromDateTime(assignment.StartTime);
            var dayState = GetDayState(day);

            if (assignment.EndTime > assignment.StartTime)
            {
                var usedHours = Convert.ToDecimal((assignment.EndTime - assignment.StartTime).TotalHours, CultureInfo.InvariantCulture);
                dayState.UsedHours += Math.Max(0m, usedHours);
            }

            if (assignment.EndTime > dayState.LatestEnd)
            {
                dayState.LatestEnd = assignment.EndTime;
            }

            if (assignment.EndTime > _latestEndTime)
            {
                _latestEndTime = assignment.EndTime;
            }
        }

        public decimal GetLoadScore(DateTime baselineStart)
        {
            if (_latestEndTime <= baselineStart)
            {
                return 0m;
            }

            return Convert.ToDecimal((_latestEndTime - baselineStart).TotalHours, CultureInfo.InvariantCulture);
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

            var searchStart = fromDate.ToDateTime(_dayStartTime);
            if (_latestEndTime > searchStart)
            {
                searchStart = _latestEndTime;
            }

            EnsureAnchor(searchStart);

            var searchStartDate = DateOnly.FromDateTime(searchStart);
            var firstDay = searchStartDate;
            var segments = new List<AllocationSegment>();
            var remaining = requiredHours;
            var finishTime = searchStart;

            for (var offset = 0; offset < searchDays && remaining > 0; offset++)
            {
                var day = searchStartDate.AddDays(offset);
                var dayState = GetDayState(day);

                var dayStart = day.ToDateTime(_dayStartTime);
                var dayEnd = dayStart.AddHours(Convert.ToDouble(_machine.DailyHours, CultureInfo.InvariantCulture));

                var startByUsedHours = dayStart.AddHours(Convert.ToDouble(dayState.UsedHours, CultureInfo.InvariantCulture));
                var start = startByUsedHours > dayState.LatestEnd ? startByUsedHours : dayState.LatestEnd;

                if (day == firstDay && searchStart > start)
                {
                    start = searchStart;
                }

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

                if (segment.EndTime > _latestEndTime)
                {
                    _latestEndTime = segment.EndTime;
                }
            }
        }

        private void EnsureAnchor(DateTime anchorTime)
        {
            var day = DateOnly.FromDateTime(anchorTime);
            var dayState = GetDayState(day);

            if (anchorTime > dayState.LatestEnd)
            {
                dayState.LatestEnd = anchorTime;
            }

            var dayStart = day.ToDateTime(_dayStartTime);
            var elapsed = Math.Max(0m, Convert.ToDecimal((anchorTime - dayStart).TotalHours, CultureInfo.InvariantCulture));
            var anchoredUsedHours = Math.Min(_machine.DailyHours, elapsed);
            if (anchoredUsedHours > dayState.UsedHours)
            {
                dayState.UsedHours = anchoredUsedHours;
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

