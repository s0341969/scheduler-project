namespace PUR2019.WinForms.Forms;

public sealed class UtilityForm : Form
{
    public UtilityForm()
    {
        Text = "Utility 工具";
        Width = 560;
        Height = 320;
        StartPosition = FormStartPosition.CenterParent;

        var text = new Label
        {
            AutoSize = false,
            Width = 500,
            Height = 180,
            Location = new Point(20, 20),
            Text = "此視窗對應 Delphi 的 Utility 模組。\r\n\r\n" +
                   "建議下一步：\r\n" +
                   "1. 把 Utility.pas 內 SQL 與共用函式分離到 C# 類別庫。\r\n" +
                   "2. 逐步替換舊版 DBTables/TQuery 呼叫。\r\n" +
                   "3. 將 Big5/簡繁轉換邏輯集中在編碼服務。"
        };

        var close = new Button { Text = "關閉", Width = 90, Height = 32, Location = new Point(20, 220) };
        close.Click += (_, _) => Close();

        Controls.Add(text);
        Controls.Add(close);
    }
}
