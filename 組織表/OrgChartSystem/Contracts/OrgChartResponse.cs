namespace OrgChartSystem.Contracts;

public class OrgChartResponse
{
    public string DisplayMode { get; set; } = "person";

    public List<OrgNodeDto> Nodes { get; set; } = [];
}

public class OrgNodeDto
{
    public int Id { get; set; }

    public int? ParentId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string DepartmentName { get; set; } = string.Empty;

    public string PersonName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public List<OrgNodeDto> Children { get; set; } = [];
}
