namespace SqlMaintenanceAgent.App.Models;

public enum AgentCommandType
{
    Ask,
    Plan,
    Explain,
    Run
}

public sealed record LlmAdvisoryResponse
{
    public string Summary { get; init; } = string.Empty;
    public string ProposedSql { get; init; } = string.Empty;
    public string RiskLevel { get; init; } = "MEDIUM";
    public string EstimatedImpact { get; init; } = string.Empty;
    public List<string> Checks { get; init; } = new();
}

public sealed record GuardFinding(string Code, string Message, bool IsBlocking);

public sealed record SqlGuardResult
{
    public string StatementType { get; init; } = "UNKNOWN";
    public bool IsWrite { get; init; }
    public bool IsAllowed { get; init; } = true;
    public List<GuardFinding> Findings { get; init; } = new();
}

public sealed record QueryExecutionResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public int AffectedRows { get; init; }
    public bool Truncated { get; init; }
    public List<string> Columns { get; init; } = new();
    public List<Dictionary<string, string>> Rows { get; init; } = new();
}

public sealed record AuditEntry
{
    public DateTimeOffset Timestamp { get; init; }
    public string CommandType { get; init; } = string.Empty;
    public string UserInput { get; init; } = string.Empty;
    public string SqlText { get; init; } = string.Empty;
    public string SqlHash { get; init; } = string.Empty;
    public string RiskLevel { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string? Error { get; init; }
    public long DurationMs { get; init; }
    public int AffectedRows { get; init; }
    public bool Truncated { get; init; }
    public string GuardSummary { get; init; } = string.Empty;
}
