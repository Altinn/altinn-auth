namespace Altinn.Authorization.Integration.Platform.AccessManagement;

/// <summary>
/// Configuration options for the Altinn Access Management integration.
/// </summary>
public class AltinnAccessManagementOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AltinnAccessManagementOptions"/> class.
    /// </summary>
    public AltinnAccessManagementOptions()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AltinnAccessManagementOptions"/> class 
    /// and applies the provided configuration action to set up the options.
    /// </summary>
    /// <param name="configureOptions">
    /// A delegate that configures the <see cref="AltinnAccessManagementOptions"/> instance.
    /// </param>
    public AltinnAccessManagementOptions(Action<AltinnAccessManagementOptions> configureOptions)
    {
        configureOptions(this);
    }

    /// <summary>
    /// Gets or sets the endpoint URL for the Altinn Register service.
    /// </summary>
    /// <remarks>
    /// This URL is used for making requests to the Altinn Register API.
    /// </remarks>
    public Uri Endpoint { get; set; }
}
