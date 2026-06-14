namespace VulnScan.Web.Models;

public sealed class ScanProfileDefinition
{
    public required string Key { get; init; }
    public required string DisplayName { get; init; }
    public required string Tool { get; init; }
    public string? ScanType { get; init; }
    public string? CliFlag { get; init; }
    public string? CliValue { get; init; }

    public static readonly List<ScanProfileDefinition> All =
    [
        new() { Key = "Quick", DisplayName = "Quick - 快速連接埠掃描", Tool = "Nmap", CliFlag = null, CliValue = "-T4 -F" },
        new() { Key = "QuickPlus", DisplayName = "QuickPlus - 快速 + 版本偵測", Tool = "Nmap", CliFlag = null, CliValue = "-T4 -sV -F" },
        new() { Key = "Standard", DisplayName = "Standard - 標準掃描", Tool = "Nmap", CliFlag = null, CliValue = "-T4 -sV -O" },
        new() { Key = "Deep", DisplayName = "Deep - 深度掃描", Tool = "Nmap", CliFlag = null, CliValue = "-T4 -sV -O -A --version-all" },
        new() { Key = "Stealth", DisplayName = "Stealth - 隱匿模式", Tool = "Nmap", CliFlag = null, CliValue = "-T2 -sV -O" },
        new() { Key = "VulnScript", DisplayName = "VulnScript - 安全腳本掃描", Tool = "Nmap", CliFlag = null, CliValue = "-T4 -sV -sC --script vuln" },
        new() { Key = "CredentialCheck", DisplayName = "CredentialCheck - 預設帳密檢測", Tool = "Nmap", CliFlag = null, CliValue = "-T4 -sV --script http-default-accounts,ftp-brute,ssh-brute,smb-brute" },

        new() { Key = "All", DisplayName = "All - 所有 Nuclei 模板", Tool = "Nuclei" },
        new() { Key = "cves", DisplayName = "CVEs - 已知 CVE", Tool = "Nuclei", ScanType = "VulnerabilityScan" },
        new() { Key = "vulnerabilities", DisplayName = "Vulnerabilities - 一般弱點", Tool = "Nuclei", ScanType = "VulnerabilityScan" },
        new() { Key = "misconfiguration", DisplayName = "Misconfiguration - 錯誤設定", Tool = "Nuclei", ScanType = "BaselineScan" },
        new() { Key = "exposures", DisplayName = "Exposures - 曝露", Tool = "Nuclei", ScanType = "VulnerabilityScan" },
        new() { Key = "default-logins", DisplayName = "Default Logins - 預設帳密檢測", Tool = "Nuclei", ScanType = "CredentialCheck" },

        new() { Key = "web-sqli", DisplayName = "Web SQL Injection", Tool = "Nuclei", ScanType = "WebScan", CliFlag = "-tags", CliValue = "sqli" },
        new() { Key = "web-xss", DisplayName = "Web XSS", Tool = "Nuclei", ScanType = "WebScan", CliFlag = "-tags", CliValue = "xss" },
        new() { Key = "web-lfi", DisplayName = "Web LFI/RFI", Tool = "Nuclei", ScanType = "WebScan", CliFlag = "-tags", CliValue = "lfi,rfi" },
        new() { Key = "web-ssrf", DisplayName = "Web SSRF", Tool = "Nuclei", ScanType = "WebScan", CliFlag = "-tags", CliValue = "ssrf" },
        new() { Key = "web-rce", DisplayName = "Web RCE", Tool = "Nuclei", ScanType = "WebScan", CliFlag = "-tags", CliValue = "rce" },
        new() { Key = "web-tech", DisplayName = "Web Tech Detection", Tool = "Nuclei", ScanType = "WebScan", CliFlag = "-tags", CliValue = "tech" },

        new() { Key = "DependencyScan", DisplayName = "DependencyScan - 相依性弱點掃描", Tool = "DependencyScanner", ScanType = "DependencyScan" },
    ];

    public static List<ScanProfileDefinition> ForTool(string tool) =>
        All.Where(p => string.Equals(p.Tool, tool, StringComparison.OrdinalIgnoreCase)).ToList();

    public static ScanProfileDefinition? Get(string key) =>
        All.FirstOrDefault(p => string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase));
}
