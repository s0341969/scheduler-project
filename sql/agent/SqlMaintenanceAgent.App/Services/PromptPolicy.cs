using SqlMaintenanceAgent.App.Models;

namespace SqlMaintenanceAgent.App.Services;

public sealed class PromptPolicy
{
    public string BuildSystemPrompt(AgentCommandType commandType)
    {
        var commandInstruction = commandType switch
        {
            AgentCommandType.Ask => "你是 SQL 維運助理，請產生可執行 SQL 並附風險評估。",
            AgentCommandType.Plan => "你是 SQL 維運助理，請提供執行計畫與預估影響，不要假裝已執行。",
            AgentCommandType.Explain => "你是 SQL 維運助理，請解釋 SQL 的邏輯、風險與優化方向。",
            _ => "你是 SQL 維運助理。"
        };

        return
            """
            你是一位嚴謹的 SQL Server 維運工程師，輸出語言必須為繁體中文。
            只可以輸出 JSON，格式如下：
            {
              "summary": "一句到三句重點摘要",
              "proposed_sql": "完整可執行 SQL；若不需要 SQL 請給空字串",
              "risk_level": "LOW 或 MEDIUM 或 HIGH",
              "estimated_impact": "預估影響範圍與風險",
              "checks": ["執行前檢查1", "執行前檢查2"]
            }
            SQL 規範：
            1. 禁止無 WHERE 的 UPDATE/DELETE。
            2. 優先使用可回滾策略與明確條件。
            3. 若需求不清楚，採安全預設並在 checks 裡列出假設。
            指令類型：
            """ + Environment.NewLine + commandInstruction;
    }
}
