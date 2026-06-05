using System.Diagnostics;
using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using VulnScan.Web.Models;

namespace VulnScan.Web.Services;

public sealed partial class NmapInstallerService(
    HttpClient httpClient,
    IWebHostEnvironment environment,
    IOptions<VulnScanOptions> options,
    INmapService nmapService,
    IAuditLogService auditLogService) : INmapInstallerService
{
    private readonly VulnScanOptions _options = options.Value;

    public async Task<NmapInstallerResult> StartInstallAsync(string triggeredBy, CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new InvalidOperationException("自動安裝流程目前只支援 Windows 主機。");
        }

        var currentStatus = nmapService.GetInstallationStatus();
        if (currentStatus.IsInstalled)
        {
            return new NmapInstallerResult
            {
                Started = false,
                InstallerPath = currentStatus.ResolvedPath,
                Message = $"Nmap 已存在，路徑：{currentStatus.ResolvedPath}",
            };
        }

        var downloadUrl = await ResolveInstallerUrlAsync(cancellationToken);
        var installerDirectory = ResolveInstallerDirectory();
        Directory.CreateDirectory(installerDirectory);

        var fileName = Path.GetFileName(new Uri(downloadUrl).LocalPath);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new InvalidOperationException("無法判定 Nmap 安裝檔檔名。");
        }

        var installerPath = Path.Combine(installerDirectory, fileName);
        await DownloadInstallerAsync(downloadUrl, installerPath, cancellationToken);
        StartInstallerProcess(installerPath);

        await auditLogService.WriteAsync(
            "NmapInstallerStarted",
            "SystemCheck",
            null,
            $"已啟動 Nmap 安裝程式：{installerPath}",
            triggeredBy,
            null,
            cancellationToken);

        return new NmapInstallerResult
        {
            Started = true,
            InstallerPath = installerPath,
            DownloadUrl = downloadUrl,
            Message = "已下載並啟動官方 Nmap Windows 安裝程式。若出現 UAC 或安裝精靈，請依畫面完成安裝。",
        };
    }

    private async Task<string> ResolveInstallerUrlAsync(CancellationToken cancellationToken)
    {
        var downloadPageUrl = _options.NmapDownloadPageUrl?.Trim();
        var installerBaseUrl = _options.NmapInstallerBaseUrl?.Trim();

        if (string.IsNullOrWhiteSpace(downloadPageUrl) || string.IsNullOrWhiteSpace(installerBaseUrl))
        {
            throw new InvalidOperationException("VulnScan Nmap 安裝設定不完整，請確認 NmapDownloadPageUrl 與 NmapInstallerBaseUrl。");
        }

        var html = await httpClient.GetStringAsync(downloadPageUrl, cancellationToken);
        var match = NmapInstallerRegex().Match(html);
        if (!match.Success)
        {
            throw new InvalidOperationException("無法從 Nmap 官方下載頁解析最新 Windows 安裝檔名稱，請稍後再試或改以手動安裝。");
        }

        var installerName = match.Groups["file"].Value;
        return new Uri(new Uri(installerBaseUrl, UriKind.Absolute), installerName).ToString();
    }

    private string ResolveInstallerDirectory()
    {
        var configured = _options.InstallerCachePath?.Trim();
        if (string.IsNullOrWhiteSpace(configured))
        {
            return Path.Combine(environment.ContentRootPath, "App_Data", "Installers");
        }

        return Path.IsPathRooted(configured)
            ? configured
            : Path.GetFullPath(Path.Combine(environment.ContentRootPath, configured));
    }

    private async Task DownloadInstallerAsync(string downloadUrl, string installerPath, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var fileStream = new FileStream(installerPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await responseStream.CopyToAsync(fileStream, cancellationToken);
    }

    private static void StartInstallerProcess(string installerPath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = installerPath,
            UseShellExecute = true,
            WorkingDirectory = Path.GetDirectoryName(installerPath) ?? AppContext.BaseDirectory,
        };

        var process = Process.Start(startInfo);
        if (process is null)
        {
            throw new InvalidOperationException("無法啟動 Nmap 安裝程式。");
        }
    }

    [GeneratedRegex("Latest stable release self-installer:\\s*(?:<[^>]+>\\s*)*(?<file>nmap-[0-9.]+-setup\\.exe)", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex NmapInstallerRegex();
}
