namespace 課堂打卡系統.Options;

public sealed class AttendanceSecurityOptions
{
    public const string SectionName = "AttendanceSecurity";

    public int QrTokenLifetimeMinutes { get; set; } = 5;

    public bool StrictNetworkValidation { get; set; }

    public List<string> AllowedIpPrefixes { get; set; } = [];

    public string ExpectedSsidLabel { get; set; } = "學校班級 SSID";
}
