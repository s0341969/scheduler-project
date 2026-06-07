using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;
using VulnScan.Web.Models;

namespace VulnScan.Web.Services;

public sealed class NmapService(IOptions<VulnScanOptions> options) : INmapService
{
    private readonly VulnScanOptions _options = options.Value;

    public NmapInstallationStatus GetInstallationStatus()
    {
        var configured = _options.NmapPath?.Trim();
        if (!string.IsNullOrWhiteSpace(configured))
        {
            if (Path.IsPathRooted(configured) && File.Exists(configured))
            {
                return new NmapInstallationStatus
                {
                    IsInstalled = true,
                    ResolvedPath = configured,
                    Source = "VulnScan:NmapPath",
                    Message = "已從設定檔直接找到 Nmap 執行檔。",
                };
            }

            var commandResolved = TryResolveFromCommand(configured);
            if (!string.IsNullOrWhiteSpace(commandResolved))
            {
                return new NmapInstallationStatus
                {
                    IsInstalled = true,
                    ResolvedPath = commandResolved,
                    Source = "PATH / 命令解析",
                    Message = "已透過系統命令解析找到 Nmap 執行檔。",
                };
            }
        }

        foreach (var candidate in EnumerateDefaultCandidates())
        {
            if (File.Exists(candidate))
            {
                return new NmapInstallationStatus
                {
                    IsInstalled = true,
                    ResolvedPath = candidate,
                    Source = "預設安裝路徑",
                    Message = "已從常見安裝路徑找到 Nmap 執行檔。",
                };
            }
        }

        return new NmapInstallationStatus
        {
            IsInstalled = false,
            ResolvedPath = string.Empty,
            Source = string.IsNullOrWhiteSpace(configured) ? "未設定" : $"設定值：{configured}",
            Message = "找不到 Nmap 執行檔。請先安裝 Nmap，或在 VulnScan 設定中把 `NmapPath` 指向有效的 nmap.exe。",
        };
    }

    public async Task<string> RunNmapAsync(string target, string outputPath, string scanProfile, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            throw new InvalidOperationException("Nmap target 不可空白。");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? _options.ResultRootPath);
        var status = GetInstallationStatus();
        if (!status.IsInstalled || string.IsNullOrWhiteSpace(status.ResolvedPath))
        {
            throw new InvalidOperationException(status.Message);
        }

        var nmapPath = status.ResolvedPath;

        var arguments = $"{BuildProfileArguments(scanProfile)} -oX \"{outputPath}\" {EscapeArg(target)}";
        var startInfo = new ProcessStartInfo
        {
            FileName = nmapPath,
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

    private static string? TryResolveFromCommand(string command)
    {
        try
        {
            var resolver = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "where.exe" : "which";
            var startInfo = new ProcessStartInfo
            {
                FileName = resolver,
                Arguments = command,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return null;
            }

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                return null;
            }

            return output
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    private static IEnumerable<string> EnumerateDefaultCandidates()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            yield return @"C:\Program Files (x86)\Nmap\nmap.exe";
            yield return @"C:\Program Files\Nmap\nmap.exe";
            yield return @"C:\Nmap\nmap.exe";
            yield break;
        }

        yield return "/usr/bin/nmap";
        yield return "/usr/local/bin/nmap";
    }

    private static string BuildProfileArguments(string scanProfile) => scanProfile switch
    {
        "Quick" => "-T4 -F",
        "QuickPlus" => "-T4 -sV -F",
        "Standard" => "-T4 -sV -O",
        "Deep" => "-T4 -sV -O -A --version-all",
        "Stealth" => "-T2 -sV -O",
        "VulnScript" => "-T4 -sV -sC --script vuln",
        _ => "-T4 -sV -O",
    };

    private static string EscapeArg(string arg)
    {
        if (string.IsNullOrWhiteSpace(arg))
        {
            return "\"\"";
        }

        if (!arg.Contains(' ') && !arg.Contains('\t') && !arg.Contains('"') && !arg.Contains('\\'))
        {
            return arg;
        }

        return "\"" + arg.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }
}
