using PUR2019.WinForms.Services;

namespace PUR2019.WinForms.Forms;

public sealed class PurchaseAdminForm : Form
{
    private readonly IPurchaseOrderService _service;

    public PurchaseAdminForm(IPurchaseOrderService service)
    {
        _service = service;
        InitializeUi();
    }

    private void InitializeUi()
    {
        Text = "PUR2019AF 採購管理";
        Width = 640;
        Height = 380;
        StartPosition = FormStartPosition.CenterParent;

        var info = new Label
        {
            AutoSize = false,
            Width = 590,
            Height = 130,
            Location = new Point(20, 20),
            Text = "此視窗對應 Delphi 的 PUR2019A 程式入口。\r\n" +
                   "目前已完成可執行骨架，並共用採購資料服務。\r\n" +
                   "後續可將 PUR2019AP / PUR2019AP1P / PUR2019AP2P 的實際規則逐一移植。"
        };

        var openQuery = new Button
        {
            Text = "開啟採購查詢",
            Width = 160,
            Height = 32,
            Location = new Point(20, 170)
        };
        openQuery.Click += (_, _) => new PurchaseMainForm(_service).ShowDialog(this);

        var close = new Button
        {
            Text = "關閉",
            Width = 100,
            Height = 32,
            Location = new Point(200, 170)
        };
        close.Click += (_, _) => Close();

        Controls.Add(info);
        Controls.Add(openQuery);
        Controls.Add(close);
    }
}
