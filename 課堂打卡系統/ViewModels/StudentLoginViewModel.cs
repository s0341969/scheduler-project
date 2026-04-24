using System.ComponentModel.DataAnnotations;

namespace 課堂打卡系統.ViewModels;

public sealed class StudentLoginViewModel
{
    [Required(ErrorMessage = "請輸入學號")]
    [Display(Name = "學號")]
    public string StudentNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入密碼")]
    [DataType(DataType.Password)]
    [Display(Name = "密碼")]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
