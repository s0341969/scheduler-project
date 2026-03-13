namespace OrgChartSystem.Contracts;

public class ImportOrgChartRequest
{
    public string? DisplayMode { get; set; }

    public List<ImportOrgNodeRequest> Nodes { get; set; } = [];
}

public class ImportOrgNodeRequest
{
    public string Code { get; set; } = string.Empty;

    public string DepartmentName { get; set; } = string.Empty;

    public string PersonName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public List<ImportOrgNodeRequest> Children { get; set; } = [];
}
