using System.Text.RegularExpressions;
using SqlMaintenanceAgent.App.Configuration;
using SqlMaintenanceAgent.App.Models;

namespace SqlMaintenanceAgent.App.Services;

public sealed class SqlGuard
{
    private static readonly Regex BlockedPattern = new(
        @"\b(xp_cmdshell|sp_oacreate|sp_execute_external_script|openrowset|opendatasource|bulk\s+insert)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly SecurityOptions _securityOptions;

    public SqlGuard(SecurityOptions securityOptions)
    {
        _securityOptions = securityOptions;
    }

    public SqlGuardResult Analyze(string sql)
    {
        var normalized = Normalize(sql);
        var statementType = DetectStatementType(normalized);
        var isWrite = statementType is "INSERT" or "UPDATE" or "DELETE" or "MERGE" or "CREATE" or "ALTER" or "DROP" or "TRUNCATE" or "EXEC";
        var findings = new List<GuardFinding>();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            findings.Add(new GuardFinding("EMPTY_SQL", "SQL 內容為空。", true));
        }

        if (BlockedPattern.IsMatch(normalized))
        {
            findings.Add(new GuardFinding("BLOCKED_KEYWORD", "SQL 包含高風險關鍵字，已拒絕執行。", true));
        }

        if (ContainsUnboundedUpdate(normalized))
        {
            findings.Add(new GuardFinding("UPDATE_WITHOUT_WHERE", "偵測到無 WHERE 的 UPDATE。", true));
        }

        if (ContainsUnboundedDelete(normalized))
        {
            findings.Add(new GuardFinding("DELETE_WITHOUT_WHERE", "偵測到無 WHERE 的 DELETE。", true));
        }

        if (_securityOptions.ReadOnly && isWrite)
        {
            findings.Add(new GuardFinding("READ_ONLY_MODE", "目前為唯讀模式，禁止寫入語句。", true));
        }

        return new SqlGuardResult
        {
            StatementType = statementType,
            IsWrite = isWrite,
            IsAllowed = findings.All(f => !f.IsBlocking),
            Findings = findings
        };
    }

    private static string DetectStatementType(string normalizedSql)
    {
        var tokens = Regex.Matches(normalizedSql, @"\b[A-Z]+\b").Select(match => match.Value).ToList();
        if (tokens.Count == 0)
        {
            return "UNKNOWN";
        }

        if (tokens[0] == "WITH" && tokens.Count > 1)
        {
            var firstDataToken = tokens.FirstOrDefault(token => token is "SELECT" or "INSERT" or "UPDATE" or "DELETE" or "MERGE");
            return firstDataToken ?? "WITH";
        }

        return tokens[0];
    }

    private static bool ContainsUnboundedUpdate(string sql)
    {
        var updatePattern = new Regex(@"\bUPDATE\b[\s\S]+?\bSET\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        return updatePattern.IsMatch(sql) && !Regex.IsMatch(sql, @"\bWHERE\b", RegexOptions.IgnoreCase);
    }

    private static bool ContainsUnboundedDelete(string sql)
    {
        var deletePattern = new Regex(@"\bDELETE\s+FROM\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        return deletePattern.IsMatch(sql) && !Regex.IsMatch(sql, @"\bWHERE\b", RegexOptions.IgnoreCase);
    }

    private static string Normalize(string sql)
    {
        var withoutLineComments = Regex.Replace(sql, @"--.*?$", string.Empty, RegexOptions.Multiline);
        var withoutBlockComments = Regex.Replace(withoutLineComments, @"/\*[\s\S]*?\*/", string.Empty);
        var withoutStringLiterals = Regex.Replace(withoutBlockComments, @"'([^']|'')*'", "''");
        return withoutStringLiterals.Trim().ToUpperInvariant();
    }
}
