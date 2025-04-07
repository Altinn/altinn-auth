using Altinn.AccessManagement.Core.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Altinn.AccessManagement.Core.Filters;

/// <summary>
/// Verifies that user uuid claim exists in token and adds the value to http context items bag.
/// If claim is not present it will return cancel pipeline and return 401 unauthorized.
/// </summary>
public partial class AuthorizePartyUuidClaimFilter(ILogger<AuthorizePartyUuidClaimFilter> logger) : IAuthorizationFilter
{
    private ILogger<AuthorizePartyUuidClaimFilter> Logger { get; } = logger;

    /// <inheritdoc/>
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (context.HttpContext.User?.Identity?.IsAuthenticated != true)
        {
            Log.ThereIsNoAuthorizedUser(Logger);
            context.Result = new UnauthorizedResult();
            return;
        }

        var claim = context.HttpContext.User?.Claims?
            .FirstOrDefault(c => c.Type.Equals(AltinnCoreClaimTypes.PartyUuid, StringComparison.OrdinalIgnoreCase));

        if (claim != null && Guid.TryParse(claim.Value, out _))
        {
            return;
        }

        Log.MissingClaimInToken(Logger);
        context.Result = new ForbidResult();
    }

    static partial class Log
    {
        [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = $"User is not authenticated.")]
        internal static partial void ThereIsNoAuthorizedUser(ILogger logger);

        [LoggerMessage(EventId = 0, Level = LogLevel.Warning, Message = $"User is authorized, but the claim '{AltinnCoreClaimTypes.PartyUuid}' is somehow missing from the token.")]
        internal static partial void MissingClaimInToken(ILogger logger);
    }
}
