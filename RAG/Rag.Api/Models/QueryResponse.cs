namespace Rag.Api.Models;

public sealed record QueryResponse(string Answer, IReadOnlyList<Citation> Citations);
