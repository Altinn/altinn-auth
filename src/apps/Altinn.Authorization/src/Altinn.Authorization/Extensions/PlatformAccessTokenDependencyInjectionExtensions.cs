using Altinn.Authorization.Services;
using Altinn.Common.AccessTokenClient.Services;
using Altinn.Platform.Authorization.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.Platform.Authorization.Extensions;

/// <summary>
/// Extension methods for adding services to the dependency injection container in order to support platform service integrations requiring platform access token.
/// </summary>
public static class PlatformAccessTokenDependencyInjectionExtensions
{
    /// <summary>
    /// Registers services to the dependency injection container in order to support platform service integrations requiring platform access token.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="config">The <see cref="IConfiguration"/>.</param>
    /// <param name="isDevelopment">Whether the setup is for local dev environment. Will setup the platform token support using the web based Altinn test token generator.</param>
    /// <returns><paramref name="services"/> for further chaining.</returns>
    public static IServiceCollection AddPlatformAccessTokenSupport(
        this IServiceCollection services, IConfiguration config, bool isDevelopment)
    {
        if (isDevelopment)
        {
            services.Configure<TokenGeneratorSettings>(config.GetSection("TokenGeneratorSettings"));
            services.AddSingleton<IAccessTokenGenerator, DevAccessTokenGenerator>();
        }
        else
        {
            services.AddSingleton<IAccessTokenGenerator, AccessTokenGenerator>();
        }

        return services;
    }
}
