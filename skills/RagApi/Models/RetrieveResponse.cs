namespace RagApi.Models;

public sealed class RetrieveResultItem
{
    public string DocumentId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Page { get; set; }
    public double Score { get; set; }
    public string Snippet { get; set; } = string.Empty;
}

public sealed class RetrieveResponse
{
    public required List<RetrieveResultItem> Results { get; init; }
}
