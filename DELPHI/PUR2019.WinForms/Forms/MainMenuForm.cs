using PUR2019.WinForms.Models;
using PUR2019.WinForms.Services;

namespace PUR2019.WinForms.Forms;

public sealed class MainMenuForm : Form
{
    private readonly IPurchaseOrderService _service;

    public MainMenuForm()
    {
        _service = new InMemoryPurchaseOrderService();
        InitializeUi();
    }

    private void InitializeUi()
    {
        Text = "PUR2019 WinForms 移植版";
        Width = 520;
        Height = 340;
        StartPosition = FormStartPosition.CenterScreen;

        var title = new Label
        {
            Text = "採購系統主選單",
            AutoSize = true,
            Font = new Font("Microsoft JhengHei UI", 14F, FontStyle.Bold),
            Location = new Point(20, 20)
        };

        var openPurchasing = CreateButton("採購作業（PUR2019F）", new Point(20, 70), (_, _) => new PurchaseMainForm(_service).ShowDialog(this));
        var openAdmin = CreateButton("採購管理（PUR2019AF）", new Point(20, 115), (_, _) => new PurchaseAdminForm(_service).ShowDialog(this));
        var openUtility = CreateButton("工具視窗（Utility）", new Point(20, 160), (_, _) => new UtilityForm().ShowDialog(this));
        var exit = CreateButton("離開", new Point(20, 205), (_, _) => Close());

        Controls.Add(title);
        Controls.Add(openPurchasing);
        Controls.Add(openAdmin);
        Controls.Add(openUtility);
        Controls.Add(exit);
    }

    private static Button CreateButton(string text, Point location, EventHandler onClick)
    {
        var button = new Button
        {
            Text = text,
            Width = 280,
            Height = 32,
            Location = location
        };

        button.Click += onClick;
        return button;
    }
}
