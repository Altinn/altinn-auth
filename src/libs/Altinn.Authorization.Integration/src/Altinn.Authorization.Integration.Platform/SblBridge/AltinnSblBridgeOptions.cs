namespace Altinn.Authorization.Integration.Platform.SblBridge;

/// <summary>
/// Configuration options for Altinn Sbl Bridge Options.
/// </summary>
public class AltinnSblBridgeOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AltinnSblBridgeOptions"/> class.
    /// </summary>
    public AltinnSblBridgeOptions()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AltinnSblBridgeOptions"/> class and applies
    /// the provided configuration action to set up the options.
    /// </summary>
    /// <param name="configureOptions">A delegate to configure the <see cref="AltinnSblBridgeOptions"/> instance.</param>
    public AltinnSblBridgeOptions(Action<AltinnSblBridgeOptions> configureOptions)
    {
        configureOptions(this);
    }

    /// <summary>
    /// Gets or sets the endpoint URL for the Altinn SBL Bridge Options.
    /// </summary>
    /// <remarks>
    /// This URL is used to make requests to Altinn SBL.
    /// </remarks>
    public Uri Endpoint { get; set; }
}
