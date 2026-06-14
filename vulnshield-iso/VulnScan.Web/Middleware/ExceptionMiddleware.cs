using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;

namespace VulnScan.Web.Middleware;

public sealed class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled exception occurred. Path: {Path}, Method: {Method}",
                context.Request.Path, context.Request.Method);

            if (IsApiRequest(context))
            {
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                var result = JsonSerializer.Serialize(new
                {
                    error = "伺服器內部錯誤，請稍後再試。",
                    detail = context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment()
                        ? exception.Message
                        : null
                });
                await context.Response.WriteAsync(result);
            }
            else
            {
                context.Response.Redirect("/Home/Error");
            }
        }
    }

    private static bool IsApiRequest(HttpContext context)
    {
        return context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase);
    }
}
