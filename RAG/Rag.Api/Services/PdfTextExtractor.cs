using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;
using Rag.Api.Options;

namespace Rag.Api.Services;

public sealed class PdfTextExtractor
{
    private readonly string _configuredPdfToTextPath;

    public PdfTextExtractor(IOptions<RagOptions> options)
    {
        _configuredPdfToTextPath = options.Value.PdfToTextPath?.Trim() ?? string.Empty;
    }

    public List<PageText> ExtractPages(string filePath)
    {
        var executable = ResolvePdfToTextExecutable();

        var psi = new ProcessStartInfo
        {
            FileName = executable,
            Arguments = $"-q -enc UTF-8 \"{filePath}\" -",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException(BuildNotFoundMessage(executable));

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"pdftotext 執行失敗 (exit={process.ExitCode}): {error}");
        }

        return output
            .Split('\f', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select((text, idx) => new PageText(idx + 1, text))
            .Where(p => !string.IsNullOrWhiteSpace(p.Text))
            .ToList();
    }

    private string ResolvePdfToTextExecutable()
    {
        if (!string.IsNullOrWhiteSpace(_configuredPdfToTextPath) && File.Exists(_configuredPdfToTextPath))
        {
            return _configuredPdfToTextPath;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var candidates = new[]
            {
                "pdftotext.exe",
                @"C:\\Program Files\\poppler\\Library\\bin\\pdftotext.exe",
                @"C:\\Program Files\\poppler-24.08.0\\Library\\bin\\pdftotext.exe",
                @"C:\\tools\\poppler\\Library\\bin\\pdftotext.exe"
            };

            var hit = candidates.FirstOrDefault(File.Exists);
            if (!string.IsNullOrWhiteSpace(hit))
            {
                return hit;
            }

            return "pdftotext.exe";
        }

        return "pdftotext";
    }

    private string BuildNotFoundMessage(string executable)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "找不到 pdftotext.exe。請先安裝 Poppler for Windows，並將 pdftotext.exe 加入 PATH，或在 appsettings 的 Rag:PdfToTextPath 設定完整路徑。";
        }

        return $"找不到 {executable}。請安裝 poppler-utils。Ubuntu/Debian: sudo apt install -y poppler-utils";
    }
}
