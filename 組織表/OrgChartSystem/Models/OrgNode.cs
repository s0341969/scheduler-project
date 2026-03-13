namespace OrgChartSystem.Models;

public class OrgNode
{
    public int Id { get; set; }

    public int? ParentId { get; set; }

    public OrgNode? Parent { get; set; }

    public List<OrgNode> Children { get; set; } = [];

    public string Code { get; set; } = string.Empty;

    public string DepartmentName { get; set; } = string.Empty;

    public string PersonName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
