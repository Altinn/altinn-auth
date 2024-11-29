using System.Diagnostics.CodeAnalysis;

namespace Altinn.Authorization.Configuration.AppSettings;

/// <summary>
/// Represents configuration settings for accessing Azure App Configuration.
/// Contains the endpoint URI used to connect to the Azure App Configuration service.
/// </summary>
[ExcludeFromCodeCoverage]
public class AppConfigurationSettings
{
    /// <summary>
    /// The URI of the Azure App Configuration service endpoint.
    /// This endpoint is used to retrieve configuration data from Azure App Configuration.
    /// </summary>
    public Uri Endpoint { get; set; }
}
