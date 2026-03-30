namespace Rag.Api.Models;

public sealed record QueryRequest(string Question, int? TopK = 5);
