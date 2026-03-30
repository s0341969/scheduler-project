namespace Rag.Api.Models;

public sealed record ChunkRow(int PageNumber, int ChunkIndex, string Content, float[] Embedding);
