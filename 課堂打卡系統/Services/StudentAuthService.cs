using Microsoft.Extensions.Options;
using 課堂打卡系統.Options;

namespace 課堂打卡系統.Services;

public interface IStudentAuthService
{
    StudentIdentity? ValidateCredentials(string studentNumber, string password);
}

public sealed class StudentAuthService : IStudentAuthService
{
    private readonly StudentAuthOptions _options;

    public StudentAuthService(IOptions<StudentAuthOptions> options)
    {
        _options = options.Value;
    }

    public StudentIdentity? ValidateCredentials(string studentNumber, string password)
    {
        var normalizedStudentNumber = studentNumber.Trim().ToUpperInvariant();
        var account = _options.Accounts.FirstOrDefault(item =>
            string.Equals(item.StudentNumber.Trim(), normalizedStudentNumber, StringComparison.OrdinalIgnoreCase));

        if (account is null || !string.Equals(account.Password, password, StringComparison.Ordinal))
        {
            return null;
        }

        return new StudentIdentity
        {
            StudentNumber = account.StudentNumber.Trim().ToUpperInvariant(),
            StudentName = account.StudentName.Trim()
        };
    }
}

public sealed class StudentIdentity
{
    public string StudentNumber { get; init; } = string.Empty;

    public string StudentName { get; init; } = string.Empty;
}
