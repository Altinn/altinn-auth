using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Altinn.AccessManagement.Core.Constants;
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
        IAuditAccessor auditContextAccessor = context.RequestServices.GetRequiredService<IAuditAccessor>();
        if (context.GetEndpoint() is var endpoint && endpoint != null)
        {
            if (endpoint.Metadata.GetMetadata<AuditJWTClaimToDbAttribute>() is var jwtClaimToDb && jwtClaimToDb != null)
            {
                Claim claim = context.User?.Claims?
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
                ConsentPartyUrn party = OrgUtil.GetAuthenticatedParty(context.User);
                if (party is not null)
                {
                    var db = context.RequestServices.GetRequiredService<AppDbContext>();
                    var consumer = await GetEntityFromConsumerClaim(db, context, party);
                    if (consumer is { })
                    {
                        auditContextAccessor.AuditValues = new(consumer.Id, SystemEntityConstants.ServiceOwnerApi, TraceId(context));
                    }
                }
            }
            else if (endpoint.Metadata.GetMetadata<AuditPlatformStaticDbAttribute>() is var platformStaticDb && platformStaticDb != null)
            {
                if (platformStaticDb.ChangedBy != null && platformStaticDb.System != null)
                {
                    auditContextAccessor.AuditValues = new(Guid.Parse(platformStaticDb.ChangedBy), Guid.Parse(platformStaticDb.System), AppAndTraceId(context), DateTimeOffset.UtcNow);
                }
                else if (platformStaticDb.System != null)
                {
                    auditContextAccessor.AuditValues = new(Guid.Parse(platformStaticDb.System), Guid.Parse(platformStaticDb.System), AppAndTraceId(context), DateTimeOffset.UtcNow);
                }
            }
        }

        await next(context);
    }

    private static string TraceId(HttpContext context)
    {
        return Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
    }

    private static string AppAndTraceId(HttpContext context)
    {
        string appid = null;
        try
        {
            string token = context.Request.Headers["PlatformAccessToken"];
            if (!string.IsNullOrEmpty(token))
            {
                var handler = new JwtSecurityTokenHandler();
                if (handler.CanReadToken(token))
                {
                    var jwtSecurityToken = handler.ReadJwtToken(token);
                    var appidentifier = jwtSecurityToken.Claims.FirstOrDefault(c => c.Type == AltinnXacmlConstants.MatchAttributeIdentifiers.AppAttribute);
                    if (appidentifier != null)
                    {
                        appid = $"app_{jwtSecurityToken.Issuer}_{appidentifier.Value}";
                    }
                }
            }
        }
        catch (Exception)
        {
        }

        if (appid is not null)
        {
            return $"{appid}_{TraceId(context)}";
        }

        return TraceId(context);
    }

    private static async Task<Entity> GetEntityFromConsumerClaim(AppDbContext db, HttpContext context, ConsentPartyUrn party)
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
    private static Claim? GetSystemUserClaim(IEnumerable<Claim>? claims)
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
