using AutoPcScheduler.Services;
using System.Globalization;

namespace AutoPcScheduler.UI;

public sealed class SchedulerMainForm : Form
{
    private readonly TextBox _connectionTextBox = new() { Anchor = AnchorStyles.Left | AnchorStyles.Right, Width = 700 };
    private readonly DateTimePicker _planDatePicker = new()
    {
        Anchor = AnchorStyles.Left,
        Width = 130,
        Format = DateTimePickerFormat.Custom,
        CustomFormat = "yyyy-MM-dd",
        Value = DateTime.Today
    };
    private readonly NumericUpDown _horizonDaysBox = new()
    {
        Anchor = AnchorStyles.Left,
        Width = 70,
        Minimum = 1,
        Maximum = 60,
        Value = 7
    };
    private readonly TextBox _assignerTextBox = new() { Anchor = AnchorStyles.Left | AnchorStyles.Right, Width = 120 };
    private readonly CheckBox _dryRunCheckBox = new() { Anchor = AnchorStyles.Left, AutoSize = true, Checked = true, Text = "僅試排(不寫入)" };
    private readonly Button _startButton = new() { Anchor = AnchorStyles.Left, AutoSize = true, Text = "開始排程" };

    private readonly Label _methodLabel = new()
    {
        AutoSize = true,
        Text = "排程方式：貪婪法（優先可用工時→交期→工時，候選機台取最早完工，可跨天切段）"
    };
    private readonly Label _workCountLabel = new() { AutoSize = true, Text = "待排工作：0 筆" };
    private readonly ProgressBar _progressBar = new() { Width = 280, Height = 18, Minimum = 0, Maximum = 1, Value = 0 };
    private readonly Label _progressLabel = new() { AutoSize = true, Text = "進度：0/0" };

    private readonly TextBox _machineFilterTextBox = new() { Width = 100 };
    private readonly TextBox _partNoFilterTextBox = new() { Width = 100 };
    private readonly TextBox _orderFilterTextBox = new() { Width = 140 };
    private readonly TextBox _processFilterTextBox = new() { Width = 100 };
    private readonly Button _applyFilterButton = new() { AutoSize = true, Text = "查詢" };
    private readonly Button _clearFilterButton = new() { AutoSize = true, Text = "清除" };
    private readonly Label _filterSummaryLabel = new() { AutoSize = true, Text = "顯示 0/0 筆" };

    private readonly DataGridView _assignmentGrid;
    private readonly DataGridView _unscheduledGrid;
    private readonly Label _summaryLabel = new() { Dock = DockStyle.Fill, AutoSize = true, Text = "請先按「開始排程」。", Padding = new Padding(0, 8, 0, 0) };

    private List<AssignmentRow> _allAssignmentRows = [];
    private List<UnscheduledRow> _allUnscheduledRows = [];

    public SchedulerMainForm()
    {
        Text = "AutoPC 自動排程";
        Width = 1450;
        Height = 900;
        StartPosition = FormStartPosition.CenterScreen;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(12)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        root.Controls.Add(BuildTopPanel(), 0, 0);

        _assignmentGrid = BuildAssignmentGrid();
        _unscheduledGrid = BuildUnscheduledGrid();

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 520
        };
        split.Panel1.Controls.Add(_assignmentGrid);
        split.Panel2.Controls.Add(_unscheduledGrid);
        root.Controls.Add(split, 0, 1);

        root.Controls.Add(_summaryLabel, 0, 2);
        Controls.Add(root);

        _connectionTextBox.Text = Environment.GetEnvironmentVariable("AUTO_PC_CONN") ?? string.Empty;
        _assignerTextBox.Text = string.IsNullOrWhiteSpace(Environment.UserName) ? "AutoPc" : Environment.UserName;

        _startButton.Click += StartButton_Click;
        _applyFilterButton.Click += (_, _) => ApplyAssignmentFilter();
        _clearFilterButton.Click += (_, _) => ClearAssignmentFilter();

        _machineFilterTextBox.KeyDown += FilterTextBox_KeyDown;
        _partNoFilterTextBox.KeyDown += FilterTextBox_KeyDown;
        _orderFilterTextBox.KeyDown += FilterTextBox_KeyDown;
        _processFilterTextBox.KeyDown += FilterTextBox_KeyDown;
    }

    private TableLayoutPanel BuildTopPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 3
        };

        panel.Controls.Add(BuildParameterRow(), 0, 0);
        panel.Controls.Add(BuildStatusRow(), 0, 1);
        panel.Controls.Add(BuildFilterRow(), 0, 2);

        return panel;
    }

    private TableLayoutPanel BuildParameterRow()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 12,
            RowCount = 1
        };

        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 12));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 12));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 12));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        panel.Controls.Add(new Label { AutoSize = true, Anchor = AnchorStyles.Left, Text = "連線字串(AUTO_PC_CONN)" }, 0, 0);
        panel.Controls.Add(_connectionTextBox, 1, 0);
        panel.Controls.Add(new Label { AutoSize = true, Text = string.Empty }, 2, 0);

        panel.Controls.Add(new Label { AutoSize = true, Anchor = AnchorStyles.Left, Text = "排程起日" }, 3, 0);
        panel.Controls.Add(_planDatePicker, 4, 0);
        panel.Controls.Add(new Label { AutoSize = true, Text = string.Empty }, 5, 0);

        panel.Controls.Add(new Label { AutoSize = true, Anchor = AnchorStyles.Left, Text = "視窗天數" }, 6, 0);
        panel.Controls.Add(_horizonDaysBox, 7, 0);
        panel.Controls.Add(new Label { AutoSize = true, Text = string.Empty }, 8, 0);

        panel.Controls.Add(new Label { AutoSize = true, Anchor = AnchorStyles.Left, Text = "指派人" }, 9, 0);
        panel.Controls.Add(_assignerTextBox, 10, 0);

        var actionPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(8, 0, 0, 0)
        };
        actionPanel.Controls.Add(_dryRunCheckBox);
        actionPanel.Controls.Add(_startButton);
        panel.Controls.Add(actionPanel, 11, 0);

        return panel;
    }

    private TableLayoutPanel BuildStatusRow()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 4,
            Margin = new Padding(0, 6, 0, 0)
        };

        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        panel.Controls.Add(_methodLabel, 0, 0);
        panel.Controls.Add(_workCountLabel, 1, 0);
        panel.Controls.Add(_progressBar, 2, 0);
        panel.Controls.Add(_progressLabel, 3, 0);

        return panel;
    }

    private FlowLayoutPanel BuildFilterRow()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(0, 6, 0, 0)
        };

        panel.Controls.Add(new Label { AutoSize = true, Text = "查詢：機台" });
        panel.Controls.Add(_machineFilterTextBox);
        panel.Controls.Add(new Label { AutoSize = true, Text = "圖號" });
        panel.Controls.Add(_partNoFilterTextBox);
        panel.Controls.Add(new Label { AutoSize = true, Text = "製卡" });
        panel.Controls.Add(_orderFilterTextBox);
        panel.Controls.Add(new Label { AutoSize = true, Text = "製程" });
        panel.Controls.Add(_processFilterTextBox);
        panel.Controls.Add(_applyFilterButton);
        panel.Controls.Add(_clearFilterButton);
        panel.Controls.Add(_filterSummaryLabel);

        return panel;
    }

    private static DataGridView BuildAssignmentGrid()
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false
        };

        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "#", DataPropertyName = nameof(AssignmentRow.Seq), Width = 50 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "機台", DataPropertyName = nameof(AssignmentRow.MachineId), Width = 120 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "製卡", DataPropertyName = nameof(AssignmentRow.OrderNo), Width = 180 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "製程", DataPropertyName = nameof(AssignmentRow.ProcessCode), Width = 120 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "製程名稱", DataPropertyName = nameof(AssignmentRow.ProcessName), Width = 160 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "圖號", DataPropertyName = nameof(AssignmentRow.PartNo), Width = 130 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "開始", DataPropertyName = nameof(AssignmentRow.StartTime), Width = 140 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "結束", DataPropertyName = nameof(AssignmentRow.EndTime), Width = 140 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "工時", DataPropertyName = nameof(AssignmentRow.WorkHours), Width = 90 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "內容", DataPropertyName = nameof(AssignmentRow.Content), AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });

        return grid;
    }

    private static DataGridView BuildUnscheduledGrid()
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            RowHeadersVisible = false
        };

        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "製卡", DataPropertyName = nameof(UnscheduledRow.OrderNo), Width = 180 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "製程", DataPropertyName = nameof(UnscheduledRow.ProcessCode), Width = 120 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "需求工時", DataPropertyName = nameof(UnscheduledRow.RequiredHours), Width = 100 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "原因", DataPropertyName = nameof(UnscheduledRow.Reason), AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });

        return grid;
    }

    private async void StartButton_Click(object? sender, EventArgs e)
    {
        await RunScheduleAsync();
    }

    private async Task RunScheduleAsync()
    {
        var connectionString = _connectionTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            MessageBox.Show("請輸入連線字串，或先設定 AUTO_PC_CONN。", "缺少連線字串", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var planDate = DateOnly.FromDateTime(_planDatePicker.Value.Date);
        var horizonDays = Convert.ToInt32(_horizonDaysBox.Value, CultureInfo.InvariantCulture);
        var assigner = string.IsNullOrWhiteSpace(_assignerTextBox.Text) ? "AutoPc" : _assignerTextBox.Text.Trim();

        ToggleBusy(true);
        _summaryLabel.Text = "讀取資料中，請稍候...";

        try
        {
            var repository = new SqlSchedulingRepository(connectionString);
            var scheduler = new AutoPcSchedulerEngine();

            var context = await repository.LoadSchedulingContextAsync(planDate, horizonDays, CancellationToken.None);
            _workCountLabel.Text = $"待排工作：{context.Works.Count} 筆";
            ResetProgress(context.Works.Count);

            _summaryLabel.Text = "排程中，請稍候...";
            var progress = new Progress<SchedulingProgress>(UpdateProgress);
            var result = await Task.Run(() => scheduler.Schedule(context, planDate, horizonDays, assigner, progress));

            _allAssignmentRows = result.Assignments
                .OrderBy(x => x.StartTime)
                .ThenBy(x => x.MachineId, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.OrdTp, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.OrdNo, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.OrdSq)
                .ThenBy(x => x.OrdSq1)
                .Select((x, index) => new AssignmentRow
                {
                    Seq = index + 1,
                    MachineId = x.MachineId,
                    OrderNo = $"{x.OrdTp}-{x.OrdNo}-{x.OrdSq}-{x.OrdSq1}",
                    ProcessCode = x.ProcessCode ?? string.Empty,
                    ProcessName = x.ProductName ?? string.Empty,
                    PartNo = x.InPart ?? string.Empty,
                    StartTime = x.StartTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                    EndTime = x.EndTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                    WorkHours = x.WorkHours,
                    Content = x.Content
                })
                .ToList();

            _allUnscheduledRows = result.Unscheduled
                .Select(x => new UnscheduledRow
                {
                    OrderNo = $"{x.Work.OrdTp}-{x.Work.OrdNo}-{x.Work.OrdSq}-{x.Work.OrdSq1}",
                    ProcessCode = x.Work.ProcessCode ?? string.Empty,
                    RequiredHours = x.Work.RequiredHours,
                    Reason = x.Reason
                })
                .ToList();

            ApplyAssignmentFilter();
            _unscheduledGrid.DataSource = _allUnscheduledRows.ToList();

            var saveMessage = "（僅試排，未寫入資料庫）";
            if (!_dryRunCheckBox.Checked)
            {
                var insertedRows = await repository.SaveAssignmentsAsync(result.Assignments, CancellationToken.None);
                saveMessage = $"（已寫入資料庫 {insertedRows} 筆）";
            }

            _summaryLabel.Text = $"排程完成：新排程 {_allAssignmentRows.Count} 筆，未排入 {_allUnscheduledRows.Count} 筆 {saveMessage}";
        }
        catch (Exception ex)
        {
            _summaryLabel.Text = "排程失敗。";
            MessageBox.Show(ex.Message, "排程錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            ToggleBusy(false);
        }
    }

    private void ResetProgress(int totalWorks)
    {
        var total = Math.Max(1, totalWorks);
        _progressBar.Minimum = 0;
        _progressBar.Maximum = total;
        _progressBar.Value = 0;
        _progressLabel.Text = totalWorks <= 0
            ? "進度：0/0"
            : $"進度：0/{totalWorks}";
    }

    private void UpdateProgress(SchedulingProgress progress)
    {
        var total = Math.Max(1, progress.TotalWorks);
        if (_progressBar.Maximum != total)
        {
            _progressBar.Maximum = total;
        }

        var value = Math.Clamp(progress.ProcessedWorks, 0, total);
        _progressBar.Value = value;

        var current = string.IsNullOrWhiteSpace(progress.CurrentOrderNo)
            ? string.Empty
            : $" | 目前：{progress.CurrentOrderNo}";

        _progressLabel.Text = $"進度：{progress.ProcessedWorks}/{progress.TotalWorks}（已排 {progress.ScheduledWorks}，未排 {progress.UnscheduledWorks}）{current}";
    }

    private void FilterTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter)
        {
            return;
        }

        e.SuppressKeyPress = true;
        ApplyAssignmentFilter();
    }

    private void ClearAssignmentFilter()
    {
        _machineFilterTextBox.Text = string.Empty;
        _partNoFilterTextBox.Text = string.Empty;
        _orderFilterTextBox.Text = string.Empty;
        _processFilterTextBox.Text = string.Empty;
        ApplyAssignmentFilter();
    }

    private void ApplyAssignmentFilter()
    {
        var machine = _machineFilterTextBox.Text.Trim();
        var partNo = _partNoFilterTextBox.Text.Trim();
        var orderNo = _orderFilterTextBox.Text.Trim();
        var process = _processFilterTextBox.Text.Trim();

        var filtered = _allAssignmentRows
            .Where(x => ContainsFilter(x.MachineId, machine))
            .Where(x => ContainsFilter(x.PartNo, partNo))
            .Where(x => ContainsFilter(x.OrderNo, orderNo))
            .Where(x => string.IsNullOrWhiteSpace(process) || ContainsFilter(x.ProcessCode, process) || ContainsFilter(x.ProcessName, process))
            .ToList();

        _assignmentGrid.DataSource = filtered;
        _filterSummaryLabel.Text = $"顯示 {filtered.Count}/{_allAssignmentRows.Count} 筆";
    }

    private static bool ContainsFilter(string source, string filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return true;
        }

        return source.Contains(filter, StringComparison.OrdinalIgnoreCase);
    }

    private void ToggleBusy(bool busy)
    {
        _startButton.Enabled = !busy;
        _applyFilterButton.Enabled = !busy;
        _clearFilterButton.Enabled = !busy;
        UseWaitCursor = busy;
    }

    private sealed class AssignmentRow
    {
        public int Seq { get; init; }

        public string MachineId { get; init; } = string.Empty;

        public string OrderNo { get; init; } = string.Empty;

        public string ProcessCode { get; init; } = string.Empty;

        public string ProcessName { get; init; } = string.Empty;

        public string PartNo { get; init; } = string.Empty;

        public string StartTime { get; init; } = string.Empty;

        public string EndTime { get; init; } = string.Empty;

        public decimal WorkHours { get; init; }

        public string Content { get; init; } = string.Empty;
    }

    private sealed class UnscheduledRow
    {
        public string OrderNo { get; init; } = string.Empty;

        public string ProcessCode { get; init; } = string.Empty;

        public decimal RequiredHours { get; init; }

        public string Reason { get; init; } = string.Empty;
    }
}



