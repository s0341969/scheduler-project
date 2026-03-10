using SchedulerConfigProvider;

namespace SchedulerDragDrop;

public static class SchedulerEngine
{
    // 每台機台每日自 08:00 起可工作 20 小時（至隔日 04:00），週日停工。
    public const int WorkStartHour = 8;
    public const int WorkingHoursPerDay = 20;
    public const int WorkEndHour = (WorkStartHour + WorkingHoursPerDay) % 24;

    public static List<ScheduleItem> BuildAutoSchedule(
        IEnumerable<WorkOrder> orders,
        IEnumerable<Machine> machines,
        Action<ScheduleProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var orderList = orders.ToList();
        var machineList = machines.ToList();
        if (orderList.Count == 0 || machineList.Count == 0)
            return [];

        var iterationCount = GetOptimizationIterationCount();
        var jobOrders = BuildCandidateJobOrders(orderList, iterationCount);

        List<ScheduleItem>? best = null;
        var bestScore = double.MaxValue;

        for (var i = 0; i < jobOrders.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var schedule = BuildScheduleForJobOrder(orderList, machineList, jobOrders[i], p =>
            {
                progress?.Invoke(p with
                {
                    Iteration = i + 1,
                    IterationCount = jobOrders.Count
                });
            }, cancellationToken);

            var score = ScoreSchedule(schedule);
            if (score < bestScore)
            {
                bestScore = score;
                best = schedule;
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        return best ?? BuildScheduleForJobOrder(orderList, machineList, [], null, cancellationToken);
    }

    public static void RebuildFromLaneOrder(IEnumerable<MachineLane> lanes, Func<WorkOrder, DateTime?>? releaseOverride = null)
    {
        var laneList = lanes.ToList();
        var laneByMachineId = laneList.ToDictionary(x => x.Machine.Id, x => x);
        var machineAvailable = laneList.ToDictionary(x => x.Machine.Id, x => x.Machine.AvailableAt);
        var laneIndexes = laneList.ToDictionary(x => x.Machine.Id, _ => 0);
        var predecessorOrderId = BuildPredecessorMap(laneList.SelectMany(x => x.Tasks).Select(x => x.Order));
        var endByOrderId = new Dictionary<string, DateTime>();

        var total = laneList.Sum(l => l.Tasks.Count);
        var done = 0;

        while (done < total)
        {
            var candidates = new List<(TaskCard Card, DateTime StartAt)>();

            foreach (var lane in laneList)
            {
                var idx = laneIndexes[lane.Machine.Id];
                if (idx >= lane.Tasks.Count) continue;

                var card = lane.Tasks[idx];
                if (predecessorOrderId.TryGetValue(card.Order.Id, out var predId) && !endByOrderId.ContainsKey(predId))
                    continue;

                var release = releaseOverride?.Invoke(card.Order) ?? card.Order.ReleaseAt ?? DateTime.MinValue;
                var predEnd = predecessorOrderId.TryGetValue(card.Order.Id, out predId)
                    ? endByOrderId[predId]
                    : DateTime.MinValue;
                var start = AlignToWorkingTime(Max(machineAvailable[lane.Machine.Id], release, predEnd));
                candidates.Add((card, start));
            }

            if (candidates.Count == 0)
                throw new InvalidOperationException("無法重建排程，請確認製程序列未造成死結。");

            var selected = candidates
                .OrderBy(x => x.StartAt)
                .ThenBy(x => x.Card.Order.SortOrder)
                .ThenBy(x => x.Card.Order.Priority)
                .ThenBy(x => x.Card.Order.Id)
                .First();

            var laneForCard = laneByMachineId[selected.Card.MachineId];
            var endAt = AddWorkingHours(selected.StartAt, selected.Card.Order.ProcessHours);

            selected.Card.StartAt = selected.StartAt;
            selected.Card.EndAt = endAt;

            machineAvailable[laneForCard.Machine.Id] = endAt;
            laneIndexes[laneForCard.Machine.Id]++;
            endByOrderId[selected.Card.Order.Id] = endAt;
            done++;
        }
    }
    public static bool IsWorkingTime(DateTime time)
    {
        if (time == DateTime.MinValue || time == DateTime.MaxValue)
            return true;

        if (time.DayOfWeek == DayOfWeek.Sunday)
            return false;

        var hour = time.TimeOfDay.TotalHours;
        return hour >= WorkStartHour || hour < WorkEndHour;
    }
    public static DateTime AlignToWorkingTime(DateTime time)
    {
        if (time == DateTime.MinValue || time == DateTime.MaxValue)
            return time;

        var current = time;
        while (true)
        {
            if (current.DayOfWeek == DayOfWeek.Sunday)
            {
                current = current.Date.AddDays(1).AddHours(WorkStartHour);
                continue;
            }

            var hour = current.TimeOfDay.TotalHours;
            if (hour >= WorkStartHour || hour < WorkEndHour)
                return current;

            current = current.Date.AddHours(WorkStartHour);
        }
    }

    public static DateTime AddWorkingHours(DateTime startAt, double hours)
    {
        var current = AlignToWorkingTime(startAt);
        if (hours <= 0)
            return current;

        var remain = hours;
        while (remain > 1e-9)
        {
            current = AlignToWorkingTime(current);
            var hour = current.TimeOfDay.TotalHours;

            var segmentEnd = hour >= WorkStartHour
                ? current.Date.AddDays(1)
                : current.Date.AddHours(WorkEndHour);

            var available = (segmentEnd - current).TotalHours;
            if (available <= 1e-9)
            {
                current = AlignToWorkingTime(current.AddMinutes(1));
                continue;
            }

            var alloc = Math.Min(remain, available);
            current = current.AddHours(alloc);
            remain -= alloc;

            if (remain > 1e-9)
                current = AlignToWorkingTime(current);
        }

        return current;
    }

    private static List<List<string>> BuildCandidateJobOrders(IReadOnlyCollection<WorkOrder> orders, int iterationCount)
    {
        var grouped = orders
            .GroupBy(GetJobKey)
            .Select(g => new JobMetric
            {
                JobKey = g.Key,
                Priority = g.Min(x => x.Priority),
                SortOrder = g.Min(x => x.SortOrder),
                DueAt = g.Min(x => x.DueAt ?? DateTime.MaxValue),
                ReleaseAt = g.Min(x => x.ReleaseAt ?? DateTime.MinValue),
                TotalHours = g.Sum(x => x.ProcessHours)
            })
            .ToList();

        var baseOrders = new List<List<string>>
        {
            grouped.OrderBy(x => x.SortOrder).ThenBy(x => x.Priority).ThenBy(x => x.DueAt).ThenBy(x => x.TotalHours).ThenBy(x => x.JobKey, StringComparer.OrdinalIgnoreCase).Select(x => x.JobKey).ToList(),
            grouped.OrderBy(x => x.DueAt).ThenBy(x => x.SortOrder).ThenBy(x => x.Priority).ThenBy(x => x.TotalHours).ThenBy(x => x.JobKey, StringComparer.OrdinalIgnoreCase).Select(x => x.JobKey).ToList(),
            grouped.OrderBy(x => x.TotalHours).ThenBy(x => x.SortOrder).ThenBy(x => x.DueAt).ThenBy(x => x.Priority).ThenBy(x => x.JobKey, StringComparer.OrdinalIgnoreCase).Select(x => x.JobKey).ToList(),
            grouped.OrderByDescending(x => x.TotalHours).ThenBy(x => x.SortOrder).ThenBy(x => x.DueAt).ThenBy(x => x.Priority).ThenBy(x => x.JobKey, StringComparer.OrdinalIgnoreCase).Select(x => x.JobKey).ToList(),
            grouped.OrderBy(x => (x.DueAt - DateTime.MinValue).TotalHours / Math.Max(0.1, x.TotalHours)).ThenBy(x => x.SortOrder).ThenBy(x => x.Priority).ThenBy(x => x.JobKey, StringComparer.OrdinalIgnoreCase).Select(x => x.JobKey).ToList()
        };

        var rng = new Random(20260308);
        var candidates = new List<List<string>>(iterationCount);

        for (var i = 0; i < iterationCount; i++)
        {
            var seed = baseOrders[i % baseOrders.Count];
            var candidate = seed.ToList();

            if (candidate.Count > 1)
            {
                var swapCount = 1 + rng.Next(1, Math.Min(5, candidate.Count));
                for (var s = 0; s < swapCount; s++)
                {
                    var a = rng.Next(candidate.Count);
                    var b = rng.Next(candidate.Count);
                    (candidate[a], candidate[b]) = (candidate[b], candidate[a]);
                }
            }

            candidates.Add(candidate);
        }

        return candidates;
    }

    private static List<ScheduleItem> BuildScheduleForJobOrder(
        IReadOnlyCollection<WorkOrder> orders,
        IReadOnlyCollection<Machine> machines,
        IReadOnlyList<string> jobOrder,
        Action<ScheduleProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var machinePool = machines
            .GroupBy(m => m.MachineType)
            .ToDictionary(
                g => g.Key,
                g => g.Select(m => new MachineState(m.Id, m.AvailableAt)).OrderBy(x => x.AvailableAt).ThenBy(x => x.Id).ToList());

        var grouped = orders
            .GroupBy(GetJobKey)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(o => o.Sequence).ThenBy(OrderSortKey).ToList(),
                StringComparer.OrdinalIgnoreCase);

        var rankMap = jobOrder
            .Select((key, idx) => (key, idx))
            .GroupBy(x => x.key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Min(x => x.idx), StringComparer.OrdinalIgnoreCase);

        var nextIdx = grouped.Keys.ToDictionary(k => k, _ => 0, StringComparer.OrdinalIgnoreCase);
        var ready = grouped
            .Where(x => x.Value.Count > 0)
            .Select(x => (JobKey: x.Key, Order: x.Value[0]))
            .ToList();

        var schedule = new List<ScheduleItem>(orders.Count);
        var total = grouped.Values.Sum(v => v.Count);

        while (schedule.Count < total)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (ready.Count == 0)
                throw new InvalidOperationException("找不到可排程的製程，請檢查路徑資料。");

            var candidates = new List<(string JobKey, WorkOrder Order, MachineState Machine, DateTime StartAt)>();
            foreach (var r in ready)
            {
                if (!machinePool.TryGetValue(r.Order.MachineType, out var machineCandidates) || machineCandidates.Count == 0)
                    throw new InvalidOperationException($"No machine found for machine_type='{r.Order.MachineType}'.");

                var chosen = ChooseMachine(machineCandidates, r.Order.PreferredMachineIds);
                var releaseAt = r.Order.ReleaseAt ?? DateTime.MinValue;
                var startAt = AlignToWorkingTime(Max(chosen.AvailableAt, releaseAt));
                candidates.Add((r.JobKey, r.Order, chosen, startAt));
            }

            var selected = candidates
                .OrderBy(x => x.StartAt)
                .ThenBy(x => x.Order.SortOrder)
                .ThenBy(x => rankMap.TryGetValue(x.JobKey, out var rank) ? rank : int.MaxValue)
                .ThenBy(x => x.Order.DueAt ?? DateTime.MaxValue)
                .ThenBy(x => x.Order.Priority)
                .ThenBy(x => x.Order.Id)
                .First();

            ready.RemoveAll(x => string.Equals(x.Order.Id, selected.Order.Id, StringComparison.OrdinalIgnoreCase));

            var endAt = AddWorkingHours(selected.StartAt, selected.Order.ProcessHours);
            schedule.Add(new ScheduleItem
            {
                OrderId = selected.Order.Id,
                MachineId = selected.Machine.Id,
                StartAt = selected.StartAt,
                EndAt = endAt,
                DueAt = selected.Order.DueAt,
                JobId = selected.Order.JobId,
                Sequence = selected.Order.Sequence
            });

            progress?.Invoke(new ScheduleProgress
            {
                Iteration = 1,
                IterationCount = 1,
                Done = schedule.Count,
                Total = total,
                JobId = selected.Order.JobId,
                ProcessCode = selected.Order.ProcessCode,
                MachineId = selected.Machine.Id
            });

            selected.Machine.AvailableAt = endAt;

            var idx = ++nextIdx[selected.JobKey];
            if (idx < grouped[selected.JobKey].Count)
            {
                var next = grouped[selected.JobKey][idx];
                var inheritedRelease = endAt;
                if (next.ReleaseAt is null || next.ReleaseAt < inheritedRelease)
                {
                    next = new WorkOrder
                    {
                        Id = next.Id,
                        ProcessCode = next.ProcessCode,
                        MachineType = next.MachineType,
                        ProcessHours = next.ProcessHours,
                        Priority = next.Priority,
                        SortOrder = next.SortOrder,
                        DueAt = next.DueAt,
                        ReleaseAt = inheritedRelease,
                        PreferredMachineIds = next.PreferredMachineIds,
                        JobId = next.JobId,
                        Sequence = next.Sequence
                    };
                    grouped[selected.JobKey][idx] = next;
                }

                ready.Add((selected.JobKey, next));
            }
        }

        return schedule;
    }

    private static double ScoreSchedule(IReadOnlyCollection<ScheduleItem> schedule)
    {
        if (schedule.Count == 0)
            return double.MaxValue;

        var minStart = schedule.Min(x => x.StartAt);
        var maxEnd = schedule.Max(x => x.EndAt);
        var makespanHours = (maxEnd - minStart).TotalHours;

        var tardinessHours = 0.0;
        var lateOps = 0;
        var flowHours = 0.0;

        foreach (var item in schedule)
        {
            flowHours += (item.EndAt - minStart).TotalHours;
            if (item.DueAt is null)
                continue;

            var late = (item.EndAt - item.DueAt.Value).TotalHours;
            if (late > 0)
            {
                lateOps++;
                tardinessHours += late;
            }
        }

        return tardinessHours * 10000.0 + lateOps * 500.0 + makespanHours * 10.0 + flowHours * 0.1;
    }

    private static int GetOptimizationIterationCount()
    {
        var raw = Environment.GetEnvironmentVariable("PROD_SCHEDULER_OPT_ITERATIONS");
        if (!int.TryParse(raw, out var parsed))
            parsed = SchedulerConfig.OptimizationIterations;

        return Math.Clamp(parsed, 10, 200);
    }

    private static string GetJobKey(WorkOrder order)
        => string.IsNullOrWhiteSpace(order.JobId) ? $"__single__{order.Id}" : order.JobId;

    private static Dictionary<string, string> BuildPredecessorMap(IEnumerable<WorkOrder> orders)
    {
        var result = new Dictionary<string, string>();
        var grouped = orders
            .Where(o => !string.IsNullOrWhiteSpace(o.JobId))
            .GroupBy(o => o.JobId);

        foreach (var g in grouped)
        {
            var ordered = g.OrderBy(x => x.Sequence).ThenBy(x => x.Id).ToList();
            for (var i = 1; i < ordered.Count; i++)
            {
                var current = ordered[i];
                var previous = ordered[i - 1];
                if (current.Sequence > previous.Sequence)
                    result[current.Id] = previous.Id;
            }
        }

        return result;
    }

    private static MachineState ChooseMachine(List<MachineState> candidates, IReadOnlyList<string> preferredIds)
    {
        IEnumerable<MachineState> source = candidates;
        if (preferredIds.Count > 0)
        {
            var set = preferredIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var preferred = candidates.Where(x => set.Contains(x.Id)).ToList();
            if (preferred.Count > 0)
                source = preferred;
        }

        return source.OrderBy(x => x.AvailableAt).ThenBy(x => x.Id).First();
    }

    private static (int SortOrder, int Priority, DateTime DueAt, DateTime ReleaseAt, string Id) OrderSortKey(WorkOrder order)
    {
        return (
            order.SortOrder,
            order.Priority,
            order.DueAt ?? DateTime.MaxValue,
            order.ReleaseAt ?? DateTime.MinValue,
            order.Id);
    }

    private static DateTime Max(DateTime a, DateTime b) => a > b ? a : b;
    private static DateTime Max(DateTime a, DateTime b, DateTime c) => Max(Max(a, b), c);

    private sealed class JobMetric
    {
        public required string JobKey { get; init; }
        public required int Priority { get; init; }
        public required int SortOrder { get; init; }
        public required DateTime DueAt { get; init; }
        public required DateTime ReleaseAt { get; init; }
        public required double TotalHours { get; init; }
    }

    private sealed class MachineState
    {
        public MachineState(string id, DateTime availableAt)
        {
            Id = id;
            AvailableAt = availableAt;
        }

        public string Id { get; }
        public DateTime AvailableAt { get; set; }
    }
}

public sealed record ScheduleProgress
{
    public int Iteration { get; init; }
    public int IterationCount { get; init; }
    public int Done { get; init; }
    public int Total { get; init; }
    public string JobId { get; init; } = string.Empty;
    public string ProcessCode { get; init; } = string.Empty;
    public string MachineId { get; init; } = string.Empty;
}

