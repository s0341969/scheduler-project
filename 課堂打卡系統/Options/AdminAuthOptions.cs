namespace 課堂打卡系統.Options;

public sealed class AdminAuthOptions
{
    public const string SectionName = "AdminAuth";

    public string Username { get; set; } = "admin";

    public string DisplayName { get; set; } = "系統管理員";

    public string Password { get; set; } = "ChangeMe123!";

    public string PasswordHashBase64 { get; set; } = string.Empty;

    public string SaltBase64 { get; set; } = string.Empty;

    public int Iterations { get; set; } = 100_000;
}
