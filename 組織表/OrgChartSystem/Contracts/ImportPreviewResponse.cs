namespace OrgChartSystem.Contracts;

public class ImportPreviewResponse
{
    public int CurrentNodeCount { get; set; }

    public int IncomingNodeCount { get; set; }

    public int RootCount { get; set; }

    public int MaxDepth { get; set; }

    public int Delta => IncomingNodeCount - CurrentNodeCount;

    public List<string> Warnings { get; set; } = [];
}
