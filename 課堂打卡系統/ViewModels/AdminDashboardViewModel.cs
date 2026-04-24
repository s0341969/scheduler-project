namespace 課堂打卡系統.ViewModels;

public sealed class AdminDashboardViewModel
{
    public DateTime GeneratedAt { get; init; }

    public int CourseCount { get; init; }

    public int SessionCount { get; init; }

    public int AttendanceCount { get; init; }

    public int SuspiciousAttendanceCount { get; init; }

    public IReadOnlyList<AdminCourseItemViewModel> Courses { get; init; } = [];

    public IReadOnlyList<AdminSessionItemViewModel> Sessions { get; init; } = [];

    public IReadOnlyList<AdminSuspiciousRecordViewModel> SuspiciousRecords { get; init; } = [];
}

public sealed class AdminCourseItemViewModel
{
    public Guid Id { get; init; }

    public string ClassCode { get; init; } = string.Empty;

    public string CourseCode { get; init; } = string.Empty;

    public string CourseName { get; init; } = string.Empty;

    public string TeacherName { get; init; } = string.Empty;

    public string Classroom { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public int SessionCount { get; init; }

    public int AttendanceCount { get; init; }
}

public sealed class AdminSessionItemViewModel
{
    public Guid Id { get; init; }

    public Guid CourseId { get; init; }

    public string ClassCode { get; init; } = string.Empty;

    public string CourseCode { get; init; } = string.Empty;

    public string CourseName { get; init; } = string.Empty;

    public string TeacherName { get; init; } = string.Empty;

    public string Classroom { get; init; } = string.Empty;

    public string Topic { get; init; } = string.Empty;

    public DateTime StartAt { get; init; }

    public DateTime EndAt { get; init; }

    public bool IsOpen { get; init; }

    public int AttendanceCount { get; init; }

    public int SuspiciousAttendanceCount { get; init; }
}

public sealed class AdminSuspiciousRecordViewModel
{
    public string CourseCode { get; init; } = string.Empty;

    public string CourseName { get; init; } = string.Empty;

    public string Topic { get; init; } = string.Empty;

    public string StudentNumber { get; init; } = string.Empty;

    public string StudentName { get; init; } = string.Empty;

    public DateTime CheckedInAt { get; init; }

    public string SourceIp { get; init; } = string.Empty;

    public string SuspiciousReasonSummary { get; init; } = string.Empty;
}
