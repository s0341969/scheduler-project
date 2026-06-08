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

        args += " -disable-update-check";

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
            }
        };

        // 只收集 stderr（錯誤訊息），stdout 透過 -o 直接寫檔
        var errorBuilder = new System.Text.StringBuilder();

        process.Start();

        // 非同步讀取 stderr，避免緩衝區滿造成 deadlock
        var stderrTask = ConsumeReaderAsync(process.StandardError, errorBuilder, cancellationToken);

        // 等候 process 結束，最多 60 分鐘
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromMinutes(60));

        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
            await stderrTask;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // 逾時觸發
            try { process.Kill(entireProcessTree: true); } catch { }
            throw new InvalidOperationException("nuclei 執行逾時（超過 60 分鐘），已強制終止。請檢查目標是否可達或縮小掃描範圍後重試。");
        }
        catch (OperationCanceledException)
        {
            // 外部 cancellation（如 Hangfire shutdown）
            try { process.Kill(entireProcessTree: true); } catch { }
            throw;
        }

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
        try
        {
            while (await reader.ReadLineAsync(cancellationToken) is { } line)
            {
                builder.AppendLine(line);
            }
        }
        catch (OperationCanceledException)
        {
            // process 已強制終止時 stream 可能也會拋異常，直接忽略
        }
    }

    private static string EscapeArg(string arg)
    {
        return $"\"{arg.Replace("\"", "\\\"")}\"";
    }
}
