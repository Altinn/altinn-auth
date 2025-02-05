using System.Diagnostics.CodeAnalysis;
using Altinn.Authorization.Host.AppSettings;
using Altinn.Authorization.ServiceDefaults;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Altinn.Authorization.Host;

/// <summary>
/// WebApp
/// </summary>
[ExcludeFromCodeCoverage]
public static class WebApp
{
    /// <summary>
    /// CreateServiceDefaults
    /// </summary>
    /// <param name="name">name</param>
    /// <param name="args">args</param>
    public static WebApplicationBuilder CreateServiceDefaults(string name, string[] args)
    {
        return AltinnHost.CreateWebApplicationBuilder(name, args);
    }

    /// <summary>
    /// ConfigureAppConfigurationDefeaults
    /// </summary>
    /// <param name="builder">builder</param>
    /// <param name="configureOptions">configureOptions</param>
    public static IHostApplicationBuilder AddAppSettingDefaults(this WebApplicationBuilder builder, Action<AppSettingsOptions> configureOptions = null)
    {
        var options = new AppSettingsOptions();
        configureOptions?.Invoke(options);
        builder.Services.Configure<AltinnAppSettings>(builder.Configuration);
        var appsettings = new AltinnAppSettings(builder.Configuration);

        if (appsettings?.AppConfiguration?.Endpoint != null)
        {
            builder.Configuration.AddAzureAppConfiguration(cfg =>
            {
                cfg.Connect(appsettings.AppConfiguration.Endpoint, DefaultTokenCredential.Instance);
                cfg.ConfigureStartupOptions(startup =>
                {
                    startup.Timeout = TimeSpan.FromSeconds(3);
                });

                cfg.ConfigureKeyVault(kv =>
                {
                    kv.SetCredential(DefaultTokenCredential.Instance);
                });

                cfg.ConfigureRefresh(refresh =>
                {
                    refresh.Register("Sentinel", refreshAll: true);
                });

                foreach (var label in options.AzureAppConfiguration.AppKeyValueLabels)
                {
                    cfg.Select("*", label);
                }

                cfg.UseFeatureFlags(flags =>
                {
                    foreach (var label in options.AzureAppConfiguration.AppFeatureFlagLabels)
                    {
                        flags.Select("*", label);
                    }
                });
            });
        }

        return builder;
    }
}
