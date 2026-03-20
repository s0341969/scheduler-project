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
    private readonly string _currentUserId = Environment.UserName;

    public PurchaseMainForm(IPurchaseOrderService service)
    {
        _service = service;
        InitializeUi();
        LoadHeaders();
    }

    private void InitializeUi()
    {
        Text = "PUR2019F 採購單作業";
        Width = 1280;
        Height = 820;
        StartPosition = FormStartPosition.CenterParent;

        _fromDate.Value = DateTime.Today.AddDays(-30);
        _fromDate.Format = DateTimePickerFormat.Short;
        _fromDate.Location = new Point(80, 12);

        _toDate.Value = DateTime.Today;
        _toDate.Format = DateTimePickerFormat.Short;
        _toDate.Location = new Point(250, 12);

        _department.PlaceholderText = "部門代碼";
        _department.Location = new Point(420, 12);
        _department.Width = 120;

        var queryButton = new Button { Text = "查詢", Location = new Point(560, 10), Width = 90 };
        queryButton.Click += (_, _) => LoadHeaders();

        var newHeaderButton = new Button { Text = "新增單頭", Location = new Point(660, 10), Width = 95 };
        newHeaderButton.Click += (_, _) => CreateHeader();

        var saveHeaderButton = new Button { Text = "儲存單頭", Location = new Point(760, 10), Width = 95 };
        saveHeaderButton.Click += (_, _) => UpdateHeader();

        var deleteHeaderButton = new Button { Text = "刪除單頭", Location = new Point(860, 10), Width = 95 };
        deleteHeaderButton.Click += (_, _) => DeleteHeader();

        var confirmButton = new Button { Text = "確認", Location = new Point(960, 10), Width = 80 };
        confirmButton.Click += (_, _) => ConfirmHeader();

        var unconfirmButton = new Button { Text = "取消確認", Location = new Point(1045, 10), Width = 95 };
        unconfirmButton.Click += (_, _) => UnconfirmHeader();

        var voidButton = new Button { Text = "作廢", Location = new Point(1145, 10), Width = 80 };
        voidButton.Click += (_, _) => VoidHeader();

        var headerEditPanel = BuildHeaderEditPanel();
        headerEditPanel.Location = new Point(12, 46);
        headerEditPanel.Width = 1240;
        headerEditPanel.Height = 78;

        var headerGrid = BuildHeaderGrid();
        headerGrid.Location = new Point(12, 130);
        headerGrid.Width = 1240;
        headerGrid.Height = 270;

        var addLineButton = new Button { Text = "新增單身", Location = new Point(12, 410), Width = 95 };
        addLineButton.Click += (_, _) => AddLine();

        var deleteLineButton = new Button { Text = "刪除單身", Location = new Point(112, 410), Width = 95 };
        deleteLineButton.Click += (_, _) => DeleteLine();

        var detailButton = new Button { Text = "明細視窗", Location = new Point(212, 410), Width = 100 };
        detailButton.Click += (_, _) => OpenSelectedDetail();

        var checkButton = new Button { Text = "異常檢查", Location = new Point(317, 410), Width = 100 };
        checkButton.Click += (_, _) => new PurchaseCheckForm(_lineBinding).ShowDialog(this);

        var lineGrid = BuildLineGrid();
        lineGrid.Location = new Point(12, 446);
        lineGrid.Width = 1240;
        lineGrid.Height = 320;

        Controls.Add(new Label { Text = "起日", Location = new Point(20, 16), AutoSize = true });
        Controls.Add(new Label { Text = "迄日", Location = new Point(190, 16), AutoSize = true });
        Controls.Add(_fromDate);
        Controls.Add(_toDate);
        Controls.Add(_department);
        Controls.Add(queryButton);
        Controls.Add(newHeaderButton);
        Controls.Add(saveHeaderButton);
        Controls.Add(deleteHeaderButton);
        Controls.Add(confirmButton);
        Controls.Add(unconfirmButton);
        Controls.Add(voidButton);
        Controls.Add(headerEditPanel);
        Controls.Add(headerGrid);
        Controls.Add(addLineButton);
        Controls.Add(deleteLineButton);
        Controls.Add(detailButton);
        Controls.Add(checkButton);
        Controls.Add(lineGrid);
    }

    private Panel BuildHeaderEditPanel()
    {
        var panel = new Panel { BorderStyle = BorderStyle.FixedSingle };

        _editDate.Format = DateTimePickerFormat.Short;
        _editDate.Location = new Point(90, 22);

        _editDepartment.Location = new Point(290, 22);
        _editDepartment.Width = 120;

        _editBuyer.Location = new Point(520, 22);
        _editBuyer.Width = 140;

        panel.Controls.Add(new Label { Text = "採購日期", Location = new Point(20, 26), AutoSize = true });
        panel.Controls.Add(new Label { Text = "部門", Location = new Point(245, 26), AutoSize = true });
        panel.Controls.Add(new Label { Text = "採購員", Location = new Point(470, 26), AutoSize = true });
        panel.Controls.Add(new Label { Text = $"目前使用者：{_currentUserId}", Location = new Point(720, 26), AutoSize = true, ForeColor = Color.DarkBlue });
        panel.Controls.Add(_editDate);
        panel.Controls.Add(_editDepartment);
        panel.Controls.Add(_editBuyer);

        return panel;
    }

    private DataGridView BuildHeaderGrid()
    {
        var grid = new DataGridView
        {
            AutoGenerateColumns = false,
            DataSource = _headerBinding,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false
        };

        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "單號", DataPropertyName = nameof(PurchaseOrderHeader.OrderNo), Width = 180 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "日期", DataPropertyName = nameof(PurchaseOrderHeader.OrderDate), Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy/MM/dd" } });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "部門", DataPropertyName = nameof(PurchaseOrderHeader.Department), Width = 110 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "採購員", DataPropertyName = nameof(PurchaseOrderHeader.Buyer), Width = 120 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "狀態", DataPropertyName = nameof(PurchaseOrderHeader.Status), Width = 90 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "總金額", DataPropertyName = nameof(PurchaseOrderHeader.TotalAmount), Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } });

        grid.SelectionChanged += (_, _) => LoadLinesForSelection();
        return grid;
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
            AllowUserToDeleteRows = false
        };

        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "序", DataPropertyName = nameof(PurchaseOrderLine.Sequence), Width = 60 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "料號", DataPropertyName = nameof(PurchaseOrderLine.ItemNo), Width = 130 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "品名", DataPropertyName = nameof(PurchaseOrderLine.ItemName), Width = 170 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "製令單號", DataPropertyName = nameof(PurchaseOrderLine.SourceOrderNo), Width = 120 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "製程區間", DataPropertyName = nameof(PurchaseOrderLine.ProcessRange), Width = 90 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "數量", DataPropertyName = nameof(PurchaseOrderLine.Quantity), Width = 90, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "單價", DataPropertyName = nameof(PurchaseOrderLine.UnitPrice), Width = 90, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "金額", DataPropertyName = nameof(PurchaseOrderLine.Amount), Width = 90, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "參考金額", DataPropertyName = nameof(PurchaseOrderLine.ReferenceAmount), Width = 90, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "成本比", DataPropertyName = nameof(PurchaseOrderLine.CostRatio), Width = 70, DefaultCellStyle = new DataGridViewCellStyle { Format = "N3", Alignment = DataGridViewContentAlignment.MiddleRight } });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "MOQ", DataPropertyName = nameof(PurchaseOrderLine.MinimumOrderQty), Width = 60, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "交期", DataPropertyName = nameof(PurchaseOrderLine.DueDate), Width = 110, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy/MM/dd" } });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "狀態", DataPropertyName = nameof(PurchaseOrderLine.StatusCode), Width = 70 });

        return grid;
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

            LoadLinesForSelection();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "查詢失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void LoadLinesForSelection()
    {
        var selected = _headerBinding.Current as PurchaseOrderHeader;
        if (selected is null)
        {
            _lineBinding.DataSource = new BindingList<PurchaseOrderLine>();
            _editDate.Value = DateTime.Today;
            _editDepartment.Clear();
            _editBuyer.Clear();
            return;
        }

        _editDate.Value = selected.OrderDate.Date;
        _editDepartment.Text = selected.Department;
        _editBuyer.Text = selected.Buyer;

        var lines = _service.QueryLines(selected.OrderNo);
        _lineBinding.DataSource = new BindingList<PurchaseOrderLine>(lines.ToList());
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
            MessageBox.Show(this, "請先選擇採購單。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

    private void OpenSelectedDetail()
    {
        var selected = GetSelectedHeader();
        if (selected is null)
        {
            return;
        }

        new PurchaseDetailForm(selected, _service.QueryLines(selected.OrderNo)).ShowDialog(this);
    }
}
