namespace OrgChartSystem.Contracts;

public class NodeMutationRequest
{
    public int? ParentId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string DepartmentName { get; set; } = string.Empty;

    public string PersonName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;
}
