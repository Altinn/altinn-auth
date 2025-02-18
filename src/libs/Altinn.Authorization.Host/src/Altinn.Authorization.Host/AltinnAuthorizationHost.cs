using System.Diagnostics.CodeAnalysis;
using Altinn.Authorization.Host.Identity;
using Altinn.Authorization.Host.Startup;
using Altinn.Authorization.ServiceDefaults;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Altinn.Authorization.Host;

/// <summary>
/// WebApp
/// </summary>
[ExcludeFromCodeCoverage]
public static partial class AltinnAuthorizationHost
{
    private static ILogger Logger { get; } = StartupLoggerFactory.Create(nameof(AltinnAuthorizationHost));

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
    public static IHostApplicationBuilder AddAzureAppConfigurationDefaults(this WebApplicationBuilder builder, Action<AltinnAuthorizationHostOptions> configureOptions)
    {
        var options = new AltinnAuthorizationHostOptions();
        configureOptions?.Invoke(options);
        if (!options.Enabled)
        {
            Log.SkipAddAzureAppConfiguration(Logger);
            return builder;
        }

        Log.AddAzureAppConfiguration(Logger, options.Endpoint);
        builder.Configuration.AddAzureAppConfiguration(cfg =>
        {
            cfg.Connect(options.Endpoint, AzureToken.Default);
            cfg.ConfigureStartupOptions(startup =>
            {
                startup.Timeout = TimeSpan.FromSeconds(3);
            });

            cfg.ConfigureKeyVault(kv =>
            {
                kv.SetCredential(AzureToken.Default);
            });

            cfg.ConfigureRefresh(refresh =>
            {
                refresh.Register("Sentinel", refreshAll: true);
            });

            foreach (var label in options.AppKeyValueLabels)
            {
                Log.AddKeyValueLabel(Logger, label);
                cfg.Select("*", label);
            }

            cfg.UseFeatureFlags(flags =>
            {
                foreach (var label in options.AppFeatureFlagLabels)
                {
                    Log.AddFeatureFlagLabel(Logger, label);
                    flags.Select("*", label);
                }
            });
        });

        return builder;
    }

    static partial class Log
    {
        [LoggerMessage(EventId = 0, Level = LogLevel.Debug, Message = "skip add azure app configuration to configuration")]
        internal static partial void SkipAddAzureAppConfiguration(ILogger logger);

        [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "add azure app configuration {endpoint} to configuration")]
        internal static partial void AddAzureAppConfiguration(ILogger logger, Uri endpoint);

        [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "load feature flags with label {label} from azure app configuration")]
        internal static partial void AddFeatureFlagLabel(ILogger logger, string label);

        [LoggerMessage(EventId = 3, Level = LogLevel.Debug, Message = "load key values with label {label} from azure app configuration")]
        internal static partial void AddKeyValueLabel(ILogger logger, string label);
    }
}
