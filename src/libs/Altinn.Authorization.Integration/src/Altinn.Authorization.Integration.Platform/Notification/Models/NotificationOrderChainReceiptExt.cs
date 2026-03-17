using System.Text.Json.Serialization;

namespace Altinn.Authorization.Integration.Platform.Notification.Models;

/// <summary>
/// Represents the complete receipt for a notification order chain creation operation.
/// </summary>
/// <remarks>
/// This class extends <see cref="NotificationOrderChainShipmentExt"/> to include information about
/// any reminders that were configured as part of the notification chain. It serves as a comprehensive
/// confirmation that includes both the primary notification and all its associated reminders,
/// providing clients with tracking information for the entire notification sequence.
/// </remarks>
public class NotificationOrderChainReceiptExt : NotificationOrderChainShipmentExt
{
    /// <summary>
    /// Gets or sets the reminders associated with this notification order.
    /// </summary>
    /// <remarks>
    /// Contains tracking information for all reminder that were created
    /// as part of this notification chain. Each reminder entry provides identifiers that
    /// can be used to track or reference the specific reminder shipment.
    /// If the notification order was created without reminders, this property will be null.
    /// </remarks>
    [JsonPropertyName("reminders")]
    public List<NotificationOrderChainShipmentExt>? Reminders { get; set; }
}
