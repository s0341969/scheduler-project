using Microsoft.AspNetCore.Mvc;
using RagApi.Models;
using RagApi.Services;

namespace RagApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class RagController(RagService ragService, ILogger<RagController> logger) : ControllerBase
{
    [HttpPost("retrieve")]
    [ProducesResponseType(typeof(RetrieveResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Retrieve([FromBody] RetrieveRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var result = await ragService.RetrieveAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "執行 retrieve 失敗");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "查詢失敗，請稍後再試。" });
        }
    }

    [HttpPost("index-folder")]
    [ProducesResponseType(typeof(IndexFolderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> IndexFolder([FromBody] IndexFolderRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var result = await ragService.IndexFolderAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (DirectoryNotFoundException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "執行 index-folder 失敗");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "建立索引失敗，請查看伺服器日誌。" });
        }
    }
}
