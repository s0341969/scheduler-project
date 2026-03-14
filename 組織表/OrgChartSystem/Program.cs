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

var app = builder.Build();

await DbInitializer.InitializeAsync(app.Services);

app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();

var api = app.MapGroup("/api/orgchart");

api.MapGet(string.Empty, async (OrgChartService service) =>
{
    var data = await service.GetChartAsync();
    return Results.Ok(data);
});

api.MapGet("/export", async (OrgChartService service) =>
{
    var data = await service.GetChartAsync();
    return Results.Ok(data);
});

api.MapPost("/nodes", async (NodeMutationRequest request, OrgChartService service) =>
{
    try
    {
        var node = await service.CreateNodeAsync(request);
        return Results.Created($"/api/orgchart/nodes/{node.Id}", node);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

api.MapPut("/nodes/{id:int}", async (int id, NodeMutationRequest request, OrgChartService service) =>
{
    try
    {
        var node = await service.UpdateNodeAsync(id, request);
        return node is null
            ? Results.NotFound(new { message = "節點不存在。" })
            : Results.Ok(node);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

api.MapDelete("/nodes/{id:int}", async (int id, OrgChartService service) =>
{
    var deleted = await service.DeleteNodeAsync(id);
    return deleted
        ? Results.NoContent()
        : Results.NotFound(new { message = "節點不存在。" });
});

api.MapPost("/nodes/{id:int}/move", async (int id, MoveNodeRequest request, OrgChartService service) =>
{
    try
    {
        var moved = await service.MoveNodeAsync(id, request.Direction);
        return moved
            ? Results.Ok(new { message = "節點已更新排序。" })
            : Results.NotFound(new { message = "節點不存在。" });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

api.MapPut("/settings", async (UpdateSettingRequest request, OrgChartService service) =>
{
    try
    {
        var mode = await service.UpdateDisplayModeAsync(request.DisplayMode);
        return Results.Ok(new { displayMode = mode });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

api.MapPost("/import", async (ImportOrgChartRequest request, OrgChartService service) =>
{
    try
    {
        await service.ImportAsync(request);
        return Results.Ok(new { message = "匯入完成。" });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

app.MapFallbackToFile("index.html");

app.Run();
