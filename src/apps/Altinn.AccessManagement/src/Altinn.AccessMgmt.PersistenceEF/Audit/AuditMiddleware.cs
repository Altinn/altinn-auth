using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using Altinn.AccessMgmt.PersistenceEF.Models.Claims;
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

                if (claim == null && jwtClaimToDb.AllowSystemUser)
                {
                    claim = GetSystemUserClaim(context.User?.Claims);
                }

                if (claim != null && Guid.TryParse(claim.Value, out var uuid))
                {
                    auditContextAccessor.AuditValues = new(uuid, Guid.Parse(jwtClaimToDb.System), Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier, DateTimeOffset.UtcNow);
                }
            }

            if (endpoint.Metadata.GetMetadata<AuditStaticDbAttribute>() is var staticDb && staticDb != null)
            {
                auditContextAccessor.AuditValues = new(Guid.Parse(staticDb.ChangedBy), Guid.Parse(staticDb.System), Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier, DateTimeOffset.UtcNow);
            }
        }

        await next(context);
    }

    /// <summary>
    /// Find the special system user claim and return it as standard claim if available
    /// </summary>
    private Claim? GetSystemUserClaim(IEnumerable<Claim>? claims)
    {
        Claim? authorizationDetails = claims?.FirstOrDefault(c => c.Type.Equals("authorization_details"));

        if (authorizationDetails != null)
        {
            JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);
            SystemUserClaim systemUserClaimCore = JsonSerializer.Deserialize<SystemUserClaim>(authorizationDetails.Value, jsonOptions);
            
            if (systemUserClaimCore?.Systemuser_id != null && systemUserClaimCore.Systemuser_id.Count > 0)
            {
                return new Claim("urn:altinn:party:uuid", systemUserClaimCore.Systemuser_id[0]);
            }
        }

        return null;
    }
}
