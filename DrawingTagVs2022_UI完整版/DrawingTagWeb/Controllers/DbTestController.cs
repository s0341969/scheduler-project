using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace DrawingTagWeb.Controllers;

[ApiController]
[Route("api")]
public sealed class DbTestController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public DbTestController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet("test-db")]
    public async Task<IActionResult> TestDb()
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            await using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();
            return Ok(new { success = true, message = "DB OK", database = conn.Database, server = conn.DataSource });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}
