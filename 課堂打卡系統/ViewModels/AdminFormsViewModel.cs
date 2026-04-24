using System.ComponentModel.DataAnnotations;

namespace 課堂打卡系統.ViewModels;

public sealed class LoginViewModel
{
    [Required(ErrorMessage = "請輸入帳號")]
    [Display(Name = "帳號")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入密碼")]
    [DataType(DataType.Password)]
    [Display(Name = "密碼")]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public sealed class CourseFormViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "請輸入班級")]
    [RegularExpression("^[1-3](0[1-9]|10)$", ErrorMessage = "班級僅支援 101-110、201-210、301-310")]
    [Display(Name = "班級")]
    public string ClassCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入課程代碼")]
    [StringLength(20, ErrorMessage = "課程代碼最多 20 個字")]
    [Display(Name = "課程代碼")]
    public string CourseCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入課程名稱")]
    [StringLength(80, ErrorMessage = "課程名稱最多 80 個字")]
    [Display(Name = "課程名稱")]
    public string CourseName { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入教師姓名")]
    [StringLength(40, ErrorMessage = "教師姓名最多 40 個字")]
    [Display(Name = "教師姓名")]
    public string TeacherName { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入教室")]
    [StringLength(40, ErrorMessage = "教室最多 40 個字")]
    [Display(Name = "教室")]
    public string Classroom { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "說明最多 200 個字")]
    [Display(Name = "課程說明")]
    public string Description { get; set; } = string.Empty;
}

public sealed class SessionFormViewModel : IValidatableObject
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "請選擇課程")]
    [Display(Name = "對應課程")]
    public Guid? CourseId { get; set; }

    [Required(ErrorMessage = "請輸入課堂主題")]
    [StringLength(120, ErrorMessage = "課堂主題最多 120 個字")]
    [Display(Name = "課堂主題")]
    public string Topic { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入開始時間")]
    [Display(Name = "開始時間")]
    public DateTime? StartAt { get; set; }

    [Required(ErrorMessage = "請輸入結束時間")]
    [Display(Name = "結束時間")]
    public DateTime? EndAt { get; set; }

    [Display(Name = "建立後立即開放打卡")]
    public bool IsOpen { get; set; }

    public IReadOnlyList<CourseOptionViewModel> CourseOptions { get; init; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartAt.HasValue && EndAt.HasValue && EndAt.Value <= StartAt.Value)
        {
            yield return new ValidationResult("結束時間必須晚於開始時間。", [nameof(EndAt)]);
        }
    }
}

public sealed class CourseOptionViewModel
{
    public Guid Id { get; init; }

    public string DisplayName { get; init; } = string.Empty;
}

public sealed class QrBoardViewModel
{
    public Guid SessionId { get; init; }

    public string CourseCode { get; init; } = string.Empty;

    public string CourseName { get; init; } = string.Empty;

    public string Topic { get; init; } = string.Empty;

    public string Classroom { get; init; } = string.Empty;

    public string CheckInUrl { get; init; } = string.Empty;

    public string QrCodeSvg { get; init; } = string.Empty;

    public DateTime StartAt { get; init; }

    public DateTime EndAt { get; init; }

    public DateTime ExpiresAt { get; init; }

    public string ExpectedSsidLabel { get; init; } = string.Empty;
}
