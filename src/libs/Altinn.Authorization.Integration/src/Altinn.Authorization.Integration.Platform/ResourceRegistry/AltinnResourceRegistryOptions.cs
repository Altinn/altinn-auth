namespace Altinn.Authorization.Integration.Platform.ResourceRegistry;

/// <summary>
/// Configuration options for Altinn Resource Register integration.
/// </summary>
public class AltinnResourceRegistryOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AltinnResourceRegistryOptions"/> class.
    /// </summary>
    public AltinnResourceRegistryOptions()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AltinnResourceRegistryOptions"/> class and applies
    /// the provided configuration action to set up the options.
    /// </summary>
    /// <param name="configureOptions">A delegate to configure the <see cref="AltinnResourceRegistryOptions"/> instance.</param>
    public AltinnResourceRegistryOptions(Action<AltinnResourceRegistryOptions> configureOptions)
    {
        configureOptions(this);
    }

    /// <summary>
    /// Gets or sets the endpoint URL for the Altinn Resource Register service.
    /// </summary>
    /// <remarks>
    /// This URL is used to make requests to the Altinn Resource Register API.
    /// </remarks>
    public Uri Endpoint { get; set; }
}
