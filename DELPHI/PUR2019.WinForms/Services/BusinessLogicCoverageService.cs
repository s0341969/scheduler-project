namespace PUR2019.WinForms.Services;

public static class BusinessLogicCoverageService
{
    public static IReadOnlyList<LogicCoverageItem> GetCoverage()
    {
        return
        [
            new LogicCoverageItem("單頭 CRUD（PURTM）", true),
            new LogicCoverageItem("單身 CRUD（PURTD）", true),
            new LogicCoverageItem("確認/取消確認/作廢狀態流", true),
            new LogicCoverageItem("交易一致性（單頭單身同步）", true),
            new LogicCoverageItem("取消確認前 PURDEL 防護", true),
            new LogicCoverageItem("刪除單身前發料防護", true),
            new LogicCoverageItem("製令連動 ORDMENO.MPCHK", true),
            new LogicCoverageItem("PUPA1/PUPA2 分段金額計算", true),
            new LogicCoverageItem("MOQ 檢核", true),
            new LogicCoverageItem("Delphi 全欄位 UI 事件連動", false),
            new LogicCoverageItem("報表與列印流程（SpeedButton11）", false),
            new LogicCoverageItem("PUR2019AP 管理模組完整邏輯", false)
        ];
    }

    public static string BuildReportText()
    {
        var items = GetCoverage();
        var done = items.Count(x => x.Implemented);
        var total = items.Count;

        var lines = new List<string>
        {
            $"商業邏輯覆蓋：{done}/{total}",
            ""
        };

        foreach (var item in items)
        {
            lines.Add($"[{(item.Implemented ? "Y" : "N")}] {item.Rule}");
        }

        return string.Join(Environment.NewLine, lines);
    }
}

public sealed record LogicCoverageItem(string Rule, bool Implemented);
