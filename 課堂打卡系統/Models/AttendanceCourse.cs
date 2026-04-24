namespace 課堂打卡系統.Models;

public sealed class AttendanceCourse
{
    public Guid Id { get; set; }

    public string ClassCode { get; set; } = string.Empty;

    public string CourseCode { get; set; } = string.Empty;

    public string CourseName { get; set; } = string.Empty;

    public string TeacherName { get; set; } = string.Empty;

    public string Classroom { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}
