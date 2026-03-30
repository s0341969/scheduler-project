namespace Rag.Api.Models;

public sealed record ChunkMatch(string SourcePath, int PageNumber, string Content, double Similarity);
