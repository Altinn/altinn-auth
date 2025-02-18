namespace Altinn.Authorization.Host.Appsettings;

/// <summary>
/// Represents the application settings for Altinn authorization, specifically for Azure App Configuration.
/// This class encapsulates settings related to feature flags, key-value labels, and the Azure App Configuration endpoint.
/// </summary>
public class AppConfigurationSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether Azure App Configuration is enabled.
    /// When set to <c>true</c>, the application will load configuration settings from Azure App Configuration.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Gets or sets the Azure App Configuration endpoint URI.
    /// This is required to connect to the Azure App Configuration service.
    /// </summary>
    public Uri Endpoint { get; set; }
}
