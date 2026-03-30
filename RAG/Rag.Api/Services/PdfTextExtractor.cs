using System.Diagnostics;

namespace Rag.Api.Services;

public sealed class PdfTextExtractor
{
    public List<PageText> ExtractPages(string filePath)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "pdftotext",
            Arguments = $"-q -enc UTF-8 \"{filePath}\" -",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("無法啟動 pdftotext，請確認主機已安裝 poppler-utils。\nLinux: sudo apt install poppler-utils");

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"pdftotext 失敗: {error}");
        }

        var pages = output
            .Split('\f', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select((text, idx) => new PageText(idx + 1, text))
            .Where(p => !string.IsNullOrWhiteSpace(p.Text))
            .ToList();

        return pages;
    }
}
