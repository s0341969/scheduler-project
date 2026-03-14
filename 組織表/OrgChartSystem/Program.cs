using Microsoft.EntityFrameworkCore;
using OrgChartSystem.Contracts;
using OrgChartSystem.Data;
using OrgChartSystem.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=orgchart.db";

    options.UseSqlite(connectionString);
});

builder.Services.AddScoped<OrgChartService>();
builder.Services.AddHostedService<BackupHostedService>();

var app = builder.Build();

await DbInitializer.InitializeAsync(app.Services);

app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();

var api = app.MapGroup("/api/orgchart");

api.MapGet(string.Empty, async (HttpContext http, OrgChartService service, IConfiguration config) =>
{
    var auth = RequireRole(http, config, requireEdit: false);
    if (auth.Failed)
    {
        return auth.Result!;
    }

    var data = await service.GetChartAsync();
    return Results.Ok(data);
});

api.MapGet("/search", async (HttpContext http, string q, int? limit, OrgChartService service, IConfiguration config) =>
{
    var auth = RequireRole(http, config, requireEdit: false);
    if (auth.Failed)
    {
        return auth.Result!;
    }

    var data = await service.SearchAsync(q, limit ?? 50);
    return Results.Ok(data);
});

api.MapGet("/export", async (HttpContext http, OrgChartService service, IConfiguration config) =>
{
    var auth = RequireRole(http, config, requireEdit: false);
    if (auth.Failed)
    {
        return auth.Result!;
    }

    var data = await service.GetChartAsync();
    return Results.Ok(data);
});

api.MapPost("/nodes", async (HttpContext http, NodeMutationRequest request, OrgChartService service, IConfiguration config) =>
{
    var auth = RequireRole(http, config, requireEdit: true);
    if (auth.Failed)
    {
        return auth.Result!;
    }

    try
    {
        var node = await service.CreateNodeAsync(request, auth.Actor, auth.Role);
        return Results.Created($"/api/orgchart/nodes/{node.Id}", node);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

api.MapPut("/nodes/{id:int}", async (HttpContext http, int id, NodeMutationRequest request, OrgChartService service, IConfiguration config) =>
{
    var auth = RequireRole(http, config, requireEdit: true);
    if (auth.Failed)
    {
        return auth.Result!;
    }

    try
    {
        var node = await service.UpdateNodeAsync(id, request, auth.Actor, auth.Role);
        return node is null
            ? Results.NotFound(new { message = "節點不存在。" })
            : Results.Ok(node);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

api.MapDelete("/nodes/{id:int}", async (HttpContext http, int id, OrgChartService service, IConfiguration config) =>
{
    var auth = RequireRole(http, config, requireEdit: true);
    if (auth.Failed)
    {
        return auth.Result!;
    }

    var deleted = await service.DeleteNodeAsync(id, auth.Actor, auth.Role);
    return deleted
        ? Results.NoContent()
        : Results.NotFound(new { message = "節點不存在。" });
});

api.MapPost("/nodes/{id:int}/move", async (HttpContext http, int id, MoveNodeRequest request, OrgChartService service, IConfiguration config) =>
{
    var auth = RequireRole(http, config, requireEdit: true);
    if (auth.Failed)
    {
        return auth.Result!;
    }

    try
    {
        var moved = await service.MoveNodeAsync(id, request.Direction, auth.Actor, auth.Role);
        return moved
            ? Results.Ok(new { message = "節點已更新排序。" })
            : Results.NotFound(new { message = "節點不存在。" });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

api.MapPost("/nodes/{id:int}/reposition", async (HttpContext http, int id, RepositionNodeRequest request, OrgChartService service, IConfiguration config) =>
{
    var auth = RequireRole(http, config, requireEdit: true);
    if (auth.Failed)
    {
        return auth.Result!;
    }

    try
    {
        var moved = await service.RepositionNodeAsync(id, request.ParentId, request.Index, auth.Actor, auth.Role);
        return moved
            ? Results.Ok(new { message = "節點已更新位置。" })
            : Results.NotFound(new { message = "節點不存在。" });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

api.MapPut("/settings", async (HttpContext http, UpdateSettingRequest request, OrgChartService service, IConfiguration config) =>
{
    var auth = RequireRole(http, config, requireEdit: true);
    if (auth.Failed)
    {
        return auth.Result!;
    }

    try
    {
        var mode = await service.UpdateDisplayModeAsync(request.DisplayMode, auth.Actor, auth.Role);
        return Results.Ok(new { displayMode = mode });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

api.MapPost("/import/preview", async (HttpContext http, ImportOrgChartRequest request, OrgChartService service, IConfiguration config) =>
{
    var auth = RequireRole(http, config, requireEdit: true);
    if (auth.Failed)
    {
        return auth.Result!;
    }

    try
    {
        var preview = await service.PreviewImportAsync(request);
        return Results.Ok(preview);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

api.MapPost("/import", async (HttpContext http, ImportOrgChartRequest request, OrgChartService service, IConfiguration config) =>
{
    var auth = RequireRole(http, config, requireEdit: true);
    if (auth.Failed)
    {
        return auth.Result!;
    }

    try
    {
        await service.ImportAsync(request, auth.Actor, auth.Role);
        return Results.Ok(new { message = "匯入完成。" });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

api.MapGet("/snapshots", async (HttpContext http, int? take, OrgChartService service, IConfiguration config) =>
{
    var auth = RequireRole(http, config, requireEdit: false);
    if (auth.Failed)
    {
        return auth.Result!;
    }

    var snapshots = await service.ListSnapshotsAsync(take ?? 50);
    return Results.Ok(snapshots);
});

api.MapPost("/snapshots", async (HttpContext http, CreateSnapshotRequest request, OrgChartService service, IConfiguration config) =>
{
    var auth = RequireRole(http, config, requireEdit: true);
    if (auth.Failed)
    {
        return auth.Result!;
    }

    var snapshot = await service.CreateSnapshotAsync(request.Reason, auth.Actor);
    return Results.Ok(snapshot);
});

api.MapPost("/snapshots/{id:int}/restore", async (HttpContext http, int id, OrgChartService service, IConfiguration config) =>
{
    var auth = RequireRole(http, config, requireEdit: true);
    if (auth.Failed)
    {
        return auth.Result!;
    }

    try
    {
        var restored = await service.RestoreSnapshotAsync(id, auth.Actor, auth.Role);
        return restored
            ? Results.Ok(new { message = "快照回復完成。" })
            : Results.NotFound(new { message = "快照不存在。" });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

api.MapGet("/backups", (HttpContext http, OrgChartService service, IConfiguration config) =>
{
    var auth = RequireRole(http, config, requireEdit: true);
    if (auth.Failed)
    {
        return auth.Result!;
    }

    var backups = service.ListDatabaseBackups();
    return Results.Ok(backups);
});

api.MapPost("/backups", async (HttpContext http, OrgChartService service, IConfiguration config) =>
{
    var auth = RequireRole(http, config, requireEdit: true);
    if (auth.Failed)
    {
        return auth.Result!;
    }

    var backup = await service.CreateDatabaseBackupAsync("手動備份", auth.Actor, auth.Role);
    return Results.Ok(backup);
});

api.MapPost("/backups/restore", async (HttpContext http, RestoreBackupRequest request, OrgChartService service, IConfiguration config) =>
{
    var auth = RequireRole(http, config, requireEdit: true);
    if (auth.Failed)
    {
        return auth.Result!;
    }

    try
    {
        await service.RestoreDatabaseBackupAsync(request.FileName, auth.Actor, auth.Role);
        return Results.Ok(new { message = "備份還原完成，請重新整理資料。" });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

api.MapGet("/audits", async (HttpContext http, int? take, OrgChartService service, IConfiguration config) =>
{
    var auth = RequireRole(http, config, requireEdit: true);
    if (auth.Failed)
    {
        return auth.Result!;
    }

    var logs = await service.GetAuditLogsAsync(take ?? 100);
    return Results.Ok(logs);
});

app.MapFallbackToFile("index.html");

app.Run();

static AuthResult RequireRole(HttpContext context, IConfiguration config, bool requireEdit)
{
    var editKey = config["Auth:EditKey"]?.Trim() ?? string.Empty;
    var readKey = config["Auth:ReadKey"]?.Trim() ?? string.Empty;

    var role = context.Request.Headers["X-OrgChart-Role"].ToString().Trim().ToLowerInvariant();
    var key = context.Request.Headers["X-OrgChart-Key"].ToString().Trim();
    var actor = context.Request.Headers["X-OrgChart-Actor"].ToString().Trim();

    if (string.IsNullOrWhiteSpace(actor))
    {
        actor = "anonymous";
    }

    var authDisabled = string.IsNullOrWhiteSpace(editKey) && string.IsNullOrWhiteSpace(readKey);
    if (authDisabled)
    {
        return new AuthResult(false, Results.Empty, actor, "editor");
    }

    if (string.IsNullOrWhiteSpace(role))
    {
        role = requireEdit ? "editor" : "viewer";
    }

    var isEditor = role == "editor" && key == editKey && !string.IsNullOrWhiteSpace(editKey);
    var isViewer = role == "viewer" && key == readKey && !string.IsNullOrWhiteSpace(readKey);

    if (requireEdit)
    {
        if (!isEditor)
        {
            return new AuthResult(true, Results.Unauthorized(), actor, role);
        }

        return new AuthResult(false, Results.Empty, actor, "editor");
    }

    if (!isEditor && !isViewer)
    {
        return new AuthResult(true, Results.Unauthorized(), actor, role);
    }

    return new AuthResult(false, Results.Empty, actor, isEditor ? "editor" : "viewer");
}

record AuthResult(bool Failed, IResult Result, string Actor, string Role);
