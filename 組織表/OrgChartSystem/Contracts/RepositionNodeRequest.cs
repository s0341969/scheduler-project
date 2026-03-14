namespace OrgChartSystem.Contracts;

public class RepositionNodeRequest
{
    public int? ParentId { get; set; }

    public int? Index { get; set; }
}
