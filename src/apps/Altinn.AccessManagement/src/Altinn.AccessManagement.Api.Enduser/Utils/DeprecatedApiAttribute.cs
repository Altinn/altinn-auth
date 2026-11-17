using System.Globalization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Altinn.AccessManagement.Api.Enduser.Utils;

/// <summary>
/// Marks endpoints as deprecated with the standard response headers: <c>Deprecation</c> (RFC 9745), and
/// <c>Link</c> with <c>rel="successor-version"</c> (RFC 8288) when <see cref="SuccessorVersion"/> is set.
/// Put it on the versioned controller, not on a shared base class, so other versions do not get it.
/// </summary>
/// <remarks>
/// Runs as a result filter, so responses short-circuited before the action, such as 401 and 403 from
/// the authorization policies, do not carry the headers.
/// </remarks>
/// <param name="deprecationDate">The date the endpoints are deprecated, as an ISO 8601 date in UTC (e.g. 2027-05-31).</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class DeprecatedApiAttribute(string deprecationDate) : ResultFilterAttribute
{
    private readonly long _deprecation = DateTimeOffset
        .Parse(deprecationDate, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal)
        .ToUnixTimeSeconds();

    /// <summary>
    /// The path of the version that replaces the deprecated endpoints, sent as a <c>successor-version</c> link.
    /// </summary>
    public string? SuccessorVersion { get; set; }

    /// <inheritdoc/>
    public override void OnResultExecuting(ResultExecutingContext context)
    {
        var headers = context.HttpContext.Response.Headers;
        headers["Deprecation"] = $"@{_deprecation}";

        if (SuccessorVersion is not null)
        {
            headers.Append("Link", $"<{SuccessorVersion}>; rel=\"successor-version\"");
        }

        base.OnResultExecuting(context);
    }
}
