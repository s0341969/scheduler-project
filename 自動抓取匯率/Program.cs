using System;
using System.Net;
using System.Windows.Forms;

namespace BotExchangeRateWinForms
{
    /// <summary>
    /// 定義 WinForms 應用程式的啟動入口。
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// 設定 TLS 與 WinForms 執行環境，然後開啟主畫面。
        /// </summary>
        [STAThread]
        private static void Main()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Forms.MainForm());
        }
    }
}
