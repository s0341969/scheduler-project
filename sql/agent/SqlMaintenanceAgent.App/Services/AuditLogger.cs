using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SqlMaintenanceAgent.App.Configuration;
using SqlMaintenanceAgent.App.Models;

namespace SqlMaintenanceAgent.App.Services;

public sealed class AuditLogger
{
    private readonly string _logDirectory;

    public AuditLogger(AuditOptions auditOptions)
    {
        _logDirectory = Path.GetFullPath(auditOptions.LogDirectory);
        Directory.CreateDirectory(_logDirectory);
    }

    public async Task WriteAsync(AuditEntry entry, CancellationToken cancellationToken)
    {
        var logFile = Path.Combine(_logDirectory, $"audit-{DateTimeOffset.Now:yyyyMMdd}.jsonl");
        var json = JsonSerializer.Serialize(entry);
        await File.AppendAllTextAsync(logFile, json + Environment.NewLine, Encoding.UTF8, cancellationToken);
    }

    public static string ComputeHash(string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text ?? string.Empty));
        return Convert.ToHexString(bytes);
    }
}
