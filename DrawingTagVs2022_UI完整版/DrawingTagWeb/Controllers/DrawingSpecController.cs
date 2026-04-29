using DrawingTagWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.IO;

namespace DrawingTagWeb.Controllers;

[ApiController]
[Route("api")]
public sealed class DrawingSpecController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public DrawingSpecController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// 依圖號、版次、項目取得檢驗規格資料。
    /// 對應 Stored Procedure：dbo.KNV10256_SIP量測表
    /// 參數：@INDWG、@DWGREV、@OPTION
    /// </summary>
    [HttpGet("drawing-spec")]
    public async Task<ActionResult<DrawingSpecLookupResponse>> GetDrawingSpec(
        [FromQuery] string indwg,
        [FromQuery] string dwgrev,
        [FromQuery] string option = "首件")
    {
        if (string.IsNullOrWhiteSpace(indwg))
        {
            return BadRequest("indwg 不可空白。");
        }

        if (string.IsNullOrWhiteSpace(dwgrev))
        {
            return BadRequest("dwgrev 不可空白。");
        }

        if (string.IsNullOrWhiteSpace(option))
        {
            option = "首件";
        }

        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        var storedProcedureName = _configuration["StoredProcedures:GetDrawingSpec"] ?? "dbo.KNV10256_SIP量測表";

        var response = new DrawingSpecLookupResponse();

        await using var conn = new SqlConnection(connectionString);
        await using var cmd = new SqlCommand(storedProcedureName, conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.Add(new SqlParameter("@INDWG", SqlDbType.VarChar, 30) { Value = indwg.Trim() });
        cmd.Parameters.Add(new SqlParameter("@DWGREV", SqlDbType.VarChar, 10) { Value = dwgrev.Trim() });
        cmd.Parameters.Add(new SqlParameter("@OPTION", SqlDbType.VarChar, 10) { Value = option.Trim() });

        await conn.OpenAsync();
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }

            response.SpecRows.Add(row);
        }

        if (await reader.NextResultAsync())
        {
            var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            while (await reader.ReadAsync())
            {
                var filePath = ResolvePdfPath(reader);
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    continue;
                }

                if (!seenPaths.Add(filePath))
                {
                    continue;
                }

                response.PdfOptions.Add(new PdfOptionDto
                {
                    FilePath = filePath,
                    DisplayName = BuildPdfDisplayName(filePath)
                });
            }
        }

        return Ok(response);
    }

    [HttpGet("drawing-spec/pdf-file")]
    public IActionResult GetPdfFile([FromQuery] string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return BadRequest("path 不可空白。");
        }

        var normalizedPath = path.Trim().Trim('"');
        if (!string.Equals(Path.GetExtension(normalizedPath), ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("僅支援載入 PDF 檔案。");
        }

        if (!System.IO.File.Exists(normalizedPath))
        {
            return NotFound($"找不到 PDF 檔案：{normalizedPath}");
        }

        try
        {
            var stream = new FileStream(
                normalizedPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite,
                bufferSize: 64 * 1024,
                useAsync: true);

            return File(stream, "application/pdf", Path.GetFileName(normalizedPath), enableRangeProcessing: true);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, $"無法讀取 PDF 檔案：{ex.Message}");
        }
        catch (IOException ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"讀取 PDF 檔案失敗：{ex.Message}");
        }
    }

    private static string? ResolvePdfPath(SqlDataReader reader)
    {
        var fullPath = GetFieldValue(reader, "PdfPath", "PDFPath", "FilePath", "FullPath", "Path", "圖檔完整路徑", "圖檔路徑", "PDF路徑");
        if (!string.IsNullOrWhiteSpace(fullPath))
        {
            return NormalizePath(fullPath);
        }

        var directory = GetFieldValue(reader, "DirectoryPath", "FolderPath", "BasePath", "RootPath", "圖檔資料夾", "圖檔路徑", "路徑");
        var fileName = GetFieldValue(reader, "PdfFileName", "FileName", "檔名", "圖檔檔名", "PDF檔名");

        if (!string.IsNullOrWhiteSpace(directory) && !string.IsNullOrWhiteSpace(fileName))
        {
            return NormalizePath(Path.Combine(directory.Trim(), fileName.Trim()));
        }

        foreach (var fallback in EnumerateAllStringValues(reader))
        {
            var normalized = NormalizePath(fallback);
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                return normalized;
            }
        }

        return null;
    }

    private static string? GetFieldValue(SqlDataReader reader, params string[] fieldNames)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            var actualName = reader.GetName(i);
            if (!fieldNames.Any(name => string.Equals(name, actualName, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            if (reader.IsDBNull(i))
            {
                return null;
            }

            var value = Convert.ToString(reader.GetValue(i));
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static IEnumerable<string> EnumerateAllStringValues(SqlDataReader reader)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (reader.IsDBNull(i))
            {
                continue;
            }

            var value = Convert.ToString(reader.GetValue(i));
            if (!string.IsNullOrWhiteSpace(value))
            {
                yield return value;
            }
        }
    }

    private static string? NormalizePath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim().Trim('"');
    }

    private static string BuildPdfDisplayName(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        return string.IsNullOrWhiteSpace(fileName) ? filePath : fileName;
    }
}
