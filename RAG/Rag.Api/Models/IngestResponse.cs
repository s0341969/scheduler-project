namespace Rag.Api.Models;

public sealed record IngestResponse(int TotalFiles, int IngestedFiles, int SkippedFiles, string Message);
