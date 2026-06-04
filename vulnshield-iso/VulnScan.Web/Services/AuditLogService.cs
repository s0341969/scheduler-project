using VulnScan.Web.Data;
using VulnScan.Web.Models;

namespace VulnScan.Web.Services;

public sealed class AuditLogService(ApplicationDbContext dbContext) : IAuditLogService
{
    public async Task WriteAsync(string actionType, string targetType, int? targetId, string message, string? userAccount, string? sourceIp, CancellationToken cancellationToken = default)
    {
        dbContext.AuditLogs.Add(new AuditLog
        {
            ActionType = actionType,
            TargetType = targetType,
            TargetId = targetId,
            LogMessage = message,
            UserAccount = userAccount,
            SourceIPAddress = sourceIp,
            CreatedAt = DateTime.UtcNow,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
