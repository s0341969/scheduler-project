namespace 課堂打卡系統.Models;

public sealed class AttendanceStore
{
    public List<AttendanceCourse> Courses { get; set; } = [];

    public List<ClassSession> Sessions { get; set; } = [];

    public List<AttendanceRecord> Records { get; set; } = [];
}
