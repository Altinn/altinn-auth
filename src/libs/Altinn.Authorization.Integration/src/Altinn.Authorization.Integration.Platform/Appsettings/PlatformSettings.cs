namespace Altinn.Authorization.Integration.Platform.Appsettings;

/// <summary>
/// Represents the platform-related configuration settings for Altinn authorization.
/// This class provides endpoints for interacting with key platform services.
/// </summary>
public class PlatformSettings
{
    /// <summary>
    /// Gets or sets the endpoint URI for the Resource Register service.
    /// This service provides metadata about resources in the Altinn ecosystem.
    /// </summary>
    /// <remarks>
    /// The endpoint should be a valid URI, typically pointing to an API service.
    /// Example: <c>https://resource-register.altinn.no</c>
    /// </remarks>
    public Uri ResourceRegisterEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the endpoint URI for the Register service.
    /// This service is used for managing organizational and individual registry data within Altinn.
    /// </summary>
    /// <remarks>
    /// The endpoint should be a valid URI, typically pointing to an API service.
    /// Example: <c>https://register.altinn.no</c>
    /// </remarks>
    public Uri RegisterEndpoint { get; set; }
}
