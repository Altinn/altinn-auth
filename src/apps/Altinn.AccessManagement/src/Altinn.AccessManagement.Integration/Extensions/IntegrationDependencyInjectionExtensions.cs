using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessManagement.Integration.Clients;
using Altinn.AccessManagement.Integration.Services;
using Altinn.AccessManagement.Integration.Services.Interfaces;
using Altinn.AccessManagement.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace Altinn.AccessManagement.Integration.Extensions;

/// <summary>
/// Extension methods for adding access management services to the dependency injection container.
/// </summary>
public static class IntegrationDependencyInjectionExtensions
{
    /// <summary>
    /// Registers access management integration services with the dependency injection container.
    /// </summary>
    /// <param name="builder">The <see cref="WebApplicationBuilder"/>.</param>
    /// <returns><paramref name="builder"/> for further chaining.</returns>
    public static WebApplicationBuilder AddAccessManagementIntegration(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IKeyVaultService, KeyVaultService>();
        builder.Services.AddSingleton<IPlatformAuthorizationTokenProvider, PlatformAuthorizationTokenProvider>();
        builder.Services.AddHttpClient<IDelegationRequestsWrapper, DelegationRequestProxy>();
        builder.Services.AddSingleton<IEventMapperService, EventMapperService>();

        builder.Services.AddHttpClient<IPartiesClient, PartiesClient>();
        builder.Services.AddHttpClient<IProfileClient, ProfileClient>();
        builder.Services.AddHttpClient<IAccessListsAuthorizationClient, AccessListAuthorizationClient>();
        builder.Services.AddHttpClient<IAltinn2RightsClient, Altinn2RightsClient>();
        builder.Services.AddHttpClient<IAltinn2ConsentClient, Altinn2ConsentClient>();
        builder.Services.AddHttpClient<IAuthenticationClient, AuthenticationClient>();
        builder.Services.AddSingleton<IResourceRegistryClient, ResourceRegistryClient>();

        builder.Services.AddHttpClient<IAltinnRolesClient, AltinnRolesClient>()
            .ReplaceResilienceHandler(static c =>
            {
                c.Retry.ShouldHandle = static _ => ValueTask.FromResult(false);
                c.AttemptTimeout.Timeout = TimeSpan.FromSeconds(15);
            });

        return builder;
    }

    /// <summary>
    /// Configures the standard resilience handler for the HTTP client.
    /// </summary>
    /// <param name="builder">A <see cref="IHttpClientBuilder"/>.</param>
    /// <param name="configure">A configuration delegate.</param>
    /// <returns><paramref name="builder"/>.</returns>
    private static IHttpClientBuilder ReplaceResilienceHandler(this IHttpClientBuilder builder, Action<HttpStandardResilienceOptions> configure)
    {
        builder.ConfigureAdditionalHttpMessageHandlers((handlers, _) =>
        {
            for (int i = handlers.Count - 1; i >= 0; i--)
            {
                if (handlers[i] is ResilienceHandler)
                {
                    handlers.RemoveAt(i);
                }
            }
        });

        builder.AddStandardResilienceHandler(configure);

        return builder;
    }
}
