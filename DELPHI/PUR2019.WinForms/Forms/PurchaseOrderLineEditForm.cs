using System.ComponentModel;
using PUR2019.WinForms.Models;

namespace PUR2019.WinForms.Forms;

public sealed class PurchaseOrderLineEditForm : Form
{
    private readonly TextBox _itemNo = new();
    private readonly TextBox _itemName = new();
    private readonly TextBox _sourceOrderNo = new();
    private readonly TextBox _processFrom = new();
    private readonly TextBox _processTo = new();
    private readonly NumericUpDown _quantity = new();
    private readonly NumericUpDown _unitPrice = new();
    private readonly DateTimePicker _dueDate = new();
    private readonly Button _loadFromSource = new();
    private readonly Label _amountPreview = new();
    private readonly Label _moqHint = new();
    private int _currentSuggestedMoq;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<string, PurchaseOrderLineSuggestion?>? SuggestionProvider { get; set; }

    public string ItemNo => _itemNo.Text.Trim();

    public string ItemName => _itemName.Text.Trim();

    public string SourceOrderNo => _sourceOrderNo.Text.Trim();

    public string ProcessFrom => _processFrom.Text.Trim();

    public string ProcessTo => _processTo.Text.Trim();

    public decimal Quantity => _quantity.Value;

    public decimal UnitPrice => _unitPrice.Value;

    public DateTime DueDate => _dueDate.Value.Date;

    public PurchaseOrderLineEditForm()
    {
        Text = "新增單身";
        Width = 450;
        Height = 410;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        _itemNo.Location = new Point(160, 20);
        _itemNo.Width = 240;

        _itemName.Location = new Point(160, 60);
        _itemName.Width = 240;

        _sourceOrderNo.Location = new Point(160, 100);
        _sourceOrderNo.Width = 160;

        _loadFromSource.Text = "帶入製令";
        _loadFromSource.Location = new Point(325, 98);
        _loadFromSource.Width = 75;
        _loadFromSource.Height = 26;
        _loadFromSource.Click += (_, _) => ApplySuggestionFromSource();

        _processFrom.Location = new Point(160, 140);
        _processFrom.Width = 100;
        _processFrom.Text = "0";

        _processTo.Location = new Point(300, 140);
        _processTo.Width = 100;
        _processTo.Text = "0";

        _quantity.Location = new Point(160, 180);
        _quantity.Width = 120;
        _quantity.DecimalPlaces = 2;
        _quantity.Minimum = 0.01M;
        _quantity.Maximum = 100000000M;
        _quantity.Value = 1M;
        _quantity.ValueChanged += (_, _) => RefreshPreview();

        _unitPrice.Location = new Point(160, 220);
        _unitPrice.Width = 120;
        _unitPrice.DecimalPlaces = 2;
        _unitPrice.Minimum = 0M;
        _unitPrice.Maximum = 100000000M;
        _unitPrice.ValueChanged += (_, _) => RefreshPreview();

        _dueDate.Location = new Point(160, 260);
        _dueDate.Format = DateTimePickerFormat.Short;
        _dueDate.Value = DateTime.Today.AddDays(7);

        _amountPreview.Location = new Point(290, 224);
        _amountPreview.AutoSize = true;
        _amountPreview.ForeColor = Color.DarkBlue;

        _moqHint.Location = new Point(160, 286);
        _moqHint.AutoSize = true;
        _moqHint.ForeColor = Color.Brown;

        _sourceOrderNo.TextChanged += (_, _) =>
        {
            _currentSuggestedMoq = 0;
            _moqHint.Text = string.Empty;
        };

        var ok = new Button { Text = "確定", Location = new Point(160, 310), Width = 90 };
        ok.Click += OnOkClicked;

        var cancel = new Button { Text = "取消", Location = new Point(260, 310), Width = 90, DialogResult = DialogResult.Cancel };

        Controls.Add(new Label { Text = "料號", Location = new Point(30, 24), AutoSize = true });
        Controls.Add(new Label { Text = "品名", Location = new Point(30, 64), AutoSize = true });
        Controls.Add(new Label { Text = "製令單號(PUPRP)", Location = new Point(30, 104), AutoSize = true });
        Controls.Add(new Label { Text = "製程區間(PUPA1/PUPA2)", Location = new Point(30, 144), AutoSize = true });
        Controls.Add(new Label { Text = "數量", Location = new Point(30, 184), AutoSize = true });
        Controls.Add(new Label { Text = "單價", Location = new Point(30, 224), AutoSize = true });
        Controls.Add(new Label { Text = "交期", Location = new Point(30, 264), AutoSize = true });
        Controls.Add(_itemNo);
        Controls.Add(_itemName);
        Controls.Add(_sourceOrderNo);
        Controls.Add(_loadFromSource);
        Controls.Add(_processFrom);
        Controls.Add(_processTo);
        Controls.Add(_quantity);
        Controls.Add(_unitPrice);
        Controls.Add(_dueDate);
        Controls.Add(_amountPreview);
        Controls.Add(_moqHint);
        Controls.Add(ok);
        Controls.Add(cancel);

        AcceptButton = ok;
        CancelButton = cancel;
        RefreshPreview();
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

        if (string.IsNullOrWhiteSpace(ProcessFrom) || string.IsNullOrWhiteSpace(ProcessTo))
        {
            MessageBox.Show(this, "製程區間不可空白。", "驗證失敗", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _processFrom.Focus();
            return;
        }

        if (_currentSuggestedMoq > 0 && Quantity < _currentSuggestedMoq)
        {
            MessageBox.Show(this, $"數量不可小於 MOQ({_currentSuggestedMoq})。", "驗證失敗", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _quantity.Focus();
            return;
        }

        DialogResult = DialogResult.OK;
    }

    private void ApplySuggestionFromSource()
    {
        if (SuggestionProvider is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(SourceOrderNo))
        {
            MessageBox.Show(this, "請先輸入製令單號。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            _sourceOrderNo.Focus();
            return;
        }

        var suggestion = SuggestionProvider(SourceOrderNo);
        if (suggestion is null)
        {
            MessageBox.Show(this, "找不到可帶入資料。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _itemNo.Text = suggestion.ItemNo;
        _itemName.Text = suggestion.ItemName;
        _quantity.Value = Math.Max(_quantity.Minimum, Math.Min(_quantity.Maximum, suggestion.SuggestedQuantity <= 0 ? _quantity.Value : suggestion.SuggestedQuantity));
        if (suggestion.SuggestedUnitPrice > 0)
        {
            _unitPrice.Value = Math.Max(_unitPrice.Minimum, Math.Min(_unitPrice.Maximum, suggestion.SuggestedUnitPrice));
        }

        _processFrom.Text = suggestion.ProcessFrom;
        _processTo.Text = suggestion.ProcessTo;
        _currentSuggestedMoq = suggestion.MinimumOrderQty;
        _moqHint.Text = _currentSuggestedMoq > 0 ? $"MOQ 提示: {_currentSuggestedMoq:N0}" : "MOQ 提示: 無下限";
        RefreshPreview();
    }

    private void RefreshPreview()
    {
        _amountPreview.Text = $"試算金額: {(Quantity * UnitPrice):N2}";
    }
}
