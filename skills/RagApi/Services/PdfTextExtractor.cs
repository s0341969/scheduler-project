using UglyToad.PdfPig;

namespace RagApi.Services;

public sealed class PdfTextExtractor
{
    public IReadOnlyList<PdfPageText> ExtractPageTexts(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("找不到 PDF 檔案。", filePath);
        }

        var pages = new List<PdfPageText>();

        using var document = PdfDocument.Open(filePath);
        foreach (var page in document.GetPages())
        {
            var text = page.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(text))
            {
                pages.Add(new PdfPageText(page.Number, text));
            }
        }

        return pages;
    }
}

public sealed record PdfPageText(int PageNumber, string Text);
