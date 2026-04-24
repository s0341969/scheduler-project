namespace 課堂打卡系統.Models;

public sealed class AttendanceRecord
{
    public Guid Id { get; set; }

    public Guid SessionId { get; set; }

    public string StudentNumber { get; set; } = string.Empty;

    public string StudentName { get; set; } = string.Empty;

    public DateTimeOffset CheckedInAt { get; set; }

    public string Note { get; set; } = string.Empty;
}
