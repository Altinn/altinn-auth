using Microsoft.Extensions.Configuration;

namespace Altinn.Authorization.Host.Startup;

/// <summary>
/// Provides a singleton instance of the application configuration during startup.
/// This class ensures that configuration settings are loaded from <c>appsettings.Startup.json</c>
/// and made available throughout the application's initialization phase.
/// </summary>
public static class StartupConfiguration
{
    /// <summary>
    /// Gets the application configuration instance.
    /// This instance loads settings from <c>appsettings.Startup.json</c>.
    /// </summary>
    public static IConfiguration Instance { get; } = new ConfigurationBuilder()
        .AddJsonFile("appsettings.Startup.json", optional: true, reloadOnChange: false)
        .Build();
}
