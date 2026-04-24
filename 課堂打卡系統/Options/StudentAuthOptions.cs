namespace 課堂打卡系統.Options;

public sealed class StudentAuthOptions
{
    public const string SectionName = "StudentAuth";

    public List<StudentAccountOption> Accounts { get; set; } = [];
}

public sealed class StudentAccountOption
{
    public string StudentNumber { get; set; } = string.Empty;

    public string StudentName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
