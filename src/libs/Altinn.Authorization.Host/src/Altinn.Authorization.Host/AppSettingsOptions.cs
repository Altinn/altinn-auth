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
    public AzureAppConfigurationOptions AzureAppConfiguration { get; set; }

    /// <summary>
    /// Azure App Configuration
    /// </summary>
    public class AzureAppConfigurationOptions
    {
        /// <summary>
        /// Key Value
        /// </summary>
        public IEnumerable<string> AppKeyValueLabels { get; set; } = [];

        /// <summary>
        /// Feature Flags
        /// </summary>
        public IEnumerable<string> AppFeatureFlagLabels { get; set; } = [];
    }
}
