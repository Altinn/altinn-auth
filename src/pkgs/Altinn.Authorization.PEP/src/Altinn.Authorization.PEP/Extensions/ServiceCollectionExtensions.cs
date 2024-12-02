using Altinn.Common.PEP.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Altinn.Authorization.PEP.Extensions;

/// <summary>
/// Provides extension methods for registering Altinn-specific authorization services.
/// Must be called atleast once if registering using extensions methods from <see cref="AuthorizationBuilderExtensions"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds authorization handlers required for Altinn PEP (Policy Enforcement Point).
    /// Registers handlers for claim-based, resource-based, and scope-based access control.
    /// <see cref="ClaimAccessHandler"/>, <see cref="ResourceAccessHandler"/> and <see cref="ResourceAccessHandler"/>
    /// </summary>
    public static IServiceCollection AddAltinnPEP(this IServiceCollection services)
    {
        services.TryAddScoped<IAuthorizationHandler, ClaimAccessHandler>();
        services.TryAddScoped<IAuthorizationHandler, ResourceAccessHandler>();
        services.TryAddScoped<IAuthorizationHandler, ScopeAccessHandler>();
        return services;
    }
}
