using Microsoft.Extensions.Options;
using Rag.Api.Options;

namespace Rag.Api.Services;

public sealed class TextChunker
{
    private readonly int _chunkSize;
    private readonly int _chunkOverlap;

    public TextChunker(IOptions<RagOptions> options)
    {
        _chunkSize = Math.Max(200, options.Value.ChunkSize);
        _chunkOverlap = Math.Clamp(options.Value.ChunkOverlap, 0, _chunkSize - 1);
    }

    public List<ChunkedPageContent> ChunkPages(IReadOnlyList<PageText> pages)
    {
        var result = new List<ChunkedPageContent>();
        foreach (var page in pages)
        {
            var text = Normalize(page.Text);
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            var start = 0;
            var chunkIndex = 0;
            while (start < text.Length)
            {
                var length = Math.Min(_chunkSize, text.Length - start);
                var candidate = text.Substring(start, length);
                var adjustedLength = AdjustLengthToBoundary(candidate, length);
                if (adjustedLength <= 0)
                {
                    break;
                }

                var content = text.Substring(start, adjustedLength).Trim();
                if (!string.IsNullOrWhiteSpace(content))
                {
                    result.Add(new ChunkedPageContent(page.PageNumber, chunkIndex, content));
                    chunkIndex++;
                }

                if (start + adjustedLength >= text.Length)
                {
                    break;
                }

                start += Math.Max(1, adjustedLength - _chunkOverlap);
            }
        }

        return result;
    }

    private static string Normalize(string text)
    {
        return text.Replace("\r\n", "\n").Replace("\r", "\n").Trim();
    }

    private static int AdjustLengthToBoundary(string candidate, int fallbackLength)
    {
        var separators = new[] { "\n\n", "\n", "。", ".", "?", "!", ";", ",", " " };
        foreach (var sep in separators)
        {
            var idx = candidate.LastIndexOf(sep, StringComparison.Ordinal);
            if (idx > candidate.Length * 0.6)
            {
                return idx + sep.Length;
            }
        }

        return fallbackLength;
    }
}

public sealed record PageText(int PageNumber, string Text);
public sealed record ChunkedPageContent(int PageNumber, int ChunkIndex, string Content);
