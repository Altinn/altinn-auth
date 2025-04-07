using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Filters;
using Microsoft.AspNetCore.Http;

namespace Altinn.AccessManagement.Core.Extensions;

/// <summary>
/// Provides extension methods for accessing and manipulating data within the HTTP context.
/// </summary>
/// <remarks>
/// This class contains extension methods that simplify accessing specific data, like the party UUID, 
/// from the HTTP context in a way that can be easily used throughout the application.
/// </remarks>
public static class HttpContextAccessorExtensions
{
    /// <summary>
    /// Retrieves the party UUID from the HTTP context items.
    /// </summary>
    /// <param name="accessor">The HTTP context accessor used to access the current request's context.</param>
    /// <returns>The party UUID if found.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the UUID is not present in the context. Ensure that the 
    /// <see cref="AuthorizePartyUuidClaimFilter"/> is enabled for the controller action.
    /// </exception>
    public static Guid GetPartyUuid(this IHttpContextAccessor accessor)
    {
        var claim = accessor.HttpContext.User?.Claims?
            .FirstOrDefault(c => c.Type.Equals(AltinnCoreClaimTypes.PartyUuid, StringComparison.OrdinalIgnoreCase));

        if (claim != null && Guid.TryParse(claim.Value, out var result))
        {
            return result;
        }

        throw new InvalidOperationException($"Failed to retrieve UUID. Is the '{nameof(AuthorizePartyUuidClaimFilter)}' ServiceFilter enabled for this action?");
    }
}
