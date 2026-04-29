using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace DrawingTagWeb.Controllers;

[ApiController]
[Route("api/system-info")]
public sealed class SystemInfoController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;
        var assemblyVersion = assembly.GetName().Version?.ToString();

        return Ok(new
        {
            systemName = "DrawingTagVs2022 UI 完整版",
            version = informationalVersion ?? assemblyVersion ?? "UNKNOWN",
            assemblyVersion = assemblyVersion ?? "UNKNOWN"
        });
    }
}
