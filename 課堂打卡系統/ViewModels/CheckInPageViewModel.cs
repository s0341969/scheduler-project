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

    public bool RequireStudentLogin { get; init; }

    public bool CanCheckIn { get; init; }

    public bool IsQrValidated { get; init; }

    public bool IsWithinAllowedNetwork { get; init; }

    public string ExpectedSsidLabel { get; init; } = string.Empty;

    public string AccessError { get; init; } = string.Empty;

    public string StudentNumber { get; init; } = string.Empty;

    public string StudentName { get; init; } = string.Empty;

    public DateTime? QrExpiresAtLocal { get; init; }

    public CheckInFormViewModel Form { get; init; } = new();

    public IReadOnlyList<AttendanceRecordViewModel> Records { get; init; } = [];
}

public sealed class CheckInFormViewModel
{
    [Required]
    public Guid SessionId { get; set; }

    [Required]
    public string Token { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "備註最多 100 個字")]
    [Display(Name = "備註")]
    public string? Note { get; set; }
}
