using System.ComponentModel;
using PUR2019.WinForms.Models;
using PUR2019.WinForms.Services;

namespace PUR2019.WinForms.Forms;

public sealed class PurchaseMainForm : Form
{
    private readonly IPurchaseOrderService _service;
    private readonly BindingSource _headerBinding = new();
    private readonly BindingSource _lineBinding = new();
    private readonly DateTimePicker _fromDate = new();
    private readonly DateTimePicker _toDate = new();
    private readonly TextBox _department = new();
    private readonly DateTimePicker _editDate = new();
    private readonly TextBox _editDepartment = new();
    private readonly TextBox _editBuyer = new();
    private readonly TextBox _orderNo = new();
    private readonly TextBox _statusCode = new();
    private readonly Label _summaryQty = new();
    private readonly Label _summaryAmount = new();
    private readonly string _currentUserId = Environment.UserName;

    public PurchaseMainForm(IPurchaseOrderService service)
    {
        _service = service;
        InitializeUi();
        LoadHeaders();
    }

    private void InitializeUi()
    {
        Text = "PUR2019F";
        Width = 1520;
        Height = 900;
        KeyPreview = true;
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Microsoft JhengHei UI", 9F);
        BackColor = Color.FromArgb(238, 229, 210);

        var commandBar = BuildCommandBar();
        commandBar.Location = new Point(0, 0);
        commandBar.Width = 1504;
        commandBar.Height = 54;

        var filterPanel = BuildFilterPanel();
        filterPanel.Location = new Point(8, 58);
        filterPanel.Width = 1488;
        filterPanel.Height = 96;

        var lineGrid = BuildLineGrid();
        lineGrid.Location = new Point(8, 162);
        lineGrid.Width = 1488;
        lineGrid.Height = 690;

        Controls.Add(commandBar);
        Controls.Add(filterPanel);
        Controls.Add(lineGrid);
    }

    private Panel BuildCommandBar()
    {
        var panel = new Panel
        {
            BackColor = Color.FromArgb(236, 236, 236),
            BorderStyle = BorderStyle.FixedSingle
        };

        var buttons = new[]
        {
            CreateCommandButton("F2新增", (_, _) => CreateHeader()),
            CreateCommandButton("F3查詢", (_, _) => LoadHeaders()),
            CreateCommandButton("F8執行", (_, _) => LoadHeaders()),
            CreateCommandButton("F5存檔", (_, _) => UpdateHeader()),
            CreateCommandButton("F9上一筆", (_, _) => MoveHeader(-1)),
            CreateCommandButton("F10下一筆", (_, _) => MoveHeader(1)),
            CreateCommandButton("新增單身", (_, _) => AddLine()),
            CreateCommandButton("刪除單身", (_, _) => DeleteLine()),
            CreateCommandButton("印表", (_, _) => MessageBox.Show(this, "報表流程尚未完成移植。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information)),
            CreateCommandButton("F11作廢", (_, _) => VoidHeader()),
            CreateCommandButton("F7取消", (_, _) => UnconfirmHeader()),
            CreateCommandButton("F12關閉", (_, _) => Close()),
            CreateCommandButton("邏輯覆蓋", (_, _) => ShowCoverageReport())
        };

        var x = 8;
        foreach (var button in buttons)
        {
            button.Location = new Point(x, 10);
            panel.Controls.Add(button);
            x += button.Width + 6;
        }

        return panel;
    }

    private Panel BuildFilterPanel()
    {
        var panel = new Panel
        {
            BackColor = Color.FromArgb(236, 229, 210),
            BorderStyle = BorderStyle.FixedSingle
        };

        _fromDate.Value = DateTime.Today.AddDays(-30);
        _fromDate.Format = DateTimePickerFormat.Short;
        _fromDate.Location = new Point(90, 8);
        _fromDate.Width = 130;

        _toDate.Value = DateTime.Today;
        _toDate.Format = DateTimePickerFormat.Short;
        _toDate.Location = new Point(230, 8);
        _toDate.Width = 130;

        _department.Location = new Point(370, 8);
        _department.Width = 80;

        _editBuyer.Location = new Point(560, 8);
        _editBuyer.Width = 90;

        _editDepartment.Location = new Point(660, 8);
        _editDepartment.Width = 90;

        _orderNo.Location = new Point(850, 8);
        _orderNo.Width = 120;
        _orderNo.ReadOnly = true;
        _orderNo.BackColor = Color.WhiteSmoke;

        _editDate.Format = DateTimePickerFormat.Short;
        _editDate.Location = new Point(980, 8);
        _editDate.Width = 120;

        _statusCode.Location = new Point(1110, 8);
        _statusCode.Width = 50;
        _statusCode.ReadOnly = true;
        _statusCode.BackColor = Color.WhiteSmoke;

        _summaryQty.Location = new Point(90, 58);
        _summaryQty.AutoSize = true;
        _summaryQty.ForeColor = Color.DarkBlue;
        _summaryQty.Text = "總數量: 0";

        _summaryAmount.Location = new Point(260, 58);
        _summaryAmount.AutoSize = true;
        _summaryAmount.ForeColor = Color.DarkBlue;
        _summaryAmount.Text = "總金額: 0";

        panel.Controls.Add(new Label { Text = "採購日期", Location = new Point(16, 12), AutoSize = true, ForeColor = Color.Navy });
        panel.Controls.Add(new Label { Text = "~", Location = new Point(222, 12), AutoSize = true, ForeColor = Color.Navy });
        panel.Controls.Add(new Label { Text = "部門", Location = new Point(340, 12), AutoSize = true, ForeColor = Color.Navy });
        panel.Controls.Add(new Label { Text = "請購人員", Location = new Point(500, 12), AutoSize = true, ForeColor = Color.Navy });
        panel.Controls.Add(new Label { Text = "扣預算部門", Location = new Point(660, 40), AutoSize = true, ForeColor = Color.Navy });
        panel.Controls.Add(new Label { Text = "請購單號", Location = new Point(790, 12), AutoSize = true, ForeColor = Color.Navy });
        panel.Controls.Add(new Label { Text = "建立日期", Location = new Point(980, 40), AutoSize = true, ForeColor = Color.Navy });
        panel.Controls.Add(new Label { Text = "控制碼", Location = new Point(1110, 40), AutoSize = true, ForeColor = Color.Navy });
        panel.Controls.Add(new Label { Text = $"使用者: {_currentUserId}", Location = new Point(1210, 12), AutoSize = true, ForeColor = Color.Brown });

        panel.Controls.Add(_fromDate);
        panel.Controls.Add(_toDate);
        panel.Controls.Add(_department);
        panel.Controls.Add(_editBuyer);
        panel.Controls.Add(_editDepartment);
        panel.Controls.Add(_orderNo);
        panel.Controls.Add(_editDate);
        panel.Controls.Add(_statusCode);
        panel.Controls.Add(_summaryQty);
        panel.Controls.Add(_summaryAmount);

        return panel;
    }

    private DataGridView BuildLineGrid()
    {
        var grid = new DataGridView
        {
            AutoGenerateColumns = false,
            DataSource = _lineBinding,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            BackgroundColor = Color.FromArgb(245, 238, 224),
            GridColor = Color.SaddleBrown,
            BorderStyle = BorderStyle.FixedSingle
        };

        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "序號", DataPropertyName = nameof(PurchaseOrderLine.Sequence), Width = 50 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "採購單號", DataPropertyName = nameof(PurchaseOrderLine.OrderNo), Width = 120 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "製令單號", DataPropertyName = nameof(PurchaseOrderLine.SourceOrderNo), Width = 130 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "起始製程", DataPropertyName = nameof(PurchaseOrderLine.ProcessFrom), Width = 70 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "結束製程", DataPropertyName = nameof(PurchaseOrderLine.ProcessTo), Width = 70 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "料號", DataPropertyName = nameof(PurchaseOrderLine.ItemNo), Width = 120 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "品名/材質", DataPropertyName = nameof(PurchaseOrderLine.ItemName), Width = 280 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "數量", DataPropertyName = nameof(PurchaseOrderLine.Quantity), Width = 80, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "MOQ", DataPropertyName = nameof(PurchaseOrderLine.MinimumOrderQty), Width = 60, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "單價", DataPropertyName = nameof(PurchaseOrderLine.UnitPrice), Width = 80, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "金額", DataPropertyName = nameof(PurchaseOrderLine.Amount), Width = 90, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "預估成本", DataPropertyName = nameof(PurchaseOrderLine.ReferenceAmount), Width = 90, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "成本比", DataPropertyName = nameof(PurchaseOrderLine.CostRatio), Width = 70, DefaultCellStyle = new DataGridViewCellStyle { Format = "N3", Alignment = DataGridViewContentAlignment.MiddleRight } });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "需求日", DataPropertyName = nameof(PurchaseOrderLine.DueDate), Width = 90, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy/MM/dd" } });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "狀態", DataPropertyName = nameof(PurchaseOrderLine.StatusCode), Width = 60 });

        return grid;
    }

    private static Button CreateCommandButton(string text, EventHandler clickHandler)
    {
        var button = new Button
        {
            Text = text,
            Width = 90,
            Height = 30,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(248, 248, 248)
        };
        button.Click += clickHandler;
        return button;
    }

    private void LoadHeaders(string? focusOrderNo = null)
    {
        try
        {
            var headers = _service.QueryHeaders(_fromDate.Value.Date, _toDate.Value.Date, _department.Text);
            _headerBinding.DataSource = new BindingList<PurchaseOrderHeader>(headers.ToList());

            if (!string.IsNullOrWhiteSpace(focusOrderNo))
            {
                for (var i = 0; i < _headerBinding.Count; i++)
                {
                    if (_headerBinding[i] is PurchaseOrderHeader h && h.OrderNo == focusOrderNo)
                    {
                        _headerBinding.Position = i;
                        break;
                    }
                }
            }

            if (_headerBinding.Count == 0)
            {
                _lineBinding.DataSource = new BindingList<PurchaseOrderLine>();
                _orderNo.Clear();
                _statusCode.Clear();
                _summaryQty.Text = "總數量: 0";
                _summaryAmount.Text = "總金額: 0";
                return;
            }

            if (_headerBinding.Current is not PurchaseOrderHeader first)
            {
                first = (PurchaseOrderHeader)_headerBinding[0]!;
                _headerBinding.Position = 0;
            }

            BindHeader(first);
            LoadLinesForSelectedOrder(first.OrderNo);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "查詢失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void LoadLinesForSelectedOrder(string orderNo)
    {
        var lines = _service.QueryLines(orderNo);
        _lineBinding.DataSource = new BindingList<PurchaseOrderLine>(lines.ToList());
        _summaryQty.Text = $"總數量: {lines.Sum(x => x.Quantity):N2}";
        _summaryAmount.Text = $"總金額: {lines.Sum(x => x.Amount):N2}";
    }

    private void BindHeader(PurchaseOrderHeader header)
    {
        _orderNo.Text = header.OrderNo;
        _statusCode.Text = header.StatusCode;
        _editDate.Value = header.OrderDate.Date;
        _editDepartment.Text = header.Department;
        _editBuyer.Text = header.Buyer;
    }

    private void CreateHeader()
    {
        using var dialog = new PurchaseOrderHeaderEditForm("新增單頭", DateTime.Today, string.Empty, _currentUserId);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        ExecuteAction(() =>
        {
            var created = _service.CreateOrder(new CreatePurchaseOrderRequest
            {
                OrderDate = dialog.OrderDate,
                Department = dialog.Department,
                Buyer = dialog.Buyer,
                UserId = _currentUserId
            });
            LoadHeaders(created.OrderNo);
        }, "新增單頭成功");
    }

    private void UpdateHeader()
    {
        var selected = GetSelectedHeader();
        if (selected is null)
        {
            return;
        }

        ExecuteAction(() =>
        {
            _service.UpdateOrder(new UpdatePurchaseOrderRequest
            {
                OrderNo = selected.OrderNo,
                OrderDate = _editDate.Value.Date,
                Department = _editDepartment.Text,
                Buyer = _editBuyer.Text,
                UserId = _currentUserId
            });
            LoadHeaders(selected.OrderNo);
        }, "儲存單頭成功");
    }

    private void DeleteHeader()
    {
        var selected = GetSelectedHeader();
        if (selected is null)
        {
            return;
        }

        if (MessageBox.Show(this, $"確定要刪除單頭 {selected.OrderNo} 嗎？", "確認", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        ExecuteAction(() =>
        {
            _service.DeleteOrder(selected.OrderNo);
            LoadHeaders();
        }, "刪除單頭成功");
    }

    private void ConfirmHeader()
    {
        var selected = GetSelectedHeader();
        if (selected is null)
        {
            return;
        }

        ExecuteAction(() =>
        {
            _service.ConfirmOrder(selected.OrderNo, _currentUserId);
            LoadHeaders(selected.OrderNo);
        }, "確認成功");
    }

    private void UnconfirmHeader()
    {
        var selected = GetSelectedHeader();
        if (selected is null)
        {
            return;
        }

        ExecuteAction(() =>
        {
            _service.UnconfirmOrder(selected.OrderNo, _currentUserId);
            LoadHeaders(selected.OrderNo);
        }, "取消確認成功");
    }

    private void VoidHeader()
    {
        var selected = GetSelectedHeader();
        if (selected is null)
        {
            return;
        }

        if (MessageBox.Show(this, $"作廢會刪除單身資料，確定作廢 {selected.OrderNo} 嗎？", "確認", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
        {
            return;
        }

        ExecuteAction(() =>
        {
            _service.VoidOrder(selected.OrderNo, _currentUserId);
            LoadHeaders(selected.OrderNo);
        }, "作廢成功");
    }

    private void MoveHeader(int delta)
    {
        if (_headerBinding.Count == 0)
        {
            return;
        }

        var target = Math.Clamp(_headerBinding.Position + delta, 0, _headerBinding.Count - 1);
        _headerBinding.Position = target;
        if (_headerBinding.Current is PurchaseOrderHeader selected)
        {
            BindHeader(selected);
            LoadLinesForSelectedOrder(selected.OrderNo);
        }
    }

    private void AddLine()
    {
        var selected = GetSelectedHeader();
        if (selected is null)
        {
            return;
        }

        using var dialog = new PurchaseOrderLineEditForm();
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        ExecuteAction(() =>
        {
            _service.AddLine(new CreatePurchaseOrderLineRequest
            {
                OrderNo = selected.OrderNo,
                ItemNo = dialog.ItemNo,
                ItemName = dialog.ItemName,
                SourceOrderNo = dialog.SourceOrderNo,
                ProcessFrom = dialog.ProcessFrom,
                ProcessTo = dialog.ProcessTo,
                Quantity = dialog.Quantity,
                UnitPrice = dialog.UnitPrice,
                DueDate = dialog.DueDate,
                UserId = _currentUserId
            });
            LoadHeaders(selected.OrderNo);
        }, "新增單身成功");
    }

    private void DeleteLine()
    {
        var selectedHeader = GetSelectedHeader();
        if (selectedHeader is null)
        {
            return;
        }

        if (_lineBinding.Current is not PurchaseOrderLine line)
        {
            MessageBox.Show(this, "請先選擇單身資料。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (MessageBox.Show(this, $"確定刪除單身序號 {line.Sequence:000} 嗎？", "確認", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        ExecuteAction(() =>
        {
            _service.DeleteLine(selectedHeader.OrderNo, line.Sequence);
            LoadHeaders(selectedHeader.OrderNo);
        }, "刪除單身成功");
    }

    private PurchaseOrderHeader? GetSelectedHeader()
    {
        if (_headerBinding.Current is not PurchaseOrderHeader selected)
        {
            MessageBox.Show(this, "請先執行查詢並選擇採購單。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return null;
        }

        return selected;
    }

    private void ExecuteAction(Action action, string successMessage)
    {
        try
        {
            action();
            MessageBox.Show(this, successMessage, "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "操作失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShowCoverageReport()
    {
        var text = BusinessLogicCoverageService.BuildReportText();
        MessageBox.Show(this, text, "商業邏輯覆蓋檢查", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            case Keys.F2:
                CreateHeader();
                return true;
            case Keys.F3:
                LoadHeaders();
                return true;
            case Keys.F5:
                UpdateHeader();
                return true;
            case Keys.F7:
                UnconfirmHeader();
                return true;
            case Keys.F8:
                LoadHeaders();
                return true;
            case Keys.F9:
                MoveHeader(-1);
                return true;
            case Keys.F10:
                MoveHeader(1);
                return true;
            case Keys.F11:
                VoidHeader();
                return true;
            case Keys.F12:
                Close();
                return true;
            default:
                return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
