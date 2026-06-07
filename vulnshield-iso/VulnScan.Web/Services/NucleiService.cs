using System.Diagnostics;
using Microsoft.Extensions.Options;
using VulnScan.Web.Models;

namespace VulnScan.Web.Services;

public sealed class NucleiService(IOptions<VulnScanOptions> options) : INucleiService
{
    private readonly VulnScanOptions _options = options.Value;

    public bool IsInstalled()
    {
        return GetInstallPath() is not null;
    }

    public string? GetInstallPath()
    {
        var configured = _options.NucleiPath;
        if (!string.IsNullOrWhiteSpace(configured))
        {
            if (Path.IsPathRooted(configured) && File.Exists(configured))
                return configured;

            try
            {
                using var proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "where",
                        Arguments = $"nuclei",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                    }
                };
                proc.Start();
                var path = proc.StandardOutput.ReadLine();
                proc.WaitForExit(3000);
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    return path;
            }
            catch
            {
            }
        }

        return null;
    }

    public async Task<string> RunNucleiAsync(string target, string outputPath, string templateFilter, CancellationToken cancellationToken = default)
    {
        var exePath = GetInstallPath() ?? throw new InvalidOperationException("找不到 nuclei.exe。請確認已在系統 PATH 中或設定 VulnScan:NucleiPath。");

        var outDir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(outDir))
            Directory.CreateDirectory(outDir);

        var args = $"-target {EscapeArg(target)} -json -o {EscapeArg(outputPath)}";

        if (!string.IsNullOrWhiteSpace(templateFilter) && templateFilter != "All")
        {
            args += $" -t {EscapeArg(templateFilter)}";
        }

        args += " -stats-json -disable-update-check";

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            }
        };

        var outputBuilder = new System.Text.StringBuilder();
        var errorBuilder = new System.Text.StringBuilder();

        process.Start();
        await Task.WhenAll(
            ConsumeReaderAsync(process.StandardOutput, outputBuilder, cancellationToken),
            ConsumeReaderAsync(process.StandardError, errorBuilder, cancellationToken)
        );
        await process.WaitForExitAsync(cancellationToken);

        // 0 = 正常結束, 2 = 掃描完成但未發現弱點（均非錯誤）
        if (process.ExitCode != 0 && process.ExitCode != 2)
        {
            var error = errorBuilder.ToString();
            throw new InvalidOperationException($"nuclei 執行失敗 (exit code {process.ExitCode}): {error}");
        }

        return outputPath;
    }

    private static async Task ConsumeReaderAsync(StreamReader reader, System.Text.StringBuilder builder, CancellationToken cancellationToken)
    {
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            builder.AppendLine(line);
        }
    }

    private static string EscapeArg(string arg)
    {
        return $"\"{arg.Replace("\"", "\\\"")}\"";
    }
}
