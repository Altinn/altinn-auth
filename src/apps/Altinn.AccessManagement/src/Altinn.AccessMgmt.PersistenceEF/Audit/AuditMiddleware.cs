using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.AccessMgmt.PersistenceEF.Audit;

public class AuditMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var auditContextAccessor = context.RequestServices.GetRequiredService<IAuditAccessor>();
        if (context.GetEndpoint() is var endpoint && endpoint != null)
        {
            if (endpoint.Metadata.GetMetadata<AuditJWTClaimToDbAttribute>() is var jwtClaimToDb && jwtClaimToDb != null)
            {
                var claim = context.User?.Claims?
                    .FirstOrDefault(c => c.Type.Equals(jwtClaimToDb.Claim, StringComparison.OrdinalIgnoreCase));

                if (claim != null && Guid.TryParse(claim.Value, out var uuid))
                {
                    auditContextAccessor.AuditValues = new(uuid, Guid.Parse(jwtClaimToDb.System), Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier);
                }
            }

            if (endpoint.Metadata.GetMetadata<AuditStaticDbAttribute>() is var staticDb && staticDb != null)
            {
                auditContextAccessor.AuditValues = new(Guid.Parse(staticDb.ChangedBy), Guid.Parse(staticDb.System), Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier);
            }
        }

        await next(context);
    }
}
