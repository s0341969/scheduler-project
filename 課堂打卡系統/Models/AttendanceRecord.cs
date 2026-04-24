namespace 課堂打卡系統.Models;

public sealed class AttendanceRecord
{
    public Guid Id { get; set; }

    public Guid SessionId { get; set; }

    public string StudentNumber { get; set; } = string.Empty;

    public string StudentName { get; set; } = string.Empty;

    public DateTimeOffset CheckedInAt { get; set; }

    public string Note { get; set; } = string.Empty;

    public string SourceIp { get; set; } = string.Empty;

    public string UserAgent { get; set; } = string.Empty;

    public string DeviceId { get; set; } = string.Empty;

    public string DeviceFingerprint { get; set; } = string.Empty;

    public DateTimeOffset QrIssuedAtUtc { get; set; }

    public DateTimeOffset QrExpiresAtUtc { get; set; }

    public bool IsSuspicious { get; set; }

    public List<string> SuspiciousReasons { get; set; } = [];

    public bool IsWithinAllowedNetwork { get; set; }
}
