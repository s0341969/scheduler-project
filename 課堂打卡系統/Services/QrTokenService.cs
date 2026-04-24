using System.Globalization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using 課堂打卡系統.Options;

namespace 課堂打卡系統.Services;

public interface IQrTokenService
{
    QrTokenPayload CreateToken(Guid sessionId);

    QrTokenValidationResult ValidateToken(Guid expectedSessionId, string? token);
}

public sealed class QrTokenService : IQrTokenService
{
    private readonly IDataProtector _protector;
    private readonly TimeProvider _timeProvider;
    private readonly AttendanceSecurityOptions _options;

    public QrTokenService(IDataProtectionProvider dataProtectionProvider, TimeProvider timeProvider, IOptions<AttendanceSecurityOptions> options)
    {
        _protector = dataProtectionProvider.CreateProtector("Attendance.QrToken.v1");
        _timeProvider = timeProvider;
        _options = options.Value;
    }

    public QrTokenPayload CreateToken(Guid sessionId)
    {
        var issuedAt = _timeProvider.GetUtcNow();
        var raw = string.Join('|',
            sessionId.ToString("D"),
            issuedAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
            Guid.NewGuid().ToString("N"));

        return new QrTokenPayload
        {
            Token = _protector.Protect(raw),
            IssuedAtUtc = issuedAt,
            ExpiresAtUtc = issuedAt.AddMinutes(_options.QrTokenLifetimeMinutes)
        };
    }

    public QrTokenValidationResult ValidateToken(Guid expectedSessionId, string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return new QrTokenValidationResult { IsValid = false, ErrorMessage = "此打卡頁缺少 QR 驗證資訊，請重新掃描課堂 QR Code。" };
        }

        try
        {
            var raw = _protector.Unprotect(token);
            var parts = raw.Split('|', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3 || !Guid.TryParse(parts[0], out var sessionId) || !long.TryParse(parts[1], out var unixTime))
            {
                return new QrTokenValidationResult { IsValid = false, ErrorMessage = "QR 驗證資訊格式錯誤，請重新掃描。" };
            }

            if (sessionId != expectedSessionId)
            {
                return new QrTokenValidationResult { IsValid = false, ErrorMessage = "此 QR Code 不屬於目前課堂，請重新掃描正確課堂的 QR Code。" };
            }

            var issuedAt = DateTimeOffset.FromUnixTimeSeconds(unixTime);
            var expiresAt = issuedAt.AddMinutes(_options.QrTokenLifetimeMinutes);
            var now = _timeProvider.GetUtcNow();
            if (now > expiresAt)
            {
                return new QrTokenValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"QR Code 已於 {expiresAt.LocalDateTime:HH:mm:ss} 失效，請重新掃描最新 QR Code。"
                };
            }

            return new QrTokenValidationResult
            {
                IsValid = true,
                Token = token,
                IssuedAtUtc = issuedAt,
                ExpiresAtUtc = expiresAt
            };
        }
        catch
        {
            return new QrTokenValidationResult { IsValid = false, ErrorMessage = "QR 驗證失敗，請重新掃描課堂 QR Code。" };
        }
    }
}

public sealed class QrTokenPayload
{
    public string Token { get; init; } = string.Empty;

    public DateTimeOffset IssuedAtUtc { get; init; }

    public DateTimeOffset ExpiresAtUtc { get; init; }
}

public sealed class QrTokenValidationResult
{
    public bool IsValid { get; init; }

    public string ErrorMessage { get; init; } = string.Empty;

    public string Token { get; init; } = string.Empty;

    public DateTimeOffset IssuedAtUtc { get; init; }

    public DateTimeOffset ExpiresAtUtc { get; init; }
}
