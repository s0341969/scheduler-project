using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace VulnScan.Web.Hubs;

[Authorize]
public sealed class NotificationHub : Hub
{
    public async Task JoinScanGroup(int runId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"scan-{runId}");
    }

    public async Task LeaveScanGroup(int runId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"scan-{runId}");
    }
}
