using Npgsql;
using RagApi.Services;
using Microsoft.Extensions.Options;
using RagApi.Options;

namespace RagApi.Repositories;

public sealed class RagRepository(IOptions<RagOptions> options)
{
    private readonly string _connectionString = string.IsNullOrWhiteSpace(options.Value.ConnectionString)
        ? throw new InvalidOperationException("Rag:ConnectionString 未設定。")
        : options.Value.ConnectionString;

    public async Task EnsureSchemaAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            CREATE EXTENSION IF NOT EXISTS vector;

            CREATE TABLE IF NOT EXISTS documents (
                id BIGSERIAL PRIMARY KEY,
                file_path TEXT NOT NULL UNIQUE,
                title TEXT NOT NULL,
                file_hash TEXT NOT NULL,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
            );

            CREATE TABLE IF NOT EXISTS chunks (
                id BIGSERIAL PRIMARY KEY,
                document_id BIGINT NOT NULL REFERENCES documents(id) ON DELETE CASCADE,
                chunk_index INT NOT NULL,
                page_number INT NOT NULL,
                content TEXT NOT NULL,
                embedding vector(1536) NOT NULL,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                UNIQUE(document_id, chunk_index)
            );

            CREATE INDEX IF NOT EXISTS idx_chunks_document ON chunks(document_id);
            CREATE INDEX IF NOT EXISTS idx_chunks_embedding ON chunks USING ivfflat (embedding vector_cosine_ops) WITH (lists = 100);
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<DocumentRow?> GetDocumentByPathAsync(string filePath, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id, file_path, title, file_hash
            FROM documents
            WHERE file_path = @file_path
            LIMIT 1;
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("file_path", filePath);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new DocumentRow(
            reader.GetInt64(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3));
    }

    public async Task UpsertDocumentWithChunksAsync(
        string filePath,
        string title,
        string fileHash,
        IReadOnlyCollection<(ChunkInput chunk, float[] embedding)> entries,
        CancellationToken cancellationToken)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var tx = await conn.BeginTransactionAsync(cancellationToken);

        var documentId = await UpsertDocumentAsync(conn, tx, filePath, title, fileHash, cancellationToken);

        const string deleteSql = "DELETE FROM chunks WHERE document_id = @document_id;";
        await using (var deleteCmd = new NpgsqlCommand(deleteSql, conn, tx))
        {
            deleteCmd.Parameters.AddWithValue("document_id", documentId);
            await deleteCmd.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var entry in entries)
        {
            const string insertChunkSql = """
                INSERT INTO chunks(document_id, chunk_index, page_number, content, embedding)
                VALUES (@document_id, @chunk_index, @page_number, @content, @embedding::vector);
                """;

            await using var insertCmd = new NpgsqlCommand(insertChunkSql, conn, tx);
            insertCmd.Parameters.AddWithValue("document_id", documentId);
            insertCmd.Parameters.AddWithValue("chunk_index", entry.chunk.ChunkIndex);
            insertCmd.Parameters.AddWithValue("page_number", entry.chunk.PageNumber);
            insertCmd.Parameters.AddWithValue("content", entry.chunk.Content);
            insertCmd.Parameters.AddWithValue("embedding", ToVectorLiteral(entry.embedding));
            await insertCmd.ExecuteNonQueryAsync(cancellationToken);
        }

        await tx.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SearchRow>> SearchAsync(float[] embedding, int topK, string? userId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT d.file_path, d.title, c.page_number, c.content,
                   1 - (c.embedding <=> @embedding::vector) AS score
            FROM chunks c
            INNER JOIN documents d ON d.id = c.document_id
            ORDER BY c.embedding <=> @embedding::vector
            LIMIT @top_k;
            """;

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("embedding", ToVectorLiteral(embedding));
        cmd.Parameters.AddWithValue("top_k", topK);

        var rows = new List<SearchRow>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new SearchRow(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetInt32(2),
                reader.GetString(3),
                reader.GetDouble(4)));
        }

        return rows;
    }

    private static async Task<long> UpsertDocumentAsync(
        NpgsqlConnection conn,
        NpgsqlTransaction tx,
        string filePath,
        string title,
        string fileHash,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO documents(file_path, title, file_hash, updated_at)
            VALUES (@file_path, @title, @file_hash, NOW())
            ON CONFLICT (file_path)
            DO UPDATE SET title = EXCLUDED.title,
                          file_hash = EXCLUDED.file_hash,
                          updated_at = NOW()
            RETURNING id;
            """;

        await using var cmd = new NpgsqlCommand(sql, conn, tx);
        cmd.Parameters.AddWithValue("file_path", filePath);
        cmd.Parameters.AddWithValue("title", title);
        cmd.Parameters.AddWithValue("file_hash", fileHash);

        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        if (result is null)
        {
            throw new InvalidOperationException("寫入 documents 失敗。無法取得 document id。");
        }

        return Convert.ToInt64(result);
    }

    private static string ToVectorLiteral(float[] vector)
    {
        var items = vector.Select(v => v.ToString("G9", System.Globalization.CultureInfo.InvariantCulture));
        return $"[{string.Join(',', items)}]";
    }
}

public sealed record DocumentRow(long Id, string FilePath, string Title, string FileHash);
public sealed record SearchRow(string DocumentPath, string Title, int PageNumber, string Content, double Score);
