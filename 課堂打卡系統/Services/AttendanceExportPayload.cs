namespace 課堂打卡系統.Services;

public sealed class AttendanceExportPayload
{
    public required string FileName { get; init; }

    public required byte[] Content { get; init; }

    public string ContentType { get; init; } = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
}
