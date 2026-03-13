namespace OrgChartSystem.Models;

public class OrgChartSetting
{
    public int Id { get; set; } = 1;

    public string DisplayMode { get; set; } = "person";

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
