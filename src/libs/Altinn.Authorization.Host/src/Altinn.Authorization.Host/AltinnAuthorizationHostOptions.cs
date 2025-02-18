namespace Altinn.Authorization.Host;

/// <summary>
/// Represents the application settings for Altinn authorization, specifically for Azure App Configuration.
/// This class encapsulates settings related to feature flags, key-value labels, and the Azure App Configuration endpoint.
/// </summary>
public class AltinnAuthorizationHostOptions
{
    /// <summary>
    /// Gets or sets a list of labels used for retrieving key-value configurations from Azure App Configuration.
    /// These labels help distinguish different environments and services.
    /// </summary>
    public List<string> AppKeyValueLabels { get; set; } = [];

    /// <summary>
    /// Gets or sets a list of labels used for retrieving feature flags from Azure App Configuration.
    /// </summary>
    public List<string> AppFeatureFlagLabels { get; set; } = [];

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

    /// <summary>
    /// Adds default labels for retrieving configurations based on the provided environment and service name.
    /// </summary>
    /// <param name="environment">The environment name (e.g., "Development", "Staging", "Production").</param>
    /// <param name="serviceName">The name of the service using the configuration.</param>
    /// <returns>The updated <see cref="AltinnAuthorizationHostOptions"/> instance with the added labels.</returns>
    public AltinnAuthorizationHostOptions AddDefaultLabels(string environment, string serviceName)
    {
        if (string.IsNullOrWhiteSpace(environment) || string.IsNullOrWhiteSpace(serviceName))
        {
            return this;
        }

        AppKeyValueLabels.Add(environment.ToLowerInvariant());
        AppFeatureFlagLabels.Add(environment.ToLowerInvariant());

        AppKeyValueLabels.Add($"{environment}-{serviceName}".ToLowerInvariant());
        AppFeatureFlagLabels.Add($"{environment}-{serviceName}".ToLowerInvariant());

        return this;
    }
}
