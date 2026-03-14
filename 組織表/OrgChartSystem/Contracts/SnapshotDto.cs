namespace OrgChartSystem.Contracts;

public class SnapshotDto
{
    public int Id { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string Actor { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}
