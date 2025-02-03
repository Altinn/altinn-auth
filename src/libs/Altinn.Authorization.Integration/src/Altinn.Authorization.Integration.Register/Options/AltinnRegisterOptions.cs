namespace Altinn.Authorization.Integration.Register.Options;

/// <summary>
/// Configuration options for Altinn Register integration.
/// </summary>
public class AltinnRegisterOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AltinnRegisterOptions"/> class.
    /// </summary>
    public AltinnRegisterOptions()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AltinnRegisterOptions"/> class and applies
    /// the provided configuration action to set up the options.
    /// </summary>
    /// <param name="configureOptions">A delegate to configure the <see cref="AltinnRegisterOptions"/> instance.</param>
    public AltinnRegisterOptions(Action<AltinnRegisterOptions> configureOptions)
    {
        configureOptions(this);
    }

    /// <summary>
    /// Gets or sets the endpoint URL for the Altinn Register service.
    /// </summary>
    /// <remarks>
    /// This URL is used to make requests to the Altinn Register API.
    /// </remarks>
    public Uri Endpoint { get; set; }
}
