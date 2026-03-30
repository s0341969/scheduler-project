namespace Rag.Api.Models;

public sealed record Citation(string SourcePath, int PageNumber, double Similarity);
