namespace Altinn.Authorization.Integration.Platform.ResourceRegister;

/// <summary>
/// Configuration options for Altinn Resource Register integration.
/// </summary>
public class AltinnResourceRegisterOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AltinnResourceRegisterOptions"/> class.
    /// </summary>
    public AltinnResourceRegisterOptions()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AltinnResourceRegisterOptions"/> class and applies
    /// the provided configuration action to set up the options.
    /// </summary>
    /// <param name="configureOptions">A delegate to configure the <see cref="AltinnResourceRegisterOptions"/> instance.</param>
    public AltinnResourceRegisterOptions(Action<AltinnResourceRegisterOptions> configureOptions)
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
