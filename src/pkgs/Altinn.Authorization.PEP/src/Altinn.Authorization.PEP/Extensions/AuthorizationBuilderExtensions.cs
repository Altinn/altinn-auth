using Altinn.Common.PEP.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace Altinn.Authorization.PEP.Extensions;

/// <summary>
/// Provides extension methods for configuring Altinn-specific authorization policies.
/// </summary>
public static class AuthorizationBuilderExtensions
{
    /// <summary>
    /// Adds a claim-based access policy to the authorization builder.
    /// </summary>
    /// <param name="builder">The <see cref="AuthorizationBuilder"/> to which the policy will be added.</param>
    /// <param name="name">The name of the policy.</param>
    /// <param name="type">The claim type required by the policy.</param>
    /// <param name="value">The claim value required by the policy.</param>
    /// <exception cref="ArgumentException">Thrown if <paramref name="name"/>, <paramref name="type"/>, or <paramref name="value"/> is null or empty.</exception>
    /// <returns>The updated <see cref="AuthorizationBuilder"/>.</returns>
    public static AuthorizationBuilder AddAltinnPEPClaimAccessPolicy(this AuthorizationBuilder builder, string name, string type, string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));
        ArgumentException.ThrowIfNullOrEmpty(type, nameof(type));
        ArgumentException.ThrowIfNullOrEmpty(value, nameof(value));
        return builder.AddPolicy(name, policy => policy.Requirements.Add(new ClaimAccessRequirement(type, value)));
    }

    /// <summary>
    /// Adds a resource-based access policy to the authorization builder.
    /// </summary>
    /// <param name="builder">The <see cref="AuthorizationBuilder"/> to which the policy will be added.</param>
    /// <param name="name">The name of the policy.</param>
    /// <param name="resourceId">The identifier of the resource to protect.</param>
    /// <param name="actionType">The type of action permitted by the policy.</param>
    /// <exception cref="ArgumentException">Thrown if <paramref name="name"/>, <paramref name="resourceId"/>, or <paramref name="actionType"/> is null or empty.</exception>
    /// <returns>The updated <see cref="AuthorizationBuilder"/>.</returns>
    public static AuthorizationBuilder AddAltinnPEPResourceAccessPolicy(this AuthorizationBuilder builder, string name, string resourceId, string actionType)
    {
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));
        ArgumentException.ThrowIfNullOrEmpty(resourceId, nameof(resourceId));
        ArgumentException.ThrowIfNullOrEmpty(actionType, nameof(actionType));
        return builder.AddPolicy(name, policy => policy.Requirements.Add(new ResourceAccessRequirement(resourceId, actionType)));
    }

    /// <summary>
    /// Adds a scope-based access policy to the authorization builder.
    /// </summary>
    /// <param name="builder">The <see cref="AuthorizationBuilder"/> to which the policy will be added.</param>
    /// <param name="name">The name of the policy.</param>
    /// <param name="scopes">An array of scopes required by the policy.</param>
    /// <exception cref="ArgumentException">Thrown if <paramref name="name"/> is null or empty or if <paramref name="scopes"/> contains null or empty values.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="scopes"/> is null.</exception>
    /// <returns>The updated <see cref="AuthorizationBuilder"/>.</returns>
    public static AuthorizationBuilder AddAltinnPEPScopePolicy(this AuthorizationBuilder builder, string name, params string[] scopes)
    {
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));
        ArgumentNullException.ThrowIfNull(scopes, nameof(scopes));
        return builder.AddPolicy(name, policy => policy.Requirements.Add(new ScopeAccessRequirement(scopes)));
    }
}
