using Microsoft.AspNetCore.Authorization;

namespace Altinn.AccessMgmt.Core.Authorization;

/// <summary>
/// Default implementation of <see cref="IAuthorizationScopeProvider"/> that retrieves scopes from user claims.
/// </summary>
/// <remarks>
/// https://github.com/Altinn/altinn-authorization-utils/blob/main/src/Altinn.Authorization.ServiceDefaults/src/ServiceDefaults.Authorization/Scopes/DefaultAuthorizationScopeProvider.cs"
/// </remarks>
internal sealed class DefaultAuthorizationScopeProvider: IAuthorizationScopeProvider
{
    /// <inheritdoc/>
    public IEnumerable<string> GetScopeStrings(AuthorizationHandlerContext context)
    {
        foreach (var identity in context.User.Identities.Where(static i => string.Equals(i.AuthenticationType, "AuthenticationTypes.Federation")))
        {
            foreach (var claim in identity.Claims.Where(static c => string.Equals(c.Type, "urn:altinn:scope")))
            {
                yield return claim.Value;
            }
        }

        foreach (var claim in context.User.Claims.Where(static c => string.Equals(c.Type, "scope")))
        {
            yield return claim.Value;
        }
    }
}

/// <summary>
/// Defines a provider that supplies authorization scopes based on the specified authorization context.
/// </summary>
public interface IAuthorizationScopeProvider
{
    /// <summary>
    /// Retrieves the collection of authorization scopes associated with the specified authorization context.
    /// </summary>
    /// <param name="context">The authorization context containing information about the current request and user. Cannot be <see langword="null"/>.</param>
    /// <returns>An enumerable collection of strings representing the available space-separated scope-strings for the authorization context. The
    /// collection will be empty if no scopes are associated.</returns>
    public IEnumerable<string> GetScopeStrings(AuthorizationHandlerContext context);
}
