using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using 課堂打卡系統.Options;

namespace 課堂打卡系統.Services;

public interface IAdminAuthService
{
    bool ValidateCredentials(string username, string password);

    string GetDisplayName();

    string GetUsername();
}

public sealed class AdminAuthService : IAdminAuthService
{
    private readonly AdminAuthOptions _options;

    public AdminAuthService(IOptions<AdminAuthOptions> options)
    {
        _options = options.Value;
    }

    public bool ValidateCredentials(string username, string password)
    {
        if (!string.Equals(username.Trim(), _options.Username.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(_options.PasswordHashBase64) && !string.IsNullOrWhiteSpace(_options.SaltBase64))
        {
            return VerifyHashedPassword(password);
        }

        return string.Equals(password, _options.Password, StringComparison.Ordinal);
    }

    public string GetDisplayName()
    {
        return _options.DisplayName;
    }

    public string GetUsername()
    {
        return _options.Username;
    }

    private bool VerifyHashedPassword(string password)
    {
        try
        {
            var salt = Convert.FromBase64String(_options.SaltBase64);
            var expectedHash = Convert.FromBase64String(_options.PasswordHashBase64);
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var actualHash = Rfc2898DeriveBytes.Pbkdf2(passwordBytes, salt, _options.Iterations, HashAlgorithmName.SHA256, expectedHash.Length);
            return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
