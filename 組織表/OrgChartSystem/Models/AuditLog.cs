namespace OrgChartSystem.Models;

public class AuditLog
{
    public int Id { get; set; }

    public string Action { get; set; } = string.Empty;

    public int? NodeId { get; set; }

    public string Detail { get; set; } = string.Empty;

    public string Actor { get; set; } = "system";

    public string Role { get; set; } = "unknown";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
