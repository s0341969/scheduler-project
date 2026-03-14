namespace OrgChartSystem.Contracts;

public class SearchResultDto
{
    public int Id { get; set; }

    public int? ParentId { get; set; }

    public string DepartmentName { get; set; } = string.Empty;

    public string PersonName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;
}
