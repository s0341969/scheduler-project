using System.Diagnostics;
using Microsoft.Extensions.Options;
using VulnScan.Web.Models;

namespace VulnScan.Web.Services;

public sealed class NmapService(IOptions<VulnScanOptions> options) : INmapService
{
    private readonly VulnScanOptions _options = options.Value;

    public async Task<string> RunNmapAsync(string target, string outputPath, string scanProfile, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            throw new InvalidOperationException("Nmap target 不可空白。");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? _options.ResultRootPath);

        var arguments = $"{BuildProfileArguments(scanProfile)} -oX \"{outputPath}\" {target}";
        var startInfo = new ProcessStartInfo
        {
            FileName = _options.NmapPath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);
            throw new InvalidOperationException($"Nmap 執行失敗：{error}");
        }

        return outputPath;
    }

    private static string BuildProfileArguments(string scanProfile) => scanProfile switch
    {
        "Low" => "-sV",
        "Deep" => "-sV -O --version-all",
        _ => "-sV -O",
    };
}
