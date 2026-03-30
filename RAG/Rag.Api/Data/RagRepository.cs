using Microsoft.Extensions.Options;
using Npgsql;
using Rag.Api.Models;
using Rag.Api.Options;

namespace Rag.Api.Data;

public sealed class RagRepository
{
    private readonly string _connectionString;

    public RagRepository(IOptions<RagOptions> options)
    {
        _connectionString = options.Value.ConnectionString;
    }

    public async Task<bool> ShouldIngestDocumentAsync(string sourcePath, string contentHash, CancellationToken ct)
    {
        const string sql = @"
SELECT content_hash
FROM rag_documents
WHERE source_path = @source_path;";

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("source_path", sourcePath);

        var existingHash = await cmd.ExecuteScalarAsync(ct) as string;
        return !string.Equals(existingHash, contentHash, StringComparison.OrdinalIgnoreCase);
    }

    public async Task UpsertDocumentWithChunksAsync(
        string sourcePath,
        string contentHash,
        IReadOnlyList<ChunkRow> chunks,
        CancellationToken ct)
    {
        if (chunks.Count == 0)
        {
            return;
        }

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        await using var tx = await conn.BeginTransactionAsync(ct);

        var docId = await UpsertDocumentAsync(conn, tx, sourcePath, contentHash, ct);

        const string deleteSql = "DELETE FROM rag_chunks WHERE doc_id = @doc_id;";
        await using (var deleteCmd = new NpgsqlCommand(deleteSql, conn, tx))
        {
            deleteCmd.Parameters.AddWithValue("doc_id", docId);
            await deleteCmd.ExecuteNonQueryAsync(ct);
        }

        const string insertSql = @"
INSERT INTO rag_chunks (doc_id, source_path, page_number, chunk_index, content, embedding)
VALUES (@doc_id, @source_path, @page_number, @chunk_index, @content, @embedding::vector);";

        foreach (var chunk in chunks)
        {
            await using var insertCmd = new NpgsqlCommand(insertSql, conn, tx);
            insertCmd.Parameters.AddWithValue("doc_id", docId);
            insertCmd.Parameters.AddWithValue("source_path", sourcePath);
            insertCmd.Parameters.AddWithValue("page_number", chunk.PageNumber);
            insertCmd.Parameters.AddWithValue("chunk_index", chunk.ChunkIndex);
            insertCmd.Parameters.AddWithValue("content", chunk.Content);
            insertCmd.Parameters.AddWithValue("embedding", ToVectorLiteral(chunk.Embedding));
            await insertCmd.ExecuteNonQueryAsync(ct);
        }

        await tx.CommitAsync(ct);
    }

    public async Task<List<ChunkMatch>> SearchSimilarChunksAsync(float[] queryEmbedding, int topK, CancellationToken ct)
    {
        const string sql = @"
SELECT source_path, page_number, content,
       1 - (embedding <=> @query_embedding::vector) AS similarity
FROM rag_chunks
ORDER BY embedding <=> @query_embedding::vector
LIMIT @top_k;";

        var matches = new List<ChunkMatch>();

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("query_embedding", ToVectorLiteral(queryEmbedding));
        cmd.Parameters.AddWithValue("top_k", topK);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            matches.Add(new ChunkMatch(
                reader.GetString(0),
                reader.GetInt32(1),
                reader.GetString(2),
                reader.GetDouble(3)));
        }

        return matches;
    }

    private static async Task<long> UpsertDocumentAsync(
        NpgsqlConnection conn,
        NpgsqlTransaction tx,
        string sourcePath,
        string contentHash,
        CancellationToken ct)
    {
        const string sql = @"
INSERT INTO rag_documents (source_path, content_hash, indexed_at)
VALUES (@source_path, @content_hash, now())
ON CONFLICT (source_path)
DO UPDATE SET content_hash = EXCLUDED.content_hash,
              indexed_at = EXCLUDED.indexed_at
RETURNING id;";

        await using var cmd = new NpgsqlCommand(sql, conn, tx);
        cmd.Parameters.AddWithValue("source_path", sourcePath);
        cmd.Parameters.AddWithValue("content_hash", contentHash);

        var result = await cmd.ExecuteScalarAsync(ct)
                     ?? throw new InvalidOperationException("無法取得文件 ID");

        return Convert.ToInt64(result);
    }

    private static string ToVectorLiteral(IReadOnlyList<float> values)
    {
        return "[" + string.Join(',', values.Select(v => v.ToString(System.Globalization.CultureInfo.InvariantCulture))) + "]";
    }
}
