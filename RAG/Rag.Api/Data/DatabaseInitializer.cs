using Microsoft.Extensions.Options;
using Npgsql;
using Rag.Api.Options;

namespace Rag.Api.Data;

public sealed class DatabaseInitializer
{
    private readonly string _connectionString;

    public DatabaseInitializer(IOptions<RagOptions> options)
    {
        _connectionString = options.Value.ConnectionString;
    }

    public async Task InitializeAsync(CancellationToken ct)
    {
        const string sql = @"
CREATE EXTENSION IF NOT EXISTS vector;

CREATE TABLE IF NOT EXISTS rag_documents (
    id BIGSERIAL PRIMARY KEY,
    source_path TEXT NOT NULL UNIQUE,
    content_hash TEXT NOT NULL,
    indexed_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS rag_chunks (
    id BIGSERIAL PRIMARY KEY,
    doc_id BIGINT NOT NULL REFERENCES rag_documents(id) ON DELETE CASCADE,
    source_path TEXT NOT NULL,
    page_number INT NOT NULL,
    chunk_index INT NOT NULL,
    content TEXT NOT NULL,
    embedding vector(768) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_rag_chunks_doc_id ON rag_chunks(doc_id);
CREATE INDEX IF NOT EXISTS ix_rag_chunks_source_path ON rag_chunks(source_path);

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_indexes
        WHERE schemaname = 'public'
          AND indexname = 'ix_rag_chunks_embedding_ivfflat'
    ) THEN
        CREATE INDEX ix_rag_chunks_embedding_ivfflat
            ON rag_chunks
            USING ivfflat (embedding vector_cosine_ops)
            WITH (lists = 100);
    END IF;
END $$;
";

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
