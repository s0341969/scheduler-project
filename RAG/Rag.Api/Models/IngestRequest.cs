namespace Rag.Api.Models;

public sealed record IngestRequest(string DirectoryPath, bool Recursive = true);
