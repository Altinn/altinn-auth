using System.Text.Json.Serialization;

namespace Altinn.Authorization.Integration.Platform.Notification.Models;

/// <summary>
/// Defines common scheduling and conditional execution parameters for notification requests.
/// </summary>
/// <remarks>
/// This base class provides fundamental parameters that control when and under what conditions
/// notifications should be delivered. It serves as a foundation for more specialized notification request types.
/// </remarks>
public class NotificationOrderBaseExt
{
    /// <summary>
    /// Gets or sets the sender's reference identifier.
    /// </summary>
    /// <remarks>
    /// An optional identifier used to correlate the notification with records in the sender's system.
    /// </remarks>
    [JsonPropertyName("sendersReference")]
    public string? SendersReference { get; set; }

    /// <summary>
    /// Gets or sets the earliest date and time when the notification should be delivered.
    /// </summary>
    /// <remarks>
    /// Allows scheduling notifications for future delivery. The system will not deliver the notification
    /// before this time, but may deliver it later depending on system load and availability.
    /// Defaults to the current UTC time if not specified.
    /// </remarks>
    [JsonPropertyName("requestedSendTime")]
    public DateTime? RequestedSendTime { get; set; }

    /// <summary>
    /// Gets or sets a URI endpoint that can determine whether the notification should be sent.
    /// </summary>
    /// <remarks>
    /// When specified, the system will call this endpoint before sending the notification.
    /// The notification will only be sent if the endpoint returns a positive response.
    /// This enables conditional delivery based on external business rules or state.
    /// </remarks>
    [JsonPropertyName("conditionEndpoint")]
    public Uri? ConditionEndpoint { get; set; }
}
