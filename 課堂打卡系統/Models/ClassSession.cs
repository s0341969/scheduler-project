namespace 課堂打卡系統.Models;

public sealed class ClassSession
{
    public Guid Id { get; set; }

    public Guid CourseId { get; set; }

    public DateTimeOffset StartAt { get; set; }

    public DateTimeOffset EndAt { get; set; }

    public bool IsOpen { get; set; }

    public string Topic { get; set; } = string.Empty;

    public bool RequireStudentLogin { get; set; } = true;
}
