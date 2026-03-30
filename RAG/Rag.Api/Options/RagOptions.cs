namespace Rag.Api.Options;

public sealed class RagOptions
{
    public const string SectionName = "Rag";

    public string ConnectionString { get; set; } = "Host=localhost;Port=5432;Database=ragdb;Username=raguser;Password=ragpass";
    public int ChunkSize { get; set; } = 900;
    public int ChunkOverlap { get; set; } = 120;
}
