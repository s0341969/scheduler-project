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
