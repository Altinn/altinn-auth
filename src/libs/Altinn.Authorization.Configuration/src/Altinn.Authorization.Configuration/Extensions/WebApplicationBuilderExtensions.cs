using Altinn.Authorization.Configuration.AppSettings;
using Altinn.Authorization.Configuration.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;

namespace Altinn.Authorization.Configuration.Extensions;

/// <summary>
/// Provides extension methods for configuring Azure App Configuration within a <see cref="WebApplicationBuilder"/>.
/// This class contains methods to integrate Azure App Configuration for Altinn applications, allowing 
/// seamless retrieval of application settings and feature flags.
/// </summary>
public static class WebApplicationBuilderExtensions
{
    /// <summary>
    /// Adds Altinn application configuration to the <see cref="WebApplicationBuilder"/>. 
    /// This method configures Azure App Configuration using the specified settings and options.
    /// </summary>
    /// <typeparam name="TAppSettings">The type of the application settings class that will be bound to the configuration.</typeparam>
    /// <param name="builder">The instance of <see cref="WebApplicationBuilder"/> to configure.</param>
    /// <param name="configureOptions">An optional delegate for configuring <see cref="AltinnAppConfigurationOptions"/>.</param>
    /// <returns>The configured instance of <see cref="WebApplicationBuilder"/> for method chaining.</returns>
    public static IHostApplicationBuilder AddAltinnAppConfiguration<TAppSettings>(this IHostApplicationBuilder builder, Action<AltinnAppConfigurationOptions> configureOptions = null)
        where TAppSettings : class, new()
    {
        builder.AddAltinnAppConfiguration(configureOptions);
        if (typeof(TAppSettings) != typeof(AltinnAppSettings))
        {
            builder.Services.Configure<TAppSettings>(builder.Configuration.Bind);
        }

        return builder;
    }

    /// <summary>
    /// Adds Altinn application configuration to the <see cref="WebApplicationBuilder"/>. 
    /// This method configures Azure App Configuration using the specified settings and options.
    /// </summary>
    /// <param name="builder">The instance of <see cref="WebApplicationBuilder"/> to configure.</param>
    /// <param name="configureOptions">An optional delegate for configuring <see cref="AltinnAppConfigurationOptions"/>.</param>
    /// <returns>The configured instance of <see cref="WebApplicationBuilder"/> for method chaining.</returns>
    public static IHostApplicationBuilder AddAltinnAppConfiguration(this IHostApplicationBuilder builder, Action<AltinnAppConfigurationOptions> configureOptions = null)
    {
        var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger("AppConfig");
        var altinnAppSettings = new AltinnAppSettings(builder.Configuration);

        logger.LogInformation("Using Client {clientid}", altinnAppSettings.EntraId.Identities.Service.ClientId);
        builder.Services.AddOptions();
        builder.Services.Configure<AltinnAppSettings>(builder.Configuration.Bind);

        builder.Configuration.AddAzureAppConfiguration(opts =>
        {
            var options = new AltinnAppConfigurationOptions(configureOptions);
            var altinnAppSettings = new AltinnAppSettings(builder.Configuration);

            opts.Connect(altinnAppSettings.AppConfiguration.Endpoint, altinnAppSettings.EntraId.Identities.Service.TokenCredential);
            opts.ConfigureKeyVault(keyvault => keyvault.SetCredential(altinnAppSettings.EntraId.Identities.Service.TokenCredential));

            opts.ConfigureRefresh(refresh =>
            {
                refresh.SetRefreshInterval(TimeSpan.FromMinutes(1));
                refresh.Register("Sentinel", true);
            });

            foreach (var label in options.FeatureLabels)
            {
                opts.UseFeatureFlags(flags => flags.Select(KeyFilter.Any, label));
            }

            foreach (var label in options.KeyLabels)
            {
                opts.Select(KeyFilter.Any, label);
            }
        });

        return builder;
    }
}
