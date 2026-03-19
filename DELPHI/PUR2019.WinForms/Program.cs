using System.Globalization;
using PUR2019.WinForms.Forms;

namespace PUR2019.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("zh-TW");
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("zh-TW");

        ApplicationConfiguration.Initialize();
        Application.Run(new MainMenuForm());
    }
}
