namespace PUR2019.WinForms.Forms;

public sealed class TextReportForm : Form
{
    public TextReportForm(string title, string reportText)
    {
        Text = title;
        Width = 980;
        Height = 720;
        StartPosition = FormStartPosition.CenterParent;

        var box = new TextBox
        {
            Multiline = true,
            ScrollBars = ScrollBars.Both,
            ReadOnly = true,
            WordWrap = false,
            Font = new Font("Consolas", 10F),
            Dock = DockStyle.Fill,
            Text = reportText
        };

        Controls.Add(box);
    }
}
