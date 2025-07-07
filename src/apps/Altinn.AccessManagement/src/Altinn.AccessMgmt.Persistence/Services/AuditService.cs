using System.Diagnostics;
using Altinn.AccessMgmt.Persistence.Core.Models;
using Microsoft.AspNetCore.Http;

namespace Altinn.AccessMgmt.Persistence.Services;

/// <summary>
/// Middleware that sets Audit state
/// </summary>
internal class AuditService(IDbAuditService auditService) : IMiddleware
{
    private IDbAuditService DbAuditService { get; } = auditService;

    /// <summary>
    /// Middleware that creates change request object
    /// </summary>
    /// <param name="context"><see cref="HttpContext"/></param>
    /// <param name="next"><see cref="RequestDelegate"/></param>
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.GetEndpoint() is var endpoint && endpoint != null)
        {
            if (endpoint.Metadata.GetMetadata<DbAuditAttribute>() is var attr && attr != null)
            {
                var claim = context.User?.Claims?
                    .FirstOrDefault(c => c.Type.Equals(attr.Claim, StringComparison.OrdinalIgnoreCase));

                if (claim != null && Guid.TryParse(claim.Value, out var uuid))
                {
                    DbAuditService.Set(new()
                    {
                        ChangedBy = uuid,
                        ChangedBySystem = Guid.Parse(attr.System),
                        ChangeOperationId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier,
                    });
                }
            }
        }

        await next(context);
    }
}

/// <summary>
/// Attribute to decorate actions
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class DbAuditAttribute : Attribute
{
    /// <summary>
    /// Token claim that gets set to ChangedBy. Claim must be UUID.
    /// </summary>
    public string Claim { get; set; }

    /// <summary>
    /// Which system that initiates the request.
    /// </summary>
    public string System { get; set; }
}

/// <inheritdoc/>
internal class AuditFactory(IHttpContextAccessor accessor) : IDbAudit, IDbAuditService
{
    private IHttpContextAccessor Accessor { get; } = accessor;

    /// <summary>
    /// Gets the <see cref="ChangeRequestOptions"/> 
    /// </summary>
    public ChangeRequestOptions Value =>
        Accessor.HttpContext.Items.TryGetValue(nameof(AuditFactory), out var result) ?
            result as ChangeRequestOptions :
            throw new InvalidOperationException($"Failed to retrieve {nameof(ChangeRequestOptions)} from HttpContext. Is action decorated with attribute {nameof(DbAuditAttribute)}?");

    /// <summary>
    /// Sets the <see cref="ChangeRequestOptions"/> 
    /// </summary>
    public void Set(ChangeRequestOptions options) =>
        Accessor.HttpContext.Items.TryAdd(nameof(AuditFactory), options);
}

/// <summary>
/// Factory for getting <see cref="ChangeRequestOptions"/> 
/// </summary>
public interface IDbAudit
{
    /// <summary>
    /// Value
    /// </summary>
    public ChangeRequestOptions Value { get; }
}

/// <summary>
/// Sets the Audit service
/// </summary>
internal interface IDbAuditService
{
    /// <summary>
    /// Sets the ChangeRequestOptions 
    /// </summary>
    void Set(ChangeRequestOptions options);
}
