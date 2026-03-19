namespace PUR2019.WinForms.Forms;

public sealed class PurchaseOrderLineEditForm : Form
{
    private readonly TextBox _itemNo = new();
    private readonly TextBox _itemName = new();
    private readonly NumericUpDown _quantity = new();
    private readonly NumericUpDown _unitPrice = new();
    private readonly DateTimePicker _dueDate = new();

    public string ItemNo => _itemNo.Text.Trim();

    public string ItemName => _itemName.Text.Trim();

    public decimal Quantity => _quantity.Value;

    public decimal UnitPrice => _unitPrice.Value;

    public DateTime DueDate => _dueDate.Value.Date;

    public PurchaseOrderLineEditForm()
    {
        Text = "新增單身";
        Width = 420;
        Height = 300;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        _itemNo.Location = new Point(120, 20);
        _itemNo.Width = 250;

        _itemName.Location = new Point(120, 60);
        _itemName.Width = 250;

        _quantity.Location = new Point(120, 100);
        _quantity.Width = 120;
        _quantity.DecimalPlaces = 2;
        _quantity.Minimum = 0.01M;
        _quantity.Maximum = 100000000M;
        _quantity.Value = 1M;

        _unitPrice.Location = new Point(120, 140);
        _unitPrice.Width = 120;
        _unitPrice.DecimalPlaces = 2;
        _unitPrice.Minimum = 0M;
        _unitPrice.Maximum = 100000000M;

        _dueDate.Location = new Point(120, 180);
        _dueDate.Format = DateTimePickerFormat.Short;
        _dueDate.Value = DateTime.Today.AddDays(7);

        var ok = new Button { Text = "確定", Location = new Point(120, 220), Width = 90 };
        ok.Click += OnOkClicked;

        var cancel = new Button { Text = "取消", Location = new Point(220, 220), Width = 90, DialogResult = DialogResult.Cancel };

        Controls.Add(new Label { Text = "料號", Location = new Point(30, 24), AutoSize = true });
        Controls.Add(new Label { Text = "品名", Location = new Point(30, 64), AutoSize = true });
        Controls.Add(new Label { Text = "數量", Location = new Point(30, 104), AutoSize = true });
        Controls.Add(new Label { Text = "單價", Location = new Point(30, 144), AutoSize = true });
        Controls.Add(new Label { Text = "交期", Location = new Point(30, 184), AutoSize = true });
        Controls.Add(_itemNo);
        Controls.Add(_itemName);
        Controls.Add(_quantity);
        Controls.Add(_unitPrice);
        Controls.Add(_dueDate);
        Controls.Add(ok);
        Controls.Add(cancel);

        AcceptButton = ok;
        CancelButton = cancel;
    }

    private void OnOkClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ItemNo))
        {
            MessageBox.Show(this, "料號不可空白。", "驗證失敗", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _itemNo.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(ItemName))
        {
            MessageBox.Show(this, "品名不可空白。", "驗證失敗", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _itemName.Focus();
            return;
        }

        DialogResult = DialogResult.OK;
    }
}
