namespace Altinn.Authorization.Integration.Platform.Notification;

/// <summary>
/// Options for configuring the Altinn Notification integration.
/// </summary>
public class AltinnNotificationOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AltinnNotificationOptions"/> class.
    /// </summary>
    public AltinnNotificationOptions()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AltinnNotificationOptions"/> class
    /// and applies the specified configuration action.
    /// </summary>
    /// <param name="configureOptions">
    /// An action used to configure the <see cref="AltinnNotificationOptions"/> instance.
    /// </param>
    public AltinnNotificationOptions(Action<AltinnNotificationOptions> configureOptions)
    {
        configureOptions(this);
    }

    /// <summary>
    /// Gets or sets the base endpoint for the Altinn Notification API.
    /// </summary>
    /// <remarks>
    /// This URI represents the base address of the Altinn Notification service
    /// and is used when constructing API requests.
    /// </remarks>
    public Uri Endpoint { get; set; }
}
