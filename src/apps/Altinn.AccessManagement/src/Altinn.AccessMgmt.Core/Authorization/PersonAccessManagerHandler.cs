using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Altinn.AccessMgmt.Core.Authorization;

/// <summary>
/// Authorization handler that denies access when an access manager for a person
/// attempts to access the "from-others" direction (incoming connections).
/// 
/// Rules:
/// - If direction is not from-others (party ≠ to) → succeed
/// - If user IS the party (acting as self) → succeed
/// - If party is a person → fail (access manager cannot read/write incoming connections)
/// - Otherwise → succeed (access managers for organizations retain bidirectional access)
/// </summary>
public class PersonAccessManagerHandler(
    IHttpContextAccessor httpContextAccessor,
    IEntityService entityService) : AuthorizationHandler<PersonAccessManagerRequirement>
{
    private const string ParamParty = "party";
    private const string ParamTo = "to";

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PersonAccessManagerRequirement requirement)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return;
        }

        var query = httpContext.Request.Query;

        if (!TryGetGuidParam(query, ParamParty, out var party))
        {
            // No party param; let other handlers deal with validation
            context.Succeed(requirement);
            return;
        }

        if (!TryGetGuidParam(query, ParamTo, out var to))
        {
            // No 'to' param means this is not a from-others direction request
            context.Succeed(requirement);
            return;
        }

        if (party != to)
        {
            // Direction is to-others (party == from), not from-others → allow
            context.Succeed(requirement);
            return;
        }

        // Direction is from-others (party == to). Check if user is the party.
        var userPartyUuid = GetUserPartyUuid(httpContext);
        if (userPartyUuid == party)
        {
            // User IS the party (acting as self) → allow (AC-1.2)
            context.Succeed(requirement);
            return;
        }

        // User is an access manager for the party. Check if party is a person.
        var entity = await entityService.GetEntity(party, httpContext.RequestAborted);
        if (entity is null)
        {
            // Cannot determine party type → fail closed for from-others when user is not the party.
            context.Fail();
            return;
        }

        if (entity.TypeId == EntityTypeConstants.Person.Id)
        {
            // Access manager for a person cannot access from-others direction (AC-2.2)
            context.Fail();
            return;
        }

        // Party is not a person (e.g., organization) → allow bidirectional access
        context.Succeed(requirement);
    }

    private static Guid GetUserPartyUuid(HttpContext context)
    {
        var claim = context.User?.Claims.FirstOrDefault(c => c.Type.Equals(AltinnCoreClaimTypes.PartyUuid));
        if (claim is not null && Guid.TryParse(claim.Value, out Guid partyUuid))
        {
            return partyUuid;
        }

        return Guid.Empty;
    }

    private static bool TryGetGuidParam(IQueryCollection query, string paramName, out Guid value)
    {
        if (query.TryGetValue(paramName, out var values) && Guid.TryParse(values.ToString(), out value))
        {
            return true;
        }

        value = Guid.Empty;
        return false;
    }
}
