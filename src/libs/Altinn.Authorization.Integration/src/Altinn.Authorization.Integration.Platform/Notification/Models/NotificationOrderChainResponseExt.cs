using System.Text.Json.Serialization;

namespace Altinn.Authorization.Integration.Platform.Notification.Models;

/// <summary>
/// Represents the response returned after successfully creating a notification order chain.
/// </summary>
/// <remarks>
/// This class encapsulates the confirmation details provided to clients upon successful creation 
/// of a notification order with optional reminders. It contains both the unique identifier for the 
/// notification order chain itself and a detailed receipt with tracking information for each component of the notification chain.
/// </remarks>
public class NotificationOrderChainResponseExt
{
    /// <summary>
    /// Gets or sets the unique identifier for the notification order chain.
    /// </summary>
    /// <remarks>
    /// This identifier can be used to reference the entire notification order chain in subsequent operations
    /// or for tracking purposes.
    /// </remarks>
    [JsonPropertyName("notificationOrderId")]
    public required Guid OrderChainId { get; set; }

    /// <summary>
    /// Gets or sets the detailed receipt for the notification order creation.
    /// </summary>
    /// <remarks>
    /// Contains information about the created notification orders and reminders.
    /// </remarks>
    [JsonPropertyName("notification")]
    public required NotificationOrderChainReceiptExt OrderChainReceipt { get; set; }
}
