namespace VulnScan.Web.Services;

public interface IAuditLogService
{
    Task WriteAsync(string actionType, string targetType, int? targetId, string message, string? userAccount, string? sourceIp, CancellationToken cancellationToken = default);
}
