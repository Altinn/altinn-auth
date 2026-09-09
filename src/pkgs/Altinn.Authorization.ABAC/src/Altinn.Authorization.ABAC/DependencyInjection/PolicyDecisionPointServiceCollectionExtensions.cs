using System;
using Altinn.Authorization.ABAC;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for registering the <see cref="PolicyDecisionPoint"/> with an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class PolicyDecisionPointServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the <see cref="PolicyDecisionPoint"/> as a singleton so consumers can resolve it from dependency injection
        /// instead of constructing it directly.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the Policy Decision Point to.</param>
        /// <returns>The same <paramref name="services"/> instance so calls can be chained.</returns>
        public static IServiceCollection AddPolicyDecisionPoint(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.TryAddSingleton<PolicyDecisionPoint>();

            return services;
        }
    }
}
