using PUR2019.WinForms.Models;

namespace PUR2019.WinForms.Forms;

public sealed class PurchaseCheckForm : Form
{
    public PurchaseCheckForm(BindingSource lineBinding)
    {
        Text = "採購異常檢查";
        Width = 520;
        Height = 280;
        StartPosition = FormStartPosition.CenterParent;

        var lines = lineBinding.List.Cast<PurchaseOrderLine>().ToArray();

        var negativeAmountCount = lines.Count(x => x.Quantity < 0 || x.UnitPrice < 0);
        var missingDueDateCount = lines.Count(x => x.DueDate is null);

        var message = new Label
        {
            AutoSize = false,
            Width = 470,
            Height = 150,
            Location = new Point(20, 20),
            Text =
                $"檢查結果：{Environment.NewLine}" +
                $"1. 負數數量或單價筆數：{negativeAmountCount}{Environment.NewLine}" +
                $"2. 未填交期筆數：{missingDueDateCount}{Environment.NewLine}" +
                "3. 此視窗對應 Delphi 的檢核按鈕，後續可擴充實際商業規則。"
        };

        var close = new Button { Text = "關閉", Width = 80, Height = 30, Location = new Point(20, 180) };
        close.Click += (_, _) => Close();

        Controls.Add(message);
        Controls.Add(close);
    }
}
