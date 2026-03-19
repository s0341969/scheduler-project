namespace PUR2019.WinForms.Forms;

public sealed class PurchaseOrderHeaderEditForm : Form
{
    private readonly DateTimePicker _orderDate = new();
    private readonly TextBox _department = new();
    private readonly TextBox _buyer = new();

    public DateTime OrderDate => _orderDate.Value.Date;

    public string Department => _department.Text.Trim();

    public string Buyer => _buyer.Text.Trim();

    public PurchaseOrderHeaderEditForm(string title, DateTime defaultDate, string defaultDepartment, string defaultBuyer)
    {
        Text = title;
        Width = 380;
        Height = 240;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        _orderDate.Format = DateTimePickerFormat.Short;
        _orderDate.Value = defaultDate;
        _orderDate.Location = new Point(110, 20);

        _department.Text = defaultDepartment;
        _department.Location = new Point(110, 60);
        _department.Width = 220;

        _buyer.Text = defaultBuyer;
        _buyer.Location = new Point(110, 100);
        _buyer.Width = 220;

        var ok = new Button { Text = "確定", Location = new Point(110, 145), Width = 90 };
        ok.Click += OnOkClicked;

        var cancel = new Button { Text = "取消", Location = new Point(210, 145), Width = 90, DialogResult = DialogResult.Cancel };

        Controls.Add(new Label { Text = "採購日期", Location = new Point(25, 24), AutoSize = true });
        Controls.Add(new Label { Text = "部門代碼", Location = new Point(25, 64), AutoSize = true });
        Controls.Add(new Label { Text = "採購員", Location = new Point(25, 104), AutoSize = true });
        Controls.Add(_orderDate);
        Controls.Add(_department);
        Controls.Add(_buyer);
        Controls.Add(ok);
        Controls.Add(cancel);

        AcceptButton = ok;
        CancelButton = cancel;
    }

    private void OnOkClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Department))
        {
            MessageBox.Show(this, "部門不可空白。", "驗證失敗", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _department.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(Buyer))
        {
            MessageBox.Show(this, "採購員不可空白。", "驗證失敗", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _buyer.Focus();
            return;
        }

        DialogResult = DialogResult.OK;
    }
}
