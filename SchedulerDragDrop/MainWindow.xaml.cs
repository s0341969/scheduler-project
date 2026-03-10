using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace SchedulerDragDrop;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly ObservableCollection<MachineLane> _lanes = new();
    private readonly ObservableCollection<TimeTick> _timeTicks = new();
    private readonly ObservableCollection<string> _selectedJobLines = new();
    private readonly ObservableCollection<ProcessGroupOption> _processGroupOptions = new();
    private readonly Dictionary<string, double> _orderShiftHours = new();
    private readonly HashSet<string> _selectedOrderIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly Random _random = new(42);

    private Point _dragStart;
    private TaskCard? _dragItem;
    private bool _isSyncingScroll;
    private string? _selectedJobId;
    private string? _lastPanelJobId;
    
    private string? _primaryOrderId;
    private DateTime _timelineStart;
    private DateTime _timelineEnd;
    private double _pixelsPerHour = 24.0;
    private double _laneWidth = 120.0;
    private double _laneCanvasHeight = 600;
    private bool _isBusy;
    private string _statusText = "就緒";
    private double _progressValue;
    private double _progressMaximum = 1;
    private bool _isProgressIndeterminate;
    private DateTime _lastProgressUpdate = DateTime.MinValue;
    private DateTime _lastTickStart = DateTime.MinValue;
    private DateTime _lastTickEnd = DateTime.MinValue;
    private CancellationTokenSource? _queryCts;
    private readonly DispatcherTimer _uiAdjustTimer = new() { Interval = TimeSpan.FromMilliseconds(40) };
    private readonly DispatcherTimer _dragPreviewTimer = new() { Interval = TimeSpan.FromMilliseconds(16) };
    private DragPreviewWindow? _dragPreview;
    private double? _pendingLaneWidth;
    private double? _pendingPixelsPerHour;

    public ObservableCollection<string> SelectedJobLines => _selectedJobLines;
    public ObservableCollection<TimeTick> TimeTicks => _timeTicks;
    public ObservableCollection<ProcessGroupOption> ProcessGroupOptions => _processGroupOptions;
    public string ProcessGroupSummary => BuildProcessGroupSummary();

    public double PixelsPerHour
    {
        get => _pixelsPerHour;
        private set
        {
            if (Math.Abs(_pixelsPerHour - value) < 0.1) return;
            _pixelsPerHour = value;
            OnPropertyChanged(nameof(PixelsPerHour));
            OnPropertyChanged(nameof(ZoomLabel));
            OnPropertyChanged(nameof(TimeAxisHeightLabel));
        }
    }

    public string ZoomLabel => $"{Math.Round(PixelsPerHour / 24.0 * 100):0}%";
    public string TimeAxisHeightLabel => $"{PixelsPerHour:0}px/小時";

    public double LaneWidth
    {
        get => _laneWidth;
        private set
        {
            if (Math.Abs(_laneWidth - value) < 0.1) return;
            _laneWidth = value;
            OnPropertyChanged(nameof(LaneWidth));
            OnPropertyChanged(nameof(TaskCardWidth));
        }
    }

    public double TaskCardWidth => Math.Max(80, LaneWidth - 12);

    public double LaneCanvasHeight
    {
        get => _laneCanvasHeight;
        private set
        {
            if (Math.Abs(_laneCanvasHeight - value) < 0.1) return;
            _laneCanvasHeight = value;
            OnPropertyChanged(nameof(LaneCanvasHeight));
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (_isBusy == value) return;
            _isBusy = value;
            OnPropertyChanged(nameof(IsBusy));
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set
        {
            if (string.Equals(_statusText, value, StringComparison.Ordinal)) return;
            _statusText = value;
            OnPropertyChanged(nameof(StatusText));
        }
    }

    public double ProgressValue
    {
        get => _progressValue;
        private set
        {
            if (Math.Abs(_progressValue - value) < 0.01) return;
            _progressValue = value;
            OnPropertyChanged(nameof(ProgressValue));
            OnPropertyChanged(nameof(ProgressPercentText));
        }
    }

    public double ProgressMaximum
    {
        get => _progressMaximum;
        private set
        {
            if (Math.Abs(_progressMaximum - value) < 0.01) return;
            _progressMaximum = value;
            OnPropertyChanged(nameof(ProgressMaximum));
            OnPropertyChanged(nameof(ProgressPercentText));
        }
    }

    public bool IsProgressIndeterminate
    {
        get => _isProgressIndeterminate;
        private set
        {
            if (_isProgressIndeterminate == value) return;
            _isProgressIndeterminate = value;
            OnPropertyChanged(nameof(IsProgressIndeterminate));
            OnPropertyChanged(nameof(ProgressPercentText));
        }
    }

    public string ProgressPercentText
    {
        get
        {
            if (IsProgressIndeterminate || ProgressMaximum <= 0.0001) return "--%";
            var percent = Math.Clamp(ProgressValue / ProgressMaximum * 100.0, 0, 100);
            return $"{percent:0}%";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;

        HeaderItemsControl.ItemsSource = _lanes;
        LaneItemsControl.ItemsSource = _lanes;
        InitializeEmptyState();
        Loaded += MainWindow_Loaded;
        _uiAdjustTimer.Tick += UiAdjustTimer_Tick;
        _dragPreviewTimer.Tick += DragPreviewTimer_Tick;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            LoadProcessGroupOptions();
        }
        catch (Exception ex)
        {
            StatusText = $"群組載入失敗：{ex.Message}";
        }
    }

    private void UiAdjustTimer_Tick(object? sender, EventArgs e)
    {
        var changed = false;

        if (_pendingLaneWidth is double lane)
        {
            LaneWidth = lane;
            _pendingLaneWidth = null;
            changed = true;
        }

        if (_pendingPixelsPerHour is double axis)
        {
            PixelsPerHour = axis;
            _pendingPixelsPerHour = null;
            changed = true;
        }

        if (_pendingLaneWidth is null && _pendingPixelsPerHour is null)
            _uiAdjustTimer.Stop();

        if (changed)
        {
            RefreshTimelineLayout();
            ApplySelectionAndHighlights();
        }
    }
    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private void LoadProcessGroupOptions()
    {
        var selectedBefore = _processGroupOptions.Where(x => x.IsSelected).Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        _processGroupOptions.Clear();

        if (DatabaseScheduleLoader.TryLoadProcessGroups(out var groups, out var reason))
        {
            foreach (var group in groups)
            {
                _processGroupOptions.Add(new ProcessGroupOption
                {
                    Name = group,
                    IsSelected = selectedBefore.Contains(group)
                });
            }
        }
        else if (!string.IsNullOrWhiteSpace(reason))
        {
            MessageBox.Show(reason, "群組讀取", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        OnPropertyChanged(nameof(ProcessGroupSummary));
    }

    private void InitializeEmptyState()
    {
        _lanes.Clear();
        _timeTicks.Clear();
        _selectedOrderIds.Clear();
        _orderShiftHours.Clear();
        _selectedJobId = null;
        _primaryOrderId = null;
        LaneCanvasHeight = 600;
        _lastPanelJobId = null;
        UpdateSelectedJobPanel(null);
    }

    private IReadOnlyCollection<string>? GetSelectedProcessGroups()
    {
        var selected = _processGroupOptions
            .Where(x => x.IsSelected)
            .Select(x => x.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return selected.Count == 0 ? null : selected;
    }

    private string BuildProcessGroupSummary()
    {
        var selected = _processGroupOptions.Where(x => x.IsSelected).Select(x => x.Name).ToList();
        if (selected.Count == 0 || selected.Count == _processGroupOptions.Count)
            return "全部";
        if (selected.Count <= 2)
            return string.Join(",", selected);
        return $"已選 {selected.Count} 項";
    }

    private void ProcessGroupOption_Changed(object sender, RoutedEventArgs e)
    {
        OnPropertyChanged(nameof(ProcessGroupSummary));
    }

    private void OpenProcessGroupDialog_Click(object sender, RoutedEventArgs e)
    {
        if (_processGroupOptions.Count == 0)
            LoadProcessGroupOptions();

        var dialog = new ProcessGroupSelectionDialog(_processGroupOptions) { Owner = this };
        if (dialog.ShowDialog() != true)
            return;

        var selected = dialog.SelectedNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var option in _processGroupOptions)
            option.IsSelected = selected.Contains(option.Name);

        OnPropertyChanged(nameof(ProcessGroupSummary));
    }

    private void ProcessGroupCombo_DropDownOpened(object sender, EventArgs e)
    {
        LoadProcessGroupOptions();
    }

    private sealed class QueryLoadResult
    {
        public bool Success { get; init; }
        public string Reason { get; init; } = string.Empty;
        public List<Machine> Machines { get; init; } = [];
        public List<WorkOrder> Orders { get; init; } = [];
    }

    private static QueryLoadResult LoadFromDatabase(IReadOnlyCollection<string>? processGroupFilters)
    {
        var ok = DatabaseScheduleLoader.TryLoadFromEnvironment(
            DateTime.Now,
            processGroupFilters,
            out var machines,
            out var orders,
            out var reason);

        return new QueryLoadResult
        {
            Success = ok,
            Reason = reason,
            Machines = machines,
            Orders = orders
        };
    }

    private void SetBusyState(bool busy, string status, bool indeterminate = true, double value = 0, double max = 1)
    {
        IsBusy = busy;
        StatusText = status;
        IsProgressIndeterminate = indeterminate;
        ProgressMaximum = Math.Max(1, max);
        ProgressValue = Math.Clamp(value, 0, ProgressMaximum);
        Mouse.OverrideCursor = busy ? Cursors.Wait : null;
    }

    private void UpdateScheduleProgress(ScheduleProgress p)
    {
        var now = DateTime.Now;
        if ((now - _lastProgressUpdate).TotalMilliseconds < 80 && p.Done < p.Total)
            return;

        _lastProgressUpdate = now;
        IsProgressIndeterminate = false;
        ProgressMaximum = Math.Max(1, p.Total);
        ProgressValue = Math.Clamp(p.Done, 0, ProgressMaximum);
        StatusText = $"排程中 第{p.Iteration}/{p.IterationCount}輪 | {p.Done}/{p.Total} | {p.JobId}-{p.ProcessCode} | {p.MachineId}";
    }

    private async Task<List<ScheduleItem>> RunAutoScheduleAsync(
        List<WorkOrder> orders,
        List<Machine> machines,
        CancellationToken cancellationToken = default)
    {
        var progress = new Progress<ScheduleProgress>(UpdateScheduleProgress);
        return await Task.Run(() =>
            SchedulerEngine.BuildAutoSchedule(
                orders,
                machines,
                p => ((IProgress<ScheduleProgress>)progress).Report(p),
                cancellationToken),
            cancellationToken);
    }

    private void ApplyScheduleResult(List<Machine> machines, List<WorkOrder> orders, List<ScheduleItem> result)
    {
        _lanes.Clear();
        foreach (var machine in machines.OrderBy(m => m.Id, StringComparer.OrdinalIgnoreCase))
            _lanes.Add(new MachineLane { Machine = machine });

        var laneByMachineId = _lanes.ToDictionary(x => x.Machine.Id, StringComparer.OrdinalIgnoreCase);
        var cardsByOrderId = orders.ToDictionary(o => o.Id, o => new TaskCard
        {
            Order = o,
            MachineId = string.Empty,
            StartAt = DateTime.MinValue,
            EndAt = DateTime.MinValue,
            IsRelatedSelected = false,
            IsOperationSelected = false,
            IsPrimarySelected = false,
            TopPx = 0,
            HeightPx = 28
        }, StringComparer.OrdinalIgnoreCase);

        foreach (var item in result.OrderBy(x => x.StartAt).ThenBy(x => x.OrderId, StringComparer.OrdinalIgnoreCase))
        {
            if (!laneByMachineId.TryGetValue(item.MachineId, out var lane))
                continue;
            if (!cardsByOrderId.TryGetValue(item.OrderId, out var card))
                continue;

            card.MachineId = item.MachineId;
            card.StartAt = item.StartAt;
            card.EndAt = item.EndAt;
            lane.Tasks.Add(card);
        }

        RefreshTimelineLayout();
        NormalizeSelection();
        ApplySelectionAndHighlights(true);
    }

    private async void QuerySchedule_Click(object sender, RoutedEventArgs e)
    {
        if (IsBusy) return;
        CancellationTokenSource? cts = null;

        try
        {
            cts = new CancellationTokenSource();
            _queryCts = cts;

            var filters = GetSelectedProcessGroups();
            SetBusyState(true, "讀取資料中...", true, 0, 1);

            var load = await Task.Run(() => LoadFromDatabase(filters), cts.Token);
            cts.Token.ThrowIfCancellationRequested();
            if (!load.Success)
            {
                SetBusyState(false, "就緒", true, 0, 1);
                MessageBox.Show(load.Reason, "資料來源", MessageBoxButton.OK, MessageBoxImage.Information);
                InitializeEmptyState();
                LoadProcessGroupOptions();
                return;
            }

            SetBusyState(true, "排程中...", false, 0, Math.Max(1, load.Orders.Count));
            var sw = Stopwatch.StartNew();
            var result = await RunAutoScheduleAsync(load.Orders, load.Machines, cts.Token);
            cts.Token.ThrowIfCancellationRequested();
            ApplyScheduleResult(load.Machines, load.Orders, result);
            sw.Stop();

            SetBusyState(false, $"查詢完成：{result.Count} 筆，耗時 {sw.Elapsed:mm\\:ss}", false, ProgressMaximum, ProgressMaximum);
        }
        catch (OperationCanceledException)
        {
            SetBusyState(false, "查詢已取消", false, 0, 1);
        }
        catch (Exception ex)
        {
            SetBusyState(false, "查詢失敗", false, 0, 1);
            MessageBox.Show(ex.Message, "查詢排程錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            if (ReferenceEquals(_queryCts, cts))
                _queryCts = null;
            cts?.Dispose();
        }
    }

    private List<Machine> BuildMachines(DateTime baseTime)
    {
        var machineTypes = new[] { "CUT", "DRL", "WLD", "ASM", "PNT" };
        var result = new List<Machine>();

        foreach (var type in machineTypes)
        {
            for (var i = 1; i <= 3; i++)
            {
                result.Add(new Machine
                {
                    Id = $"{type}-{i:00}",
                    MachineType = type,
                    AvailableAt = baseTime
                });
            }
        }

        return result;
    }

    private List<WorkOrder> BuildWorkOrders(DateTime baseTime, List<Machine> machines)
    {
        var routes = new List<(string ProcessCode, string MachineType, double MinHours, double MaxHours)>
        {
            ("P10", "CUT", 1.0, 2.0), ("P20", "DRL", 0.8, 1.8), ("P30", "WLD", 1.2, 2.5),
            ("P40", "ASM", 1.0, 2.2), ("P50", "PNT", 0.8, 1.5), ("P60", "ASM", 1.0, 2.0),
            ("P70", "WLD", 1.1, 2.3), ("P80", "PNT", 0.7, 1.4), ("P90", "ASM", 1.0, 2.1), ("P100", "DRL", 0.9, 1.7)
        };

        var machineIdsByType = machines
            .GroupBy(m => m.MachineType)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(m => m.Id).ToList());

        var all = new List<WorkOrder>();
        for (var cardIndex = 1; cardIndex <= 30; cardIndex++)
        {
            var jobId = $"CARD-{cardIndex:000}";
            var processCount = _random.Next(5, 11);
            var dueAt = baseTime.AddHours(28 + cardIndex * 1.6 + processCount * 1.1);
            var priority = _random.Next(1, 4);

            for (var seq = 0; seq < processCount; seq++)
            {
                var route = routes[seq];
                var hours = Math.Round(route.MinHours + _random.NextDouble() * (route.MaxHours - route.MinHours), 1);
                var preferred = PickPreferredMachineIds(machineIdsByType[route.MachineType]);

                all.Add(new WorkOrder
                {
                    Id = $"{jobId}-{route.ProcessCode}",
                    ProcessCode = route.ProcessCode,
                    MachineType = route.MachineType,
                    ProcessHours = hours,
                    Priority = priority,
                    SortOrder = (seq + 1) * 10,
                    DueAt = dueAt,
                    ReleaseAt = seq == 0 ? baseTime : null,
                    PreferredMachineIds = preferred,
                    JobId = jobId,
                    Sequence = (seq + 1) * 10
                });
            }
        }

        return all;
    }

    private IReadOnlyList<string> PickPreferredMachineIds(IReadOnlyList<string> machineIds)
    {
        if (_random.NextDouble() < 0.35) return Array.Empty<string>();
        return new[] { machineIds[_random.Next(machineIds.Count)] };
    }

    private void CancelQuery_Click(object sender, RoutedEventArgs e)
    {
        if (_queryCts is null || _queryCts.IsCancellationRequested)
            return;

        _queryCts.Cancel();
        StatusText = "取消中...";
        IsProgressIndeterminate = true;
    }

    private async Task ApplyAutoScheduleAsync(string actionName)
    {
        var orders = _lanes.SelectMany(x => x.Tasks).Select(x => x.Order).DistinctBy(x => x.Id).ToList();
        var machines = _lanes.Select(x => x.Machine).ToList();
        if (orders.Count == 0 || machines.Count == 0)
            return;

        SetBusyState(true, $"{actionName}中...", false, 0, Math.Max(1, orders.Count));
        var sw = Stopwatch.StartNew();
        var result = await RunAutoScheduleAsync(orders, machines);
        ApplyScheduleResult(machines, orders, result);
        sw.Stop();
        SetBusyState(false, $"{actionName}完成：{result.Count} 筆，耗時 {sw.Elapsed:mm\\:ss}", false, ProgressMaximum, ProgressMaximum);
    }

    private void RebuildWithShifts(IReadOnlyDictionary<string, DateTime>? noEarlierStartMap = null)
    {
        var shiftMap = _orderShiftHours.ToDictionary(x => x.Key, x => x.Value);

        SchedulerEngine.RebuildFromLaneOrder(_lanes, order =>
        {
            DateTime? release = order.ReleaseAt;

            if (shiftMap.TryGetValue(order.Id, out var shiftHours) && Math.Abs(shiftHours) >= 0.001)
            {
                var baseRelease = order.ReleaseAt ?? _timelineStart;
                release = baseRelease.AddHours(shiftHours);
            }

            if (noEarlierStartMap is not null && noEarlierStartMap.TryGetValue(order.Id, out var noEarlierThan))
            {
                if (release is null || release < noEarlierThan)
                    release = noEarlierThan;
            }

            return release;
        });

        RefreshTimelineLayout();
        NormalizeSelection();
        ApplySelectionAndHighlights(true);
    }

    private void RefreshTimelineLayout(bool preserveRange = false)
    {
        var cards = _lanes.SelectMany(l => l.Tasks).ToList();
        if (cards.Count == 0)
        {
            LaneCanvasHeight = 600;
            _timeTicks.Clear();
            _lastTickStart = DateTime.MinValue;
            _lastTickEnd = DateTime.MinValue;
            return;
        }

        var minStart = cards.Min(c => c.StartAt);
        var maxEnd = cards.Max(c => c.EndAt);

        var computedStart = new DateTime(minStart.Year, minStart.Month, minStart.Day, minStart.Hour, 0, 0);
        if (computedStart > minStart)
            computedStart = computedStart.AddHours(-1);

        var computedEnd = new DateTime(maxEnd.Year, maxEnd.Month, maxEnd.Day, maxEnd.Hour, 0, 0).AddHours(1);

        if (preserveRange && _timelineEnd > _timelineStart)
        {
            _timelineStart = computedStart < _timelineStart ? computedStart : _timelineStart;
            _timelineEnd = computedEnd > _timelineEnd ? computedEnd : _timelineEnd;
        }
        else
        {
            _timelineStart = computedStart;
            _timelineEnd = computedEnd;
        }

        var tickRangeChanged = _timelineStart != _lastTickStart || _timelineEnd != _lastTickEnd || _timeTicks.Count == 0;
        if (tickRangeChanged)
        {
            _timeTicks.Clear();

            var tick = _timelineStart;
            while (tick <= _timelineEnd)
            {
                _timeTicks.Add(new TimeTick
                {
                    Time = tick,
                    Label = tick.ToString("MM-dd HH:mm"),
                    IsWorking = SchedulerEngine.IsWorkingTime(tick)
                });
                tick = tick.AddHours(1);
            }

            _lastTickStart = _timelineStart;
            _lastTickEnd = _timelineEnd;
        }

        var totalHours = Math.Max(1.0, (_timelineEnd - _timelineStart).TotalHours);
        LaneCanvasHeight = totalHours * PixelsPerHour;

        foreach (var card in cards)
        {
            var startHours = (card.StartAt - _timelineStart).TotalHours;
            var durationHours = Math.Max(0.2, (card.EndAt - card.StartAt).TotalHours);
            card.TopPx = Math.Max(0, startHours * PixelsPerHour + 2);
            card.HeightPx = Math.Max(28, durationHours * PixelsPerHour - 4);
        }
    }

    private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!IsLoaded) return;
        _pendingPixelsPerHour = e.NewValue;
        _uiAdjustTimer.Start();
    }

    private void LaneWidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!IsLoaded) return;
        _pendingLaneWidth = e.NewValue;
        _uiAdjustTimer.Start();
    }
    private void ShiftSelectedEarlier_Click(object sender, RoutedEventArgs e) => ShiftSelectedJobsByHours(-1);
    private void ShiftSelectedLater_Click(object sender, RoutedEventArgs e) => ShiftSelectedJobsByHours(1);

    private void ShiftSelectedJobsByHours(double deltaHours)
    {
        var selectedOrders = GetSelectedCards().Select(c => c.Order.Id).Distinct().ToList();
        if (selectedOrders.Count == 0)
        {
            MessageBox.Show("請先選取一個或多個製程。", "Shift", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var deltaByOrderId = selectedOrders.ToDictionary(id => id, _ => deltaHours, StringComparer.OrdinalIgnoreCase);
        TryApplySelectedOnlyShift(deltaByOrderId);
    }

    private void ShiftSelectedToDateTimeMenu_Click(object sender, RoutedEventArgs e)
    {
        var card = GetCardFromContextMenuSender(sender);
        if (card is null) return;

        EnsureCardSelectedForContext(card);
        var selectedCards = GetSelectedCards().ToList();
        if (selectedCards.Count == 0)
            return;

        var confirm = MessageBox.Show("是否要開啟推移設定視窗？", "確認", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes)
            return;

        var defaultStart = selectedCards.Min(c => c.StartAt);
        var dialog = new ShiftToDateTimeDialog(defaultStart) { Owner = this };
        if (dialog.ShowDialog() != true || dialog.SelectedDateTime is null)
            return;

        ApplyShiftToDateTime(selectedCards, dialog.SelectedDateTime.Value);
    }

    private void ApplyShiftToDateTime(List<TaskCard> selectedCards, DateTime targetDateTime)
    {
        if (selectedCards.Count == 0)
            return;

        var selectedOrders = selectedCards
            .GroupBy(c => c.Order.Id)
            .Select(g => g.First())
            .ToList();

        var firstStart = selectedOrders.Min(c => c.StartAt);
        var deltaHours = (targetDateTime - firstStart).TotalHours;

        var deltaByOrderId = selectedOrders.ToDictionary(c => c.Order.Id, _ => deltaHours, StringComparer.OrdinalIgnoreCase);
        TryApplySelectedOnlyShift(deltaByOrderId);
    }

    private void TryApplySelectedOnlyShift(Dictionary<string, double> deltaByOrderId)
    {
        if (deltaByOrderId.Count == 1)
        {
            var single = deltaByOrderId.First();
            if (TryApplyFastSingleShift(single.Key, single.Value))
                return;
        }
        var before = _lanes.SelectMany(x => x.Tasks)
            .ToDictionary(c => c.Order.Id, c => c.StartAt, StringComparer.OrdinalIgnoreCase);

        var selectedIds = deltaByOrderId.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var backup = deltaByOrderId.Keys.ToDictionary(id => id, id => _orderShiftHours.GetValueOrDefault(id, 0.0), StringComparer.OrdinalIgnoreCase);

        try
        {
            foreach (var kv in deltaByOrderId)
                _orderShiftHours[kv.Key] = _orderShiftHours.GetValueOrDefault(kv.Key, 0.0) + kv.Value;

            SetBusyState(true, "推移重算中...", true, 0, 1);
            RebuildWithShifts();
            SetBusyState(false, "就緒", false, 1, 1);

            var changedUnselected = _lanes.SelectMany(x => x.Tasks)
                .Where(c => !selectedIds.Contains(c.Order.Id))
                .Any(c => before.TryGetValue(c.Order.Id, out var prev) && Math.Abs((c.StartAt - prev).TotalMinutes) > 0.01);

            if (changedUnselected)
                throw new InvalidOperationException("未選取的區塊發生位移，已取消本次移動。請調整目標時間或改用拖拉。 ");
        }
        catch (Exception ex)
        {
            foreach (var kv in backup)
                _orderShiftHours[kv.Key] = kv.Value;
            RebuildWithShifts();
            SetBusyState(false, "推移重算失敗", false, 0, 1);
            MessageBox.Show(ex.Message, "推移錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }


    private bool TryApplyFastSingleShift(string orderId, double deltaHours)
    {
        var card = _lanes.SelectMany(x => x.Tasks)
            .FirstOrDefault(c => string.Equals(c.Order.Id, orderId, StringComparison.OrdinalIgnoreCase));
        if (card is null || Math.Abs(deltaHours) < 0.0001)
            return false;

        var lane = _lanes.FirstOrDefault(x => x.Tasks.Contains(card));
        if (lane is null)
            return false;

        var desiredStart = SchedulerEngine.AlignToWorkingTime(card.StartAt.AddHours(deltaHours));
        var desiredEnd = SchedulerEngine.AddWorkingHours(desiredStart, card.Order.ProcessHours);

        var predecessorEnd = _lanes.SelectMany(x => x.Tasks)
            .Where(x => x.Order.JobId == card.Order.JobId && x.Order.Sequence < card.Order.Sequence)
            .Select(x => (DateTime?)x.EndAt)
            .DefaultIfEmpty()
            .Max();
        if (predecessorEnd is not null && desiredStart < predecessorEnd.Value)
        {
            MessageBox.Show("不可將此製程移到前段製程之前。", "製程序規則", MessageBoxButton.OK, MessageBoxImage.Warning);
            return true;
        }

        var hasOverlap = lane.Tasks
            .Where(x => !ReferenceEquals(x, card))
            .Any(x => desiredStart < x.EndAt && desiredEnd > x.StartAt);
        if (hasOverlap)
        {
            MessageBox.Show("目標時段與同機台其他工作重疊，請改用拖拉放置以觸發自動後推。", "推移限制", MessageBoxButton.OK, MessageBoxImage.Information);
            return true;
        }

        card.StartAt = desiredStart;
        card.EndAt = desiredEnd;
        ReinsertCardByStart(lane, card);
        PushLaterProcessesForSameJob(card);
        RefreshTimelineLayout(preserveRange: true);
        ApplySelectionAndHighlights(true);
        return true;
    }

    private TaskCard? GetCardFromContextMenuSender(object sender)
    {
        if (sender is not MenuItem menuItem)
            return null;

        var cm = FindOwningContextMenu(menuItem);
        if (cm is null)
            return null;

        return (cm.PlacementTarget as FrameworkElement)?.DataContext as TaskCard;
    }

    private static ContextMenu? FindOwningContextMenu(MenuItem menuItem)
    {
        object? current = menuItem.Parent;
        while (current is not null)
        {
            if (current is ContextMenu cm)
                return cm;

            if (current is MenuItem parentMenu)
            {
                current = parentMenu.Parent;
                continue;
            }

            break;
        }

        return null;
    }

    private void EnsureCardSelectedForContext(TaskCard card)
    {
        if (_selectedOrderIds.Contains(card.Order.Id))
            return;

        _selectedOrderIds.Clear();
        _selectedOrderIds.Add(card.Order.Id);
        _primaryOrderId = card.Order.Id;
        _selectedJobId = card.Order.JobId;
        ApplySelectionAndHighlights(true);
    }

    private async void AutoSchedule_Click(object sender, RoutedEventArgs e)
    {
        if (IsBusy) return;
        try
        {
            await ApplyAutoScheduleAsync("自動排程");
        }
        catch (Exception ex)
        {
            SetBusyState(false, "自動排程失敗", false, 0, 1);
            MessageBox.Show(ex.Message, "排程錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void Recalculate_Click(object sender, RoutedEventArgs e)
    {
        if (IsBusy) return;
        try
        {
            SetBusyState(true, "重算中...", true, 0, 1);
            await Task.Yield();
            RebuildWithShifts();
            SetBusyState(false, "重算完成", false, 1, 1);
        }
        catch (Exception ex)
        {
            SetBusyState(false, "重算失敗", false, 0, 1);
            MessageBox.Show(ex.Message, "重算錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LanesScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (_isSyncingScroll) return;

        _isSyncingScroll = true;
        if (Math.Abs(TimeAxisScrollViewer.VerticalOffset - e.VerticalOffset) > 0.5)
            TimeAxisScrollViewer.ScrollToVerticalOffset(e.VerticalOffset);
        if (Math.Abs(HeaderScrollViewer.HorizontalOffset - e.HorizontalOffset) > 0.5)
            HeaderScrollViewer.ScrollToHorizontalOffset(e.HorizontalOffset);
        _isSyncingScroll = false;
    }

    private void TimeAxisScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (_isSyncingScroll || Math.Abs(e.VerticalChange) < 0.1) return;
        _isSyncingScroll = true;
        LanesScrollViewer.ScrollToVerticalOffset(e.VerticalOffset);
        _isSyncingScroll = false;
    }

    private void TaskCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStart = e.GetPosition(LanesScrollViewer);
        if (sender is not FrameworkElement element || element.DataContext is not TaskCard card) return;

        _dragItem = card;
        var ctrlPressed = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;

        if (ctrlPressed)
        {
            if (_selectedOrderIds.Contains(card.Order.Id))
            {
                _selectedOrderIds.Remove(card.Order.Id);
                if (string.Equals(_primaryOrderId, card.Order.Id, StringComparison.OrdinalIgnoreCase))
                    _primaryOrderId = _selectedOrderIds.FirstOrDefault();
            }
            else
            {
                _selectedOrderIds.Add(card.Order.Id);
                _primaryOrderId = card.Order.Id;
            }
        }
        else
        {
            // Without Ctrl, fallback to single-select behavior.
            _selectedOrderIds.Clear();
            _selectedOrderIds.Add(card.Order.Id);
            _primaryOrderId = card.Order.Id;
        }

        _selectedJobId = card.Order.JobId;
        ApplySelectionAndHighlights(true);
        e.Handled = true;
    }

    private void LaneBackground_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is Border b && b.DataContext is MachineLane)
        {
            _selectedOrderIds.Clear();
            _primaryOrderId = null;
            _selectedJobId = null;
            ApplySelectionAndHighlights(true);
        }
    }

    private void TaskCard_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _dragItem is null) return;

        var position = e.GetPosition(LanesScrollViewer);
        if (Math.Abs(position.X - _dragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(position.Y - _dragStart.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;

        var dragCard = _dragItem;
        var data = new DataObject(typeof(TaskCard), dragCard);

        try
        {
            dragCard.IsDragging = true;
            StartDragPreview(dragCard);
            DragDrop.DoDragDrop((DependencyObject)sender, data, DragDropEffects.Move);
        }
        finally
        {
            StopDragPreview();
            dragCard.IsDragging = false;
            _dragItem = null;
        }
    }

    private void DragPreviewTimer_Tick(object? sender, EventArgs e)
    {
        UpdateDragPreviewPosition();
    }

    private void StartDragPreview(TaskCard card)
    {
        StopDragPreview();
        _dragPreview = new DragPreviewWindow(card);
        UpdateDragPreviewPosition();
        _dragPreview.Show();
        _dragPreviewTimer.Start();
    }

    private void StopDragPreview()
    {
        _dragPreviewTimer.Stop();
        if (_dragPreview is null)
            return;

        _dragPreview.Close();
        _dragPreview = null;
    }

    private void UpdateDragPreviewPosition()
    {
        if (_dragPreview is null)
            return;

        if (!NativeMethods.GetCursorPos(out var p))
            return;

        _dragPreview.Left = p.X + 16;
        _dragPreview.Top = p.Y + 16;
    }

    private void LaneCanvas_DragOver(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(typeof(TaskCard)) || sender is not FrameworkElement target || target.DataContext is not MachineLane)
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void LaneCanvas_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(typeof(TaskCard)) || sender is not FrameworkElement target || target.DataContext is not MachineLane targetLane)
            return;

        var card = (TaskCard)e.Data.GetData(typeof(TaskCard));
        var sourceLane = _lanes.FirstOrDefault(x => x.Tasks.Contains(card));
        if (sourceLane is null)
            return;

        var dropPoint = e.GetPosition(target);
        var desiredStart = SchedulerEngine.AlignToWorkingTime(_timelineStart.AddHours(Math.Max(0, dropPoint.Y / PixelsPerHour)));
        var originalIndex = sourceLane.Tasks.IndexOf(card);
        var originalMachineId = card.MachineId;
        var originalStart = card.StartAt;
        var originalEnd = card.EndAt;

        sourceLane.Tasks.Remove(card);
        card.MachineId = targetLane.Machine.Id;
        card.StartAt = desiredStart;
        card.EndAt = SchedulerEngine.AddWorkingHours(desiredStart, card.Order.ProcessHours);
        ReinsertCardByStart(targetLane, card);

        try
        {
            if (!TryPlaceCardWithLanePush(card, targetLane, desiredStart))
                throw new InvalidOperationException("製程序衝突：不可將同一張製卡的後段製程排在前段製程之前。");

            PushLaterProcessesForSameJob(card);
            RefreshTimelineLayout(preserveRange: true);
            _selectedJobId = card.Order.JobId;
            ApplySelectionAndHighlights(true);
            StatusText = "就緒";
        }
        catch (Exception ex)
        {
            targetLane.Tasks.Remove(card);
            sourceLane.Tasks.Insert(Math.Max(0, originalIndex), card);
            card.MachineId = originalMachineId;
            card.StartAt = originalStart;
            card.EndAt = originalEnd;
            StatusText = "拖拉調整失敗";
            MessageBox.Show(ex.Message, "重算錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private bool TryPlaceCardWithLanePush(TaskCard card, MachineLane targetLane, DateTime desiredStart)
    {
        var predecessorEnd = _lanes.SelectMany(x => x.Tasks)
            .Where(x => !ReferenceEquals(x, card) && x.Order.JobId == card.Order.JobId && x.Order.Sequence < card.Order.Sequence)
            .Select(x => (DateTime?)x.EndAt)
            .DefaultIfEmpty()
            .Max();
        if (predecessorEnd is not null && desiredStart < predecessorEnd.Value)
            return false;

        var normalizedStart = SchedulerEngine.AlignToWorkingTime(desiredStart);
        card.StartAt = normalizedStart;
        card.EndAt = SchedulerEngine.AddWorkingHours(normalizedStart, card.Order.ProcessHours);

        ShiftOverlappingTasksOnLane(targetLane, card, card.StartAt);
        return true;
    }

    private void PushLaterProcessesForSameJob(TaskCard movedCard)
    {
        if (string.IsNullOrWhiteSpace(movedCard.Order.JobId))
            return;

        var chain = _lanes.SelectMany(x => x.Tasks)
            .Where(x => x.Order.JobId == movedCard.Order.JobId)
            .OrderBy(x => x.Order.Sequence)
            .ThenBy(x => x.StartAt)
            .ToList();
        if (chain.Count <= 1)
            return;

        var anchorIndex = chain.FindIndex(x => string.Equals(x.Order.Id, movedCard.Order.Id, StringComparison.OrdinalIgnoreCase));
        if (anchorIndex < 0)
            return;

        var requiredStart = chain[anchorIndex].EndAt;
        for (var i = anchorIndex + 1; i < chain.Count; i++)
        {
            var successor = chain[i];
            if (successor.StartAt >= requiredStart)
            {
                requiredStart = successor.EndAt;
                continue;
            }

            var lane = _lanes.FirstOrDefault(x => x.Tasks.Contains(successor));
            if (lane is null)
                continue;

            var normalizedStart = SchedulerEngine.AlignToWorkingTime(requiredStart);
            successor.StartAt = normalizedStart;
            successor.EndAt = SchedulerEngine.AddWorkingHours(normalizedStart, successor.Order.ProcessHours);
            ReinsertCardByStart(lane, successor);
            ShiftOverlappingTasksOnLane(lane, successor, successor.StartAt);
            requiredStart = successor.EndAt;
        }
    }

    private static void ShiftOverlappingTasksOnLane(MachineLane lane, TaskCard pivot, DateTime affectedStart)
    {
        var trailing = lane.Tasks
            .Where(t => !ReferenceEquals(t, pivot) && t.EndAt > affectedStart)
            .OrderBy(t => t.StartAt)
            .ThenBy(t => t.Order.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var nextAvailable = pivot.EndAt;
        foreach (var task in trailing)
        {
            if (task.StartAt >= nextAvailable)
            {
                nextAvailable = task.EndAt;
                continue;
            }

            var normalizedStart = SchedulerEngine.AlignToWorkingTime(nextAvailable);
            task.StartAt = normalizedStart;
            task.EndAt = SchedulerEngine.AddWorkingHours(normalizedStart, task.Order.ProcessHours);
            nextAvailable = task.EndAt;
        }
    }

    private static void ReinsertCardByStart(MachineLane lane, TaskCard card)
    {
        var oldIndex = lane.Tasks.IndexOf(card);
        if (oldIndex >= 0)
            lane.Tasks.RemoveAt(oldIndex);

        var insertIndex = 0;
        while (insertIndex < lane.Tasks.Count)
        {
            var current = lane.Tasks[insertIndex];
            if (card.StartAt < current.StartAt)
                break;
            if (card.StartAt == current.StartAt && card.Order.Sequence < current.Order.Sequence)
                break;
            if (card.StartAt == current.StartAt && card.Order.Sequence == current.Order.Sequence
                && string.Compare(card.Order.Id, current.Order.Id, StringComparison.OrdinalIgnoreCase) < 0)
                break;
            insertIndex++;
        }

        lane.Tasks.Insert(insertIndex, card);
    }

    private static int GetInsertIndexByY(MachineLane lane, double y)
    {
        for (var i = 0; i < lane.Tasks.Count; i++)
        {
            var task = lane.Tasks[i];
            var middle = task.TopPx + task.HeightPx / 2.0;
            if (y < middle) return i;
        }

        return lane.Tasks.Count;
    }

    private IEnumerable<TaskCard> GetSelectedCards() => _lanes.SelectMany(x => x.Tasks).Where(c => _selectedOrderIds.Contains(c.Order.Id));

    private void NormalizeSelection()
    {
        var existingIds = _lanes.SelectMany(x => x.Tasks).Select(c => c.Order.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        _selectedOrderIds.RemoveWhere(id => !existingIds.Contains(id));

        if (_primaryOrderId is not null && !existingIds.Contains(_primaryOrderId))
            _primaryOrderId = null;

        if (_primaryOrderId is null)
            _primaryOrderId = _selectedOrderIds.FirstOrDefault();

        if (_primaryOrderId is null)
        {
            _selectedJobId = null;
            return;
        }

        var primary = _lanes.SelectMany(x => x.Tasks).FirstOrDefault(c => c.Order.Id == _primaryOrderId);
        _selectedJobId = primary?.Order.JobId;
    }

    private void ApplySelectionAndHighlights(bool forcePanelRefresh = false)
    {
        foreach (var card in _lanes.SelectMany(x => x.Tasks))
        {
            card.IsOperationSelected = _selectedOrderIds.Contains(card.Order.Id);
            card.IsPrimarySelected = _primaryOrderId is not null && string.Equals(card.Order.Id, _primaryOrderId, StringComparison.OrdinalIgnoreCase);
            card.IsRelatedSelected = !string.IsNullOrWhiteSpace(_selectedJobId) && card.Order.JobId == _selectedJobId;
        }

        if (forcePanelRefresh || !string.Equals(_lastPanelJobId, _selectedJobId, StringComparison.OrdinalIgnoreCase))
        {
            UpdateSelectedJobPanel(_selectedJobId);
            _lastPanelJobId = _selectedJobId;
        }
    }

    private void UpdateSelectedJobPanel(string? jobId)
    {
        _selectedJobLines.Clear();

        if (string.IsNullOrWhiteSpace(jobId))
        {
            _selectedJobLines.Add("尚未選取製卡。");
            return;
        }

        var items = _lanes.SelectMany(x => x.Tasks)
            .Where(x => x.Order.JobId == jobId)
            .OrderBy(x => x.Order.Sequence)
            .ToList();

        _selectedJobLines.Add($"{jobId} ({items.Count} processes)");
        _selectedJobLines.Add("--------------------------------");

        foreach (var item in items)
        {
            _selectedJobLines.Add($"{item.Order.ProcessCode} | Seq {item.Order.Sequence} | 圖號 {item.Order.PartNo} | 數量 {item.Order.Quantity:0.##}");
            _selectedJobLines.Add($"{item.MachineId} | {item.StartAt:MM-dd HH:mm} -> {item.EndAt:MM-dd HH:mm}");
            _selectedJobLines.Add(string.Empty);
        }
    }

    private static bool TryFindInvertedSequence(MachineLane lane, out (string JobId, string EarlierProcessCode, int EarlierSequence, string LaterProcessCode, int LaterSequence)? conflict)
    {
        foreach (var group in lane.Tasks.Where(t => !string.IsNullOrWhiteSpace(t.Order.JobId)).GroupBy(t => t.Order.JobId))
        {
            TaskCard? prev = null;
            foreach (var task in group)
            {
                if (prev is not null && task.Order.Sequence < prev.Order.Sequence)
                {
                    conflict = (task.Order.JobId, task.Order.ProcessCode, task.Order.Sequence, prev.Order.ProcessCode, prev.Order.Sequence);
                    return true;
                }
                prev = task;
            }
        }

        conflict = null;
        return false;
    }

    private sealed class DragPreviewWindow : Window
    {
        public DragPreviewWindow(TaskCard card)
        {
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ShowInTaskbar = false;
            Topmost = true;
            IsHitTestVisible = false;
            SizeToContent = SizeToContent.WidthAndHeight;

            Content = new Border
            {
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 8, 10, 8),
                Background = new SolidColorBrush(Color.FromArgb(235, 219, 234, 254)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(37, 99, 235)),
                BorderThickness = new Thickness(2),
                Effect = new DropShadowEffect
                {
                    BlurRadius = 14,
                    ShadowDepth = 2,
                    Opacity = 0.3,
                    Color = Colors.Black
                },
                Child = new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Text = card.Title, FontWeight = FontWeights.SemiBold },
                        new TextBlock { Text = card.Meta, FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(68, 68, 68)) },
                        new TextBlock { Text = card.Meta2, FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(68, 68, 68)) }
                    }
                }
            };
        }
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }
    }

    public sealed class TimeTick
    {
        public required DateTime Time { get; init; }
        public required string Label { get; init; }
        public required bool IsWorking { get; init; }
    }
}
























































































