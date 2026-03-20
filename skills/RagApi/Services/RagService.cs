using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using RagApi.Models;
using RagApi.Options;
using RagApi.Repositories;

namespace RagApi.Services;

public sealed class RagService(
    RagRepository repository,
    OpenAiEmbeddingClient embeddingClient,
    PdfTextExtractor pdfTextExtractor,
    IOptions<RagOptions> ragOptions,
    ILogger<RagService> logger)
{
    private readonly RagOptions _options = ragOptions.Value;

    public async Task<RetrieveResponse> RetrieveAsync(RetrieveRequest request, CancellationToken cancellationToken)
    {
        var topK = Math.Clamp(request.TopK ?? _options.DefaultTopK, 1, _options.MaxTopK);
        var embedding = await embeddingClient.CreateEmbeddingAsync(request.Query.Trim(), cancellationToken);
        var rows = await repository.SearchAsync(embedding, topK, request.UserId, cancellationToken);

        return new RetrieveResponse
        {
            Results = rows.Select(r => new RetrieveResultItem
            {
                DocumentId = r.DocumentPath,
                Title = r.Title,
                Page = r.PageNumber,
                Score = Math.Round(r.Score, 6),
                Snippet = r.Content.Length > 300 ? string.Concat(r.Content.AsSpan(0, 300), "...") : r.Content
            }).ToList()
        };
    }

    public async Task<IndexFolderResponse> IndexFolderAsync(IndexFolderRequest request, CancellationToken cancellationToken)
    {
        var folderPath = string.IsNullOrWhiteSpace(request.FolderPath) ? _options.PdfRootPath : request.FolderPath.Trim();
        if (!Directory.Exists(folderPath))
        {
            throw new DirectoryNotFoundException($"找不到資料夾：{folderPath}");
        }

        var files = Directory.GetFiles(folderPath, "*.pdf", SearchOption.AllDirectories)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var maxFiles = request.MaxFiles is > 0 ? request.MaxFiles.Value : files.Count;
        files = files.Take(maxFiles).ToList();

        var errors = new List<string>();
        var indexed = 0;
        var skipped = 0;

        foreach (var file in files)
        {
            try
            {
                var hash = ComputeSha256(file);
                var existing = await repository.GetDocumentByPathAsync(file, cancellationToken);
                if (existing is not null && string.Equals(existing.FileHash, hash, StringComparison.OrdinalIgnoreCase))
                {
                    skipped++;
                    continue;
                }

                var pages = pdfTextExtractor.ExtractPageTexts(file);
                if (pages.Count == 0)
                {
                    logger.LogWarning("PDF 無可用文字，已略過：{File}", file);
                    skipped++;
                    continue;
                }

                var chunks = CreateChunks(pages, _options.ChunkSize, _options.ChunkOverlap);
                if (chunks.Count == 0)
                {
                    skipped++;
                    continue;
                }

                var embeddings = new List<(ChunkInput chunk, float[] embedding)>(chunks.Count);
                foreach (var chunk in chunks)
                {
                    var embedding = await embeddingClient.CreateEmbeddingAsync(chunk.Content, cancellationToken);
                    embeddings.Add((chunk, embedding));
                }

                await repository.UpsertDocumentWithChunksAsync(file, Path.GetFileName(file), hash, embeddings, cancellationToken);
                indexed++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "處理 PDF 失敗：{File}", file);
                errors.Add($"{file}: {ex.Message}");
            }
        }

        return new IndexFolderResponse
        {
            TotalFiles = files.Count,
            IndexedFiles = indexed,
            SkippedFiles = skipped,
            Errors = errors
        };
    }

    private static string ComputeSha256(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(stream);
        return Convert.ToHexString(hash);
    }

    private static List<ChunkInput> CreateChunks(IReadOnlyList<PdfPageText> pages, int size, int overlap)
    {
        var normalizedSize = Math.Max(300, size);
        var normalizedOverlap = Math.Clamp(overlap, 0, normalizedSize - 50);
        var chunks = new List<ChunkInput>();
        var index = 0;

        foreach (var page in pages)
        {
            var text = NormalizeWhitespace(page.Text);
            if (text.Length < 10)
            {
                continue;
            }

            var start = 0;
            while (start < text.Length)
            {
                var length = Math.Min(normalizedSize, text.Length - start);
                var segment = text.Substring(start, length).Trim();
                if (!string.IsNullOrWhiteSpace(segment))
                {
                    chunks.Add(new ChunkInput(index, page.PageNumber, segment));
                    index++;
                }

                if (start + length >= text.Length)
                {
                    break;
                }

                start += Math.Max(1, length - normalizedOverlap);
            }
        }

        return chunks;
    }

    private static string NormalizeWhitespace(string input)
    {
        var sb = new StringBuilder(input.Length);
        var inWhitespace = false;

        foreach (var ch in input)
        {
            if (char.IsWhiteSpace(ch))
            {
                if (!inWhitespace)
                {
                    sb.Append(' ');
                    inWhitespace = true;
                }

                continue;
            }

            sb.Append(ch);
            inWhitespace = false;
        }

        return sb.ToString().Trim();
    }
}

public sealed record ChunkInput(int ChunkIndex, int PageNumber, string Content);
