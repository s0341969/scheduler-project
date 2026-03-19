using System.Globalization;
using PUR2019.WinForms.Configuration;
using PUR2019.WinForms.Forms;
using PUR2019.WinForms.Services;

namespace PUR2019.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("zh-TW");
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("zh-TW");

        ApplicationConfiguration.Initialize();

        var settings = AppSettings.Load(AppContext.BaseDirectory);
        IPurchaseOrderService service;
        string sourceLabel;

        try
        {
            (service, sourceLabel) = PurchaseOrderServiceFactory.Create(settings.DataSource);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"資料來源初始化失敗，改用 InMemory。{Environment.NewLine}{ex.Message}",
                "警告",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            service = new InMemoryPurchaseOrderService();
            sourceLabel = "資料來源：InMemory（Fallback）";
        }

        Application.Run(new MainMenuForm(service, sourceLabel));
    }
}
