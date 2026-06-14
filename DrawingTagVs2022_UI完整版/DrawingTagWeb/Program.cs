using System.Runtime.InteropServices;

EnsureNativeLibrarySearchPaths();

var builder = WebApplication.CreateBuilder(args);

var configuredUrl = builder.Configuration["Hosting:Url"];
var explicitUrl = builder.Configuration["urls"]
    ?? builder.Configuration["URLS"]
    ?? builder.Configuration["ASPNETCORE_URLS"];

if (string.IsNullOrWhiteSpace(explicitUrl) && !string.IsNullOrWhiteSpace(configuredUrl))
{
    builder.WebHost.UseUrls(configuredUrl);
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

app.Run();

static void EnsureNativeLibrarySearchPaths()
{
    var baseDir = AppContext.BaseDirectory;
    var rid = RuntimeInformation.ProcessArchitecture == Architecture.X64 ? "win-x64" : "win-x86";
    var nativeDir = Path.Combine(baseDir, "runtimes", rid, "native");
    var archDir = Path.Combine(baseDir, RuntimeInformation.ProcessArchitecture == Architecture.X64 ? "x64" : "x86");
    var existingPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
    var prependPaths = new List<string>();

    if (Directory.Exists(nativeDir))
    {
        prependPaths.Add(nativeDir);
    }

    if (Directory.Exists(archDir))
    {
        prependPaths.Add(archDir);
    }

    if (prependPaths.Count == 0)
    {
        return;
    }

    var newPath = string.Join(";", prependPaths.Where(path =>
        !existingPath.Contains(path, StringComparison.OrdinalIgnoreCase)));

    if (string.IsNullOrWhiteSpace(newPath))
    {
        return;
    }

    Environment.SetEnvironmentVariable("PATH", $"{newPath};{existingPath}");
}
