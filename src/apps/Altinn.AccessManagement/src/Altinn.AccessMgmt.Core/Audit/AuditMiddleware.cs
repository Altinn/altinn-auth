using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using Altinn.AccessManagement.Core.Models.Consent;
using Altinn.AccessMgmt.Core.Utils;
using Altinn.AccessMgmt.PersistenceEF.Audit;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Models.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.AccessMgmt.Core.Audit;

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
                    auditContextAccessor.AuditValues = new(uuid, Guid.Parse(jwtClaimToDb.System), TraceId(context), DateTimeOffset.UtcNow);
                }
            }
            else if (endpoint.Metadata.GetMetadata<AuditStaticDbAttribute>() is var staticDb && staticDb != null)
            {
                if (staticDb.ChangedBy != null && staticDb.System != null)
                {
                    auditContextAccessor.AuditValues = new(Guid.Parse(staticDb.ChangedBy), Guid.Parse(staticDb.System), TraceId(context), DateTimeOffset.UtcNow);
                }
                else if (staticDb.System != null)
                {
                    auditContextAccessor.AuditValues = new(Guid.Parse(staticDb.System), Guid.Parse(staticDb.System), TraceId(context), DateTimeOffset.UtcNow);
                }
            }
            else if (endpoint.Metadata.GetMetadata<AuditServiceOwnerConsumerAttribute>() is var serviceOwnerConsumer && serviceOwnerConsumer is { })
            {
                var party = OrgUtil.GetAuthenticatedParty(context.User);
                if (party is { })
                {
                    var db = context.RequestServices.GetRequiredService<AppDbContext>();
                    var entity = await GetEntityFromConsumerClaim(db, context, party);
                    if (entity is { })
                    {
                        auditContextAccessor.AuditValues = new(entity.Id, SystemEntityConstants.ServiceOwnerApi, TraceId(context));
                    }
                }
            }
        }

        await next(context);
    }

    private static string TraceId(HttpContext context)
    {
        return Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
    }

    private async Task<Entity> GetEntityFromConsumerClaim(AppDbContext db, HttpContext context, ConsentPartyUrn party)
    {
        if (party.IsOrganizationId(out var organizationIdentifier))
        {
            return await db.Entities.FirstOrDefaultAsync(e => e.OrganizationIdentifier == organizationIdentifier.ToString(), context.RequestAborted);
        }

        return null;
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
