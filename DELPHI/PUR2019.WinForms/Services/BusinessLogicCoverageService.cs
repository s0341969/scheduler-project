namespace PUR2019.WinForms.Services;

public static class BusinessLogicCoverageService
{
    public static IReadOnlyList<LogicCoverageItem> GetCoverage()
    {
        return
        [
            new LogicCoverageItem("單頭 CRUD（PURTM）", "Y", "已落地"),
            new LogicCoverageItem("單身 CRUD（PURTD）", "Y", "已落地"),
            new LogicCoverageItem("確認/取消確認/作廢狀態流", "Y", "已落地"),
            new LogicCoverageItem("交易一致性（單頭單身同步）", "Y", "已落地"),
            new LogicCoverageItem("取消確認前 PURDEL 防護", "Y", "已落地"),
            new LogicCoverageItem("刪除單身前發料防護", "Y", "已落地"),
            new LogicCoverageItem("製令連動 ORDMENO.MPCHK", "Y", "已落地"),
            new LogicCoverageItem("PUPA1/PUPA2 分段金額計算", "Y", "已落地"),
            new LogicCoverageItem("MOQ 檢核", "Y", "已落地"),
            new LogicCoverageItem("Delphi 全欄位 UI 事件連動", "P", "部分完成"),
            new LogicCoverageItem("報表與列印流程（SpeedButton11）", "N", "尚未移植"),
            new LogicCoverageItem("PUR2019AP 管理模組完整邏輯", "N", "尚未移植")
        ];
    }

    public static string BuildReportText()
    {
        var items = GetCoverage();
        var done = items.Count(x => x.Status == "Y");
        var partial = items.Count(x => x.Status == "P");
        var total = items.Count;

        var lines = new List<string>
        {
            $"商業邏輯覆蓋：Y={done}, P={partial}, Total={total}",
            ""
        };

        foreach (var item in items)
        {
            lines.Add($"[{item.Status}] {item.Rule} ({item.Note})");
        }

        return string.Join(Environment.NewLine, lines);
    }
}

public sealed record LogicCoverageItem(string Rule, string Status, string Note);
