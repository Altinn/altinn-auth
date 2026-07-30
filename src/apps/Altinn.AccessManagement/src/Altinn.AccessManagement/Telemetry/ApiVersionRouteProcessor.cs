using System.Diagnostics;
using System.Text.RegularExpressions;
using OpenTelemetry;

namespace Altinn.AccessManagement.Telemetry;

/// <summary>
/// Replaces API version route parameters such as <c>{version:apiVersion}</c> in the recorded HTTP route
/// with the version the request actually used, so telemetry groups requests per API version.
/// Other route parameters are left as template placeholders.
/// </summary>
public sealed partial class ApiVersionRouteProcessor(IHttpContextAccessor httpContextAccessor) : BaseProcessor<Activity>
{
    private const string HttpRouteTag = "http.route";

    /// <inheritdoc/>
    public override void OnEnd(Activity activity)
    {
        if (activity.Kind != ActivityKind.Server)
        {
            return;
        }

        if (activity.GetTagItem(HttpRouteTag) is not string route || !route.Contains(":apiVersion}", StringComparison.Ordinal))
        {
            return;
        }

        var routeValues = httpContextAccessor.HttpContext?.Request.RouteValues;
        if (routeValues is null)
        {
            return;
        }

        var resolved = ApiVersionParameter().Replace(route, match =>
        {
            var name = match.Groups["name"].Value;
            if (routeValues.TryGetValue(name, out var value) && value?.ToString() is { Length: > 0 } segment)
            {
                return segment;
            }

            return match.Value;
        });

        if (string.Equals(resolved, route, StringComparison.Ordinal))
        {
            return;
        }

        activity.SetTag(HttpRouteTag, resolved);
        activity.DisplayName = activity.DisplayName.Replace(route, resolved, StringComparison.Ordinal);
    }

    [GeneratedRegex(@"\{(?<name>[^{}:]+):apiVersion\}")]
    private static partial Regex ApiVersionParameter();
}
