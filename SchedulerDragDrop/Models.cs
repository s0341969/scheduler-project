using System.ComponentModel;

namespace SchedulerDragDrop;

public sealed class WorkOrder
{
    public required string Id { get; init; }
    public required string ProcessCode { get; init; }
    public required string MachineType { get; init; }
    public required double ProcessHours { get; init; }
    public int Priority { get; init; } = 3;
    public int SortOrder { get; init; } = int.MaxValue;
    public DateTime? DueAt { get; init; }
    public DateTime? ReleaseAt { get; init; }
    public IReadOnlyList<string> PreferredMachineIds { get; init; } = Array.Empty<string>();
    public string JobId { get; init; } = string.Empty;
    public int Sequence { get; init; } = 0;
    public string PartNo { get; init; } = string.Empty;
    public double Quantity { get; init; } = 0;
}

public sealed class Machine
{
    public required string Id { get; init; }
    public required string MachineType { get; init; }
    public required DateTime AvailableAt { get; init; }
}

public sealed class ScheduleItem
{
    public required string OrderId { get; init; }
    public required string MachineId { get; init; }
    public required DateTime StartAt { get; init; }
    public required DateTime EndAt { get; init; }
    public DateTime? DueAt { get; init; }
    public string JobId { get; init; } = string.Empty;
    public int Sequence { get; init; }
}

public sealed class TaskCard : INotifyPropertyChanged
{
    private string _machineId = string.Empty;
    private DateTime _startAt;
    private DateTime _endAt;
    private bool _isRelatedSelected;
    private bool _isOperationSelected;
    private bool _isPrimarySelected;
    private bool _isDragging;
    private double _topPx;
    private double _heightPx;

    public required WorkOrder Order { get; init; }

    public string MachineId
    {
        get => _machineId;
        set
        {
            if (_machineId == value) return;
            _machineId = value;
            OnPropertyChanged(nameof(MachineId));
        }
    }

    public DateTime StartAt
    {
        get => _startAt;
        set
        {
            if (_startAt == value) return;
            _startAt = value;
            OnPropertyChanged(nameof(StartAt));
            OnPropertyChanged(nameof(StartText));
        }
    }

    public DateTime EndAt
    {
        get => _endAt;
        set
        {
            if (_endAt == value) return;
            _endAt = value;
            OnPropertyChanged(nameof(EndAt));
            OnPropertyChanged(nameof(EndText));
        }
    }

    public bool IsRelatedSelected
    {
        get => _isRelatedSelected;
        set
        {
            if (_isRelatedSelected == value) return;
            _isRelatedSelected = value;
            OnPropertyChanged(nameof(IsRelatedSelected));
        }
    }

    public bool IsOperationSelected
    {
        get => _isOperationSelected;
        set
        {
            if (_isOperationSelected == value) return;
            _isOperationSelected = value;
            OnPropertyChanged(nameof(IsOperationSelected));
        }
    }

    public bool IsPrimarySelected
    {
        get => _isPrimarySelected;
        set
        {
            if (_isPrimarySelected == value) return;
            _isPrimarySelected = value;
            OnPropertyChanged(nameof(IsPrimarySelected));
        }
    }

    public bool IsDragging
    {
        get => _isDragging;
        set
        {
            if (_isDragging == value) return;
            _isDragging = value;
            OnPropertyChanged(nameof(IsDragging));
        }
    }

    public double TopPx
    {
        get => _topPx;
        set
        {
            if (Math.Abs(_topPx - value) < 0.01) return;
            _topPx = value;
            OnPropertyChanged(nameof(TopPx));
        }
    }

    public double HeightPx
    {
        get => _heightPx;
        set
        {
            if (Math.Abs(_heightPx - value) < 0.01) return;
            _heightPx = value;
            OnPropertyChanged(nameof(HeightPx));
        }
    }

    public string Title => $"{Order.JobId}-{Order.ProcessCode}";
    public string Meta => $"Seq {Order.Sequence} | 圖號 {Order.PartNo} | 數量 {Order.Quantity:0.##}";
    public string Meta2 => $"{Order.ProcessHours:0.#}h | 機台 {MachineId}";
    public string StartText => $"{StartAt:MM-dd HH:mm}";
    public string EndText => $"{EndAt:MM-dd HH:mm}";

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class MachineLane
{
    public required Machine Machine { get; init; }
    public BindingList<TaskCard> Tasks { get; } = new();

    public string Header => $"{Machine.Id} ({Machine.MachineType})";
}

public sealed class ProcessGroupOption : INotifyPropertyChanged
{
    private bool _isSelected;

    public required string Name { get; init; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public sealed class ScheduleGridRow
{
    public int Seq { get; init; }
    public string MachineId { get; init; } = string.Empty;
    public string JobId { get; init; } = string.Empty;
    public string ProcessCode { get; init; } = string.Empty;
    public string PartNo { get; init; } = string.Empty;
    public double Quantity { get; init; }
    public double WorkHours { get; init; }
    public DateTime StartAt { get; init; }
    public DateTime EndAt { get; init; }
}
