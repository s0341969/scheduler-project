using System.Diagnostics;
using System.Text;
using SqlMaintenanceAgent.App.Configuration;
using SqlMaintenanceAgent.App.Models;
using SqlMaintenanceAgent.App.Services;

namespace SqlMaintenanceAgent.App;

public sealed class CliHost
{
    private readonly AgentOptions _options;
    private readonly LlmClient _llmClient;
    private readonly PromptPolicy _promptPolicy;
    private readonly SqlGuard _sqlGuard;
    private readonly SqlExecutor _sqlExecutor;
    private readonly AuditLogger _auditLogger;

    public CliHost(
        AgentOptions options,
        LlmClient llmClient,
        PromptPolicy promptPolicy,
        SqlGuard sqlGuard,
        SqlExecutor sqlExecutor,
        AuditLogger auditLogger)
    {
        _options = options;
        _llmClient = llmClient;
        _promptPolicy = promptPolicy;
        _sqlGuard = sqlGuard;
        _sqlExecutor = sqlExecutor;
        _auditLogger = auditLogger;
    }

    public async Task RunAsync()
    {
        PrintBanner();
        while (true)
        {
            Console.Write("sql-agent> ");
            var line = Console.ReadLine();
            if (line is null)
            {
                return;
            }

            var (command, payload) = ParseCommand(line);
            if (string.IsNullOrWhiteSpace(command))
            {
                continue;
            }

            if (command.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (command.Equals("help", StringComparison.OrdinalIgnoreCase))
            {
                PrintHelp();
                continue;
            }

            if (string.IsNullOrWhiteSpace(payload))
            {
                Console.WriteLine("請提供命令內容。");
                continue;
            }

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.Database.CommandTimeoutSeconds));
            try
            {
                switch (command.ToLowerInvariant())
                {
                    case "ask":
                        await HandleLlmCommandAsync(AgentCommandType.Ask, payload, cts.Token);
                        break;
                    case "plan":
                        await HandleLlmCommandAsync(AgentCommandType.Plan, payload, cts.Token);
                        break;
                    case "explain":
                        await HandleLlmCommandAsync(AgentCommandType.Explain, payload, cts.Token);
                        break;
                    case "run":
                        await HandleRunAsync(payload, cts.Token);
                        break;
                    default:
                        Console.WriteLine("未知命令，輸入 help 查看可用命令。");
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("操作逾時或已取消。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"執行失敗：{ex.Message}");
            }
        }
    }

    private async Task HandleLlmCommandAsync(AgentCommandType commandType, string input, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var systemPrompt = _promptPolicy.BuildSystemPrompt(commandType);
        var response = await _llmClient.GetAdvisoryAsync(commandType, systemPrompt, input, cancellationToken);
        stopwatch.Stop();

        Console.WriteLine($"[摘要] {response.Summary}");
        if (!string.IsNullOrWhiteSpace(response.ProposedSql))
        {
            Console.WriteLine("[建議 SQL]");
            Console.WriteLine(response.ProposedSql);
            var guardResult = _sqlGuard.Analyze(response.ProposedSql);
            PrintGuardResult(guardResult);
        }

        Console.WriteLine($"[風險] {response.RiskLevel}");
        Console.WriteLine($"[影響] {response.EstimatedImpact}");
        if (response.Checks.Count > 0)
        {
            Console.WriteLine("[執行前檢查]");
            foreach (var check in response.Checks)
            {
                Console.WriteLine($"- {check}");
            }
        }

        await _auditLogger.WriteAsync(new AuditEntry
        {
            Timestamp = DateTimeOffset.Now,
            CommandType = commandType.ToString(),
            UserInput = input,
            SqlText = response.ProposedSql,
            SqlHash = AuditLogger.ComputeHash(response.ProposedSql),
            RiskLevel = response.RiskLevel,
            Success = true,
            DurationMs = stopwatch.ElapsedMilliseconds,
            GuardSummary = "LLM advisory"
        }, cancellationToken);
    }

    private async Task HandleRunAsync(string sql, CancellationToken cancellationToken)
    {
        var guardResult = _sqlGuard.Analyze(sql);
        PrintGuardResult(guardResult);
        if (!guardResult.IsAllowed)
        {
            Console.WriteLine("SQL Guard 已阻擋執行。");
            await WriteRunAuditAsync(sql, guardResult, false, "SQL Guard blocked.", 0, false, 0, cancellationToken);
            return;
        }

        if (guardResult.IsWrite && !_options.AllowWrite)
        {
            Console.WriteLine("目前未開啟 --allow-write，拒絕寫入語句。");
            await WriteRunAuditAsync(sql, guardResult, false, "allow-write is required.", 0, false, 0, cancellationToken);
            return;
        }

        if (guardResult.IsWrite)
        {
            Console.WriteLine("即將執行寫入語句，請輸入 YES 確認：");
            var confirm = Console.ReadLine();
            if (!string.Equals(confirm, "YES", StringComparison.Ordinal))
            {
                Console.WriteLine("已取消。");
                await WriteRunAuditAsync(sql, guardResult, false, "User cancelled.", 0, false, 0, cancellationToken);
                return;
            }
        }

        var stopwatch = Stopwatch.StartNew();
        var result = guardResult.IsWrite
            ? await _sqlExecutor.ExecuteWriteWithTransactionAsync(sql, cancellationToken)
            : await _sqlExecutor.ExecuteReadAsync(sql, cancellationToken);
        stopwatch.Stop();

        if (!result.Success)
        {
            Console.WriteLine($"執行失敗：{result.ErrorMessage}");
            await WriteRunAuditAsync(sql, guardResult, false, result.ErrorMessage, result.AffectedRows, result.Truncated, stopwatch.ElapsedMilliseconds, cancellationToken);
            return;
        }

        Console.WriteLine($"執行成功。Rows={result.AffectedRows}, Truncated={result.Truncated}");
        if (!guardResult.IsWrite && result.Rows.Count > 0)
        {
            PrintRows(result);
        }

        await WriteRunAuditAsync(sql, guardResult, true, null, result.AffectedRows, result.Truncated, stopwatch.ElapsedMilliseconds, cancellationToken);
    }

    private async Task WriteRunAuditAsync(
        string sql,
        SqlGuardResult guardResult,
        bool success,
        string? error,
        int affectedRows,
        bool truncated,
        long durationMs,
        CancellationToken cancellationToken)
    {
        var guardSummary = guardResult.Findings.Count == 0
            ? "No findings"
            : string.Join("; ", guardResult.Findings.Select(f => $"{f.Code}:{f.Message}"));

        await _auditLogger.WriteAsync(new AuditEntry
        {
            Timestamp = DateTimeOffset.Now,
            CommandType = AgentCommandType.Run.ToString(),
            UserInput = sql,
            SqlText = sql,
            SqlHash = AuditLogger.ComputeHash(sql),
            RiskLevel = guardResult.IsWrite ? "HIGH" : "LOW",
            Success = success,
            Error = error,
            DurationMs = durationMs,
            AffectedRows = affectedRows,
            Truncated = truncated,
            GuardSummary = guardSummary
        }, cancellationToken);
    }

    private static void PrintGuardResult(SqlGuardResult result)
    {
        Console.WriteLine($"[Guard] Type={result.StatementType}, IsWrite={result.IsWrite}, Allowed={result.IsAllowed}");
        foreach (var finding in result.Findings)
        {
            Console.WriteLine($"  - {finding.Code}: {finding.Message}");
        }
    }

    private static void PrintRows(QueryExecutionResult result)
    {
        var header = string.Join(" | ", result.Columns);
        Console.WriteLine(header);
        Console.WriteLine(new string('-', Math.Min(120, header.Length)));
        foreach (var row in result.Rows)
        {
            var line = string.Join(" | ", result.Columns.Select(column => row.TryGetValue(column, out var value) ? value : string.Empty));
            Console.WriteLine(line);
        }
    }

    private static (string Command, string Payload) ParseCommand(string input)
    {
        var trimmed = input.Trim();
        var firstSpace = trimmed.IndexOf(' ');
        if (firstSpace < 0)
        {
            return (trimmed, string.Empty);
        }

        var command = trimmed[..firstSpace].Trim();
        var payload = trimmed[(firstSpace + 1)..].Trim();
        if (payload.StartsWith('"') && payload.EndsWith('"') && payload.Length >= 2)
        {
            payload = payload[1..^1];
        }

        return (command, payload);
    }

    private void PrintBanner()
    {
        var mode = _options.Security.ReadOnly ? "READ-ONLY" : "READ-WRITE";
        var allowWriteFlag = _options.AllowWrite ? "ON" : "OFF";
        Console.WriteLine("SQL 維運 AI Agent 已啟動");
        Console.WriteLine($"Security Mode: {mode}, --allow-write: {allowWriteFlag}");
        PrintHelp();
    }

    private static void PrintHelp()
    {
        var builder = new StringBuilder();
        builder.AppendLine("可用命令：");
        builder.AppendLine("  ask \"自然語言需求\"      -> 產生建議 SQL + 風險評估");
        builder.AppendLine("  plan \"需求\"            -> 輸出執行計畫與預估影響");
        builder.AppendLine("  explain \"SQL\"          -> 解釋 SQL 邏輯與風險");
        builder.AppendLine("  run \"SQL\"              -> 執行 SQL（需通過 Guard）");
        builder.AppendLine("  help                    -> 顯示說明");
        builder.AppendLine("  exit                    -> 離開");
        Console.WriteLine(builder.ToString());
    }
}
