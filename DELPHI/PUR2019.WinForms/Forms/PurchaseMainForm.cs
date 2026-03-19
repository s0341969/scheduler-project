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

    public PurchaseMainForm(IPurchaseOrderService service)
    {
        _service = service;
        InitializeUi();
        LoadHeaders();
    }

    private void InitializeUi()
    {
        Text = "PUR2019F 採購單作業";
        Width = 1200;
        Height = 760;
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

        var detailButton = new Button { Text = "明細視窗", Location = new Point(660, 10), Width = 100 };
        detailButton.Click += (_, _) => OpenSelectedDetail();

        var checkButton = new Button { Text = "異常檢查", Location = new Point(770, 10), Width = 100 };
        checkButton.Click += (_, _) => new PurchaseCheckForm(_lineBinding).ShowDialog(this);

        var headerGrid = BuildHeaderGrid();
        headerGrid.Location = new Point(12, 50);
        headerGrid.Width = 1150;
        headerGrid.Height = 290;

        var lineGrid = BuildLineGrid();
        lineGrid.Location = new Point(12, 360);
        lineGrid.Width = 1150;
        lineGrid.Height = 330;

        Controls.Add(new Label { Text = "起日", Location = new Point(20, 16), AutoSize = true });
        Controls.Add(new Label { Text = "迄日", Location = new Point(190, 16), AutoSize = true });
        Controls.Add(_fromDate);
        Controls.Add(_toDate);
        Controls.Add(_department);
        Controls.Add(queryButton);
        Controls.Add(detailButton);
        Controls.Add(checkButton);
        Controls.Add(headerGrid);
        Controls.Add(lineGrid);
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

        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "單號", DataPropertyName = nameof(PurchaseOrderHeader.OrderNo), Width = 160 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "日期", DataPropertyName = nameof(PurchaseOrderHeader.OrderDate), Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy/MM/dd" } });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "部門", DataPropertyName = nameof(PurchaseOrderHeader.Department), Width = 90 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "採購員", DataPropertyName = nameof(PurchaseOrderHeader.Buyer), Width = 110 });
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
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "品名", DataPropertyName = nameof(PurchaseOrderLine.ItemName), Width = 240 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "數量", DataPropertyName = nameof(PurchaseOrderLine.Quantity), Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "單價", DataPropertyName = nameof(PurchaseOrderLine.UnitPrice), Width = 100, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "金額", DataPropertyName = nameof(PurchaseOrderLine.Amount), Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight } });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "交期", DataPropertyName = nameof(PurchaseOrderLine.DueDate), Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy/MM/dd" } });

        return grid;
    }

    private void LoadHeaders()
    {
        try
        {
            var headers = _service.QueryHeaders(_fromDate.Value.Date, _toDate.Value.Date, _department.Text);
            _headerBinding.DataSource = new BindingList<PurchaseOrderHeader>(headers.ToList());
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
            return;
        }

        var lines = _service.QueryLines(selected.OrderNo);
        _lineBinding.DataSource = new BindingList<PurchaseOrderLine>(lines.ToList());
    }

    private void OpenSelectedDetail()
    {
        var selected = _headerBinding.Current as PurchaseOrderHeader;
        if (selected is null)
        {
            MessageBox.Show(this, "請先選擇採購單。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        new PurchaseDetailForm(selected, _service.QueryLines(selected.OrderNo)).ShowDialog(this);
    }
}
