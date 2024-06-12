using System.Net;
using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Altinn.ResourceRegistry;

/// <summary>
/// Middleware for logging requests before and after applying X-Forwarded headers.
/// </summary>
internal partial class RequestForwarderLogMiddleware 
{
    private readonly static PathString HealthPath = new("/health");
    private readonly static PathString SwaggerPath = new("/swagger");

    private readonly string _name;
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestForwarderLogMiddleware> _logger;
    private readonly IOptionsMonitor<ForwardedHeadersOptions> _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestForwarderLogMiddleware"/> class.
    /// </summary>
    public RequestForwarderLogMiddleware(
        RequestDelegate next,
        IOptionsMonitor<ForwardedHeadersOptions> options,
        ILogger<RequestForwarderLogMiddleware> logger, 
        string name)
    {
        Guard.IsNotNull(next);
        Guard.IsNotNull(options);
        Guard.IsNotNull(logger);
        Guard.IsNotNullOrEmpty(name);

        _next = next;
        _options = options;
        _logger = logger;
        _name = name;
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The http context.</param>
    public Task Invoke(HttpContext context)
    {
        LogProperties(context);
        return _next(context);
    }

    private void LogProperties(HttpContext context)
    {
        var path = context.Request.Path;
        if (path.StartsWithSegments(HealthPath, StringComparison.OrdinalIgnoreCase)
            || path.StartsWithSegments(SwaggerPath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var options = _options.CurrentValue;
        var forwardedFor = context.Request.Headers[options.ForwardedForHeaderName];
        var forwardedHost = context.Request.Headers[options.ForwardedHostHeaderName];
        var forwardedProto = context.Request.Headers[options.ForwardedProtoHeaderName];
        var forwardedPrefix = context.Request.Headers[options.ForwardedPrefixHeaderName];
        var knownNetworks = options.KnownNetworks.Select(n => $"{n.Prefix}/{n.PrefixLength}");
        var knownProxies = options.KnownProxies;

        Log.RequestReceived(
            _logger,
            _name,
            context.Request.Scheme,
            context.Request.Host,
            context.Request.PathBase,
            context.Request.Path,
            knownNetworks,
            knownProxies,
            forwardedFor,
            forwardedHost,
            forwardedProto,
            forwardedPrefix);
    }

    private partial class Log
    {
        [LoggerMessage(
            0,
            LogLevel.Information,
            """
            {name} request info: {proto}://{host} {pathBase} {path}
            Known networks: {knownNetworks}
            Known proxies: {knownProxies}
            Forwarded-For: {forwardedForHeader}
            Forwarded-Host: {forwardedHostHeader}
            Forwarded-Proto: {forwardedProtoHeader}
            Forwarded-Prefix: {forwardedPrefixHeader}
            """)]
        public static partial void RequestReceived(
            ILogger logger,
            string name,
            string proto,
            HostString host,
            string pathBase,
            string path,
            IEnumerable<string> knownNetworks,
            IEnumerable<IPAddress> knownProxies,
            StringValues forwardedForHeader,
            StringValues forwardedHostHeader,
            StringValues forwardedProtoHeader,
            StringValues forwardedPrefixHeader);
    }
}
