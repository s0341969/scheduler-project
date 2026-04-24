namespace 課堂打卡系統.ViewModels;

public sealed class AttendanceDashboardViewModel
{
    public DateTime GeneratedAt { get; init; }

    public int OpenSessionCount { get; init; }

    public int TotalAttendanceCount { get; init; }

    public IReadOnlyList<SessionCardViewModel> Sessions { get; init; } = [];
}

public sealed class SessionCardViewModel
{
    public Guid SessionId { get; init; }

    public string CourseCode { get; init; } = string.Empty;

    public string CourseName { get; init; } = string.Empty;

    public string TeacherName { get; init; } = string.Empty;

    public string Classroom { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string Topic { get; init; } = string.Empty;

    public DateTime StartAt { get; init; }

    public DateTime EndAt { get; init; }

    public bool IsOpen { get; init; }

    public int AttendanceCount { get; init; }

    public IReadOnlyList<AttendanceRecordViewModel> RecentStudents { get; init; } = [];
}

public sealed class AttendanceRecordViewModel
{
    public string StudentNumber { get; init; } = string.Empty;

    public string StudentName { get; init; } = string.Empty;

    public DateTime CheckedInAt { get; init; }

    public string Note { get; init; } = string.Empty;
}
