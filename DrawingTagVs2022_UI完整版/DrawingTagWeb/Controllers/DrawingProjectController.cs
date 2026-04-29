using DrawingTagWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;

namespace DrawingTagWeb.Controllers;

[ApiController]
[Route("api/drawing-project")]
public sealed class DrawingProjectController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public DrawingProjectController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost("save")]
    public async Task<IActionResult> Save([FromBody] SaveDrawingProjectRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DrawingNumber))
            return BadRequest("DrawingNumber 不可空白。");

        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        var image = request.ImageSrc ?? request.Image;
        var specJson = request.SpecDataMap is null ? null : JsonSerializer.Serialize(request.SpecDataMap);
        var currentSeq = request.CurrentSeq ?? "1";
        var currentZoom = request.CurrentZoom ?? 1;

        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();

        try
        {
            var versionNo = await GetNextVersionAsync(conn, tx, request.DrawingNumber);
            var projectId = await InsertProjectAsync(conn, tx, request.DrawingNumber, versionNo, image, specJson, currentSeq, currentZoom);

            foreach (var tag in request.Tags)
            {
                var itemNo = TryParseInt(tag.Id);
                await InsertTagAsync(conn, tx, projectId, itemNo, tag.Id, tag.Method, tag.X, tag.Y);
            }

            await tx.CommitAsync();
            return Ok(new { projectId, version = versionNo, versionNo });
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    [HttpGet("latest/{drawingNumber}")]
    public async Task<IActionResult> GetLatest(string drawingNumber)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");

        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        await using var projectCmd = new SqlCommand(@"
SELECT TOP 1 ProjectId, DrawingNumber, VersionNo, ImageBase64, SpecDataJson, CurrentSeq, CurrentZoom
FROM dbo.DrawingProject
WHERE DrawingNumber = @DrawingNumber
ORDER BY VersionNo DESC, ProjectId DESC;", conn);

        projectCmd.Parameters.Add(new SqlParameter("@DrawingNumber", SqlDbType.NVarChar, 100) { Value = drawingNumber });

        await using var reader = await projectCmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return NotFound();

        var projectId = reader.GetInt32(0);
        var response = new DrawingProjectResponse
        {
            ProjectId = projectId,
            DrawingNumber = reader.GetString(1),
            Version = reader.GetInt32(2),
            VersionNo = reader.GetInt32(2),
            ImageSrc = reader.IsDBNull(3) ? null : reader.GetString(3),
            Image = reader.IsDBNull(3) ? null : reader.GetString(3),
            SpecDataMap = reader.IsDBNull(4) ? null : JsonSerializer.Deserialize<Dictionary<string, string>>(reader.GetString(4)),
            CurrentSeq = reader.IsDBNull(5) ? "1" : reader.GetString(5),
            CurrentZoom = reader.IsDBNull(6) ? 1 : reader.GetDecimal(6)
        };
        await reader.CloseAsync();

        await using var tagCmd = new SqlCommand(@"
SELECT ItemNo, ItemText, InspectionMethod, X, Y
FROM dbo.DrawingTag
WHERE ProjectId = @ProjectId
ORDER BY TagId;", conn);
        tagCmd.Parameters.Add(new SqlParameter("@ProjectId", SqlDbType.Int) { Value = projectId });

        await using var tagReader = await tagCmd.ExecuteReaderAsync();
        while (await tagReader.ReadAsync())
        {
            var itemNo = tagReader.GetInt32(0);
            var itemText = tagReader.IsDBNull(1) ? itemNo.ToString() : tagReader.GetString(1);
            var method = tagReader.IsDBNull(2) ? "UNASSIGNED" : tagReader.GetString(2);
            response.Tags.Add(new DrawingTagResponse
            {
                Id = itemText,
                ItemNo = itemNo,
                Method = method,
                InspectionMethod = method,
                X = tagReader.GetDouble(3),
                Y = tagReader.GetDouble(4)
            });
        }

        return Ok(response);
    }

    private static async Task<int> GetNextVersionAsync(SqlConnection conn, IDbTransaction tx, string drawingNumber)
    {
        await using var cmd = new SqlCommand(@"
SELECT ISNULL(MAX(VersionNo), 0) + 1
FROM dbo.DrawingProject WITH (UPDLOCK, HOLDLOCK)
WHERE DrawingNumber = @DrawingNumber;", conn, (SqlTransaction)tx);
        cmd.Parameters.Add(new SqlParameter("@DrawingNumber", SqlDbType.NVarChar, 100) { Value = drawingNumber });
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    private static async Task<int> InsertProjectAsync(SqlConnection conn, IDbTransaction tx, string drawingNumber, int versionNo, string? image, string? specJson, string currentSeq, decimal currentZoom)
    {
        await using var cmd = new SqlCommand(@"
INSERT INTO dbo.DrawingProject(DrawingNumber, VersionNo, ImageBase64, SpecDataJson, CurrentSeq, CurrentZoom, CreatedBy)
VALUES(@DrawingNumber, @VersionNo, @ImageBase64, @SpecDataJson, @CurrentSeq, @CurrentZoom, @CreatedBy);
SELECT CONVERT(INT, SCOPE_IDENTITY());", conn, (SqlTransaction)tx);

        cmd.Parameters.Add(new SqlParameter("@DrawingNumber", SqlDbType.NVarChar, 100) { Value = drawingNumber });
        cmd.Parameters.Add(new SqlParameter("@VersionNo", SqlDbType.Int) { Value = versionNo });
        cmd.Parameters.Add(new SqlParameter("@ImageBase64", SqlDbType.NVarChar, -1) { Value = (object?)image ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@SpecDataJson", SqlDbType.NVarChar, -1) { Value = (object?)specJson ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@CurrentSeq", SqlDbType.NVarChar, 20) { Value = currentSeq });
        cmd.Parameters.Add(new SqlParameter("@CurrentZoom", SqlDbType.Decimal) { Precision = 10, Scale = 4, Value = currentZoom });
        cmd.Parameters.Add(new SqlParameter("@CreatedBy", SqlDbType.NVarChar, 50) { Value = Environment.UserName });

        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    private static async Task InsertTagAsync(SqlConnection conn, IDbTransaction tx, int projectId, int itemNo, string? itemText, string? method, double x, double y)
    {
        await using var cmd = new SqlCommand(@"
INSERT INTO dbo.DrawingTag(ProjectId, ItemNo, ItemText, InspectionMethod, X, Y)
VALUES(@ProjectId, @ItemNo, @ItemText, @InspectionMethod, @X, @Y);", conn, (SqlTransaction)tx);

        cmd.Parameters.Add(new SqlParameter("@ProjectId", SqlDbType.Int) { Value = projectId });
        cmd.Parameters.Add(new SqlParameter("@ItemNo", SqlDbType.Int) { Value = itemNo });
        cmd.Parameters.Add(new SqlParameter("@ItemText", SqlDbType.NVarChar, 50) { Value = (object?)itemText ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@InspectionMethod", SqlDbType.NVarChar, 50) { Value = (object?)method ?? DBNull.Value });
        cmd.Parameters.Add(new SqlParameter("@X", SqlDbType.Float) { Value = x });
        cmd.Parameters.Add(new SqlParameter("@Y", SqlDbType.Float) { Value = y });

        await cmd.ExecuteNonQueryAsync();
    }

    private static int TryParseInt(string? value)
    {
        return int.TryParse(value, out var n) ? n : 0;
    }
}
