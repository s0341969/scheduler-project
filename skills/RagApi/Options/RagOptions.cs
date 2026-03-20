namespace RagApi.Options;

public sealed class RagOptions
{
    public const string SectionName = "Rag";

    public string ConnectionString { get; set; } = string.Empty;
    public string PdfRootPath { get; set; } = "./pdfs";
    public int ChunkSize { get; set; } = 1200;
    public int ChunkOverlap { get; set; } = 200;
    public int DefaultTopK { get; set; } = 5;
    public int MaxTopK { get; set; } = 20;
}
