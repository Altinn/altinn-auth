namespace Altinn.Authorization.Host;

/// <summary>
/// Represents the application settings options used for configuration.
/// </summary>
public class AppSettingsOptions
{
    /// <summary>
    /// Gets or sets the labels used for app configuration.
    /// These labels help in fetching specific configuration settings.
    /// </summary>
    public IEnumerable<string> AppConfigurationLabels { get; set; } = [];
}
