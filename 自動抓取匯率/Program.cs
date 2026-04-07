using GonGinLibrary;
using System;
using System.Configuration;
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
        private static void Main(string[] args)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            String ConfigString = "<?xml version=\"1.0\"?>" +
                "<configuration>" +
                    "<configSections>" +
                    "</configSections>" +
                "<startup useLegacyV2RuntimeActivationPolicy=\"true\"><supportedRuntime version=\"v4.0\" sku=\".NETFramework,Version=v4.0\"/></startup>" +                    
                "</configuration>";

            if (!System.IO.File.Exists(ConfigurationManager.OpenExeConfiguration(Application.ExecutablePath).FilePath.ToString()))
            {
                System.IO.File.WriteAllText(ConfigurationManager.OpenExeConfiguration(Application.ExecutablePath).FilePath.ToString(), ConfigString);
            }

            //----------------------------------------------------------------------------------------
            //              資料環境建立及取得
            //----------------------------------------------------------------------------------------
            GonGinSystemEnquipment SystemEnquipment = new GonGinSystemEnquipment();


            // 取得註冊檔資料(公邦準IP及資料庫名)
            if (!SystemEnquipment.GetLoginRegistry())
            {
                MessageBox.Show("無法由註冊檔取得執行環境設定值,請更新登入程式或請與資訊室連絡 !!", "訊息提示",
                                 MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
            SystemEnquipment.GetSqlConnectString("自動抓取匯率");

            // 取得命令上的參數值
            // Y Y Y Y Y Y Y 20030101 1439
            if (args.Length > 0)
            {
                GonGinVariable.AuthorizatioString = args[0].ToString() + args[1].ToString() + args[2].ToString() + args[3].ToString() + args[4].ToString() + args[5].ToString() + args[6].ToString(); // YYYYYYY
                GonGinVariable.ApplicationUser = args[8].ToString(); // 系統使用者
                if (args[7].ToString() != "20030101")
                {
                    Application.Exit();
                }
                // 取得系統操作者姓名
                GonGinCheckOfDataDuplication GetUserName = new GonGinCheckOfDataDuplication(GonGinVariable.SqlConnectString.ToString(), "PERSON", "PNAME", "PNAME", "PNAME", "PENNO = '" + GonGinVariable.ApplicationUser + "'");
                GonGinVariable.ApplicationUserName = GetUserName.傳回值;
                // 執行表單

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Forms.MainForm());
            }
            else
            {
                // 允許某些電腦不用帶DOS參數即可不受權限控管(在設計時方便)
                if (System.Environment.MachineName.CompareTo("AC1931") == 0 ||
                    System.Environment.MachineName.CompareTo("AC2032-7-PC") == 0 ||
                    System.Environment.MachineName.CompareTo("AC2155-6") == 0 ||
                    System.Environment.MachineName.CompareTo("AC2153-4") == 0 ||
                    System.Environment.MachineName.CompareTo("AC2252-3") == 0 ||
                    System.Environment.MachineName.CompareTo("AC2137-8") == 0 ||
                    System.Environment.MachineName.CompareTo("AC2155-6") == 0 ||
                    System.Environment.MachineName.CompareTo("AC2384-1") == 0
                    )
                {
                    GonGinVariable.AuthorizatioString = "YYYYYYY";
                    GonGinVariable.ApplicationUser = "1035";
                    GonGinVariable.ApplicationUserDeptNo = "9310";
                    GonGinVariable.ApplicationUserName = "張展嘉";
                }

                // 程式設計階段時執行用 ( 設計階段時先將註記拿掉 )
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Forms.MainForm());
            }


        }
    }
}
