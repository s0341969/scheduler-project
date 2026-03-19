using PUR2019.WinForms.Models;

namespace PUR2019.WinForms.Forms;

public sealed class PurchaseDetailForm : Form
{
    public PurchaseDetailForm(PurchaseOrderHeader header, IReadOnlyList<PurchaseOrderLine> lines)
    {
        Text = $"採購單明細 - {header.OrderNo}";
        Width = 900;
        Height = 560;
        StartPosition = FormStartPosition.CenterParent;

        Controls.Add(new Label
        {
            AutoSize = true,
            Location = new Point(12, 12),
            Text = $"單號：{header.OrderNo}    日期：{header.OrderDate:yyyy/MM/dd}    部門：{header.Department}    採購員：{header.Buyer}"
        });

        var grid = new DataGridView
        {
            AutoGenerateColumns = true,
            ReadOnly = true,
            DataSource = lines,
            Location = new Point(12, 40),
            Width = 860,
            Height = 450,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false
        };

        Controls.Add(grid);
    }
}
