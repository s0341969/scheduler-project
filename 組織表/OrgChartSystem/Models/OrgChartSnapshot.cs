namespace OrgChartSystem.Models;

public class OrgChartSnapshot
{
    public int Id { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string DataJson { get; set; } = string.Empty;

    public string Actor { get; set; } = "system";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
