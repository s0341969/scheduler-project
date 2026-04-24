using System.ComponentModel.DataAnnotations;

namespace 課堂打卡系統.ViewModels;

public sealed class CheckInPageViewModel
{
    public Guid SessionId { get; init; }

    public string CourseCode { get; init; } = string.Empty;

    public string CourseName { get; init; } = string.Empty;

    public string TeacherName { get; init; } = string.Empty;

    public string Classroom { get; init; } = string.Empty;

    public string Topic { get; init; } = string.Empty;

    public DateTime StartAt { get; init; }

    public DateTime EndAt { get; init; }

    public bool IsOpen { get; init; }

    public CheckInFormViewModel Form { get; init; } = new();

    public IReadOnlyList<AttendanceRecordViewModel> Records { get; init; } = [];
}

public sealed class CheckInFormViewModel
{
    [Required]
    public Guid SessionId { get; set; }

    [Required(ErrorMessage = "請輸入學號")]
    [StringLength(20, ErrorMessage = "學號最多 20 個字")]
    [Display(Name = "學號")]
    public string StudentNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入姓名")]
    [StringLength(50, ErrorMessage = "姓名最多 50 個字")]
    [Display(Name = "姓名")]
    public string StudentName { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "備註最多 100 個字")]
    [Display(Name = "備註")]
    public string? Note { get; set; }
}
