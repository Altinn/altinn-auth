using System;
using System.Collections.Generic;
using System.Linq;
using Altinn.Authorization.ServiceDefaults;
using Altinn.Platform.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Logging;

WebApplication app = AuthenticationHost.Create(args);

if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.UseDeveloperExceptionPage();

    // Enable higher level of detail in exceptions related to JWT validation
    IdentityModelEventSource.ShowPII = true;
}
else
{
    app.UseExceptionHandler("/authentication/api/v1/error");
}

app.Use(async (context, next) =>
{
    if (DebugForwardedHeaders(context.Request))
    {
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("DebugForwardedHeaders");
        Log.DebugForwardedHeaders(logger, "Pre", context.Request);
    }

    await next();
});

app.UseForwardedHeaders();

app.Use(async (context, next) =>
{
    if (DebugForwardedHeaders(context.Request))
    {
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("DebugForwardedHeaders");
        Log.DebugForwardedHeaders(logger, "Post", context.Request);
    }

    await next();
});

app.UseSwagger(o => o.RouteTemplate = "authentication/swagger/{documentName}/swagger.json");

app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/authentication/swagger/v1/swagger.json", "Altinn Platform Authentication API");
    c.RoutePrefix = "authentication/swagger";
});

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

await app.RunAsync();

static bool DebugForwardedHeaders(HttpRequest request)
{
    return request.Headers.TryGetValue("Altinn-Debug-Forwarded-For", out var values)
        && values.Any(static v => v == "true");
}

/// <summary>
/// Startup class.
/// </summary>
public partial class Program
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1118:Parameter should not span multiple lines", Justification = "Multiline log message")]
    private static partial class Log
    {
        [LoggerMessage(
            EventId = 1,
            Level = LogLevel.Warning,
            Message =
            """
            DebugForwardedHeaders - {Stage}: 
              Forwarded-For: {XForwardedFor}
              Forwarded-Host: {XForwardedHost}
              Client-IP: {ClientIp}
            """)]
        private static partial void DebugForwardedHeaders(ILogger logger, string stage, string? xForwardedFor, string? xForwardedHost, string clientIp);

        public static void DebugForwardedHeaders(ILogger logger, string stage, HttpRequest request)
        {
            DebugForwardedHeaders(
                logger,
                stage,
                string.Join(", ", request.Headers["X-Forwarded-For"].AsEnumerable()),
                request.Headers["X-Forwarded-Host"].FirstOrDefault(),
                request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
        }
    }
}
