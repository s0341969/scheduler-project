namespace 課堂打卡系統.Services;

public sealed class OperationResult
{
    public bool Success { get; init; }

    public string Message { get; init; } = string.Empty;
}
