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
    private readonly DataGridView _assignmentGrid;
    private readonly DataGridView _unscheduledGrid;
    private readonly Label _summaryLabel = new() { Dock = DockStyle.Fill, AutoSize = true, Text = "請先按「開始排程」。", Padding = new Padding(0, 8, 0, 0) };

    public SchedulerMainForm()
    {
        Text = "AutoPC 自動排程";
        Width = 1400;
        Height = 860;
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

        root.Controls.Add(BuildConfigPanel(), 0, 0);

        _assignmentGrid = BuildAssignmentGrid();
        _unscheduledGrid = BuildUnscheduledGrid();

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 500
        };
        split.Panel1.Controls.Add(_assignmentGrid);
        split.Panel2.Controls.Add(_unscheduledGrid);
        root.Controls.Add(split, 0, 1);

        root.Controls.Add(_summaryLabel, 0, 2);

        Controls.Add(root);

        _connectionTextBox.Text = Environment.GetEnvironmentVariable("AUTO_PC_CONN") ?? string.Empty;
        _assignerTextBox.Text = string.IsNullOrWhiteSpace(Environment.UserName) ? "AutoPc" : Environment.UserName;

        _startButton.Click += StartButton_Click;
    }

    private TableLayoutPanel BuildConfigPanel()
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
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "工單", DataPropertyName = nameof(AssignmentRow.OrderNo), Width = 180 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "製程", DataPropertyName = nameof(AssignmentRow.ProcessCode), Width = 120 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "料號", DataPropertyName = nameof(AssignmentRow.PartNo), Width = 130 });
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

        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "工單", DataPropertyName = nameof(UnscheduledRow.OrderNo), Width = 180 });
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
        _summaryLabel.Text = "排程中，請稍候...";

        try
        {
            var repository = new SqlSchedulingRepository(connectionString);
            var scheduler = new AutoPcSchedulerEngine();

            var context = await repository.LoadSchedulingContextAsync(planDate, horizonDays, CancellationToken.None);
            var result = scheduler.Schedule(context, planDate, horizonDays, assigner);

            var assignmentRows = result.Assignments
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
                    PartNo = x.InPart ?? string.Empty,
                    StartTime = x.StartTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                    EndTime = x.EndTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                    WorkHours = x.WorkHours,
                    Content = x.Content
                })
                .ToList();

            var unscheduledRows = result.Unscheduled
                .Select(x => new UnscheduledRow
                {
                    OrderNo = $"{x.Work.OrdTp}-{x.Work.OrdNo}-{x.Work.OrdSq}-{x.Work.OrdSq1}",
                    ProcessCode = x.Work.ProcessCode ?? string.Empty,
                    RequiredHours = x.Work.RequiredHours,
                    Reason = x.Reason
                })
                .ToList();

            _assignmentGrid.DataSource = assignmentRows;
            _unscheduledGrid.DataSource = unscheduledRows;

            var saveMessage = "（僅試排，未寫入資料庫）";
            if (!_dryRunCheckBox.Checked)
            {
                var insertedRows = await repository.SaveAssignmentsAsync(result.Assignments, CancellationToken.None);
                saveMessage = $"（已寫入資料庫 {insertedRows} 筆）";
            }

            _summaryLabel.Text = $"排程完成：新排程 {assignmentRows.Count} 筆，未排入 {unscheduledRows.Count} 筆 {saveMessage}";
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

    private void ToggleBusy(bool busy)
    {
        _startButton.Enabled = !busy;
        UseWaitCursor = busy;
    }

    private sealed class AssignmentRow
    {
        public int Seq { get; init; }

        public string MachineId { get; init; } = string.Empty;

        public string OrderNo { get; init; } = string.Empty;

        public string ProcessCode { get; init; } = string.Empty;

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
