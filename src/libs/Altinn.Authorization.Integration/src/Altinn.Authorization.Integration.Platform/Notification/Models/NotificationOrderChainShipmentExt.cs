using System.Text.Json.Serialization;

namespace Altinn.Authorization.Integration.Platform.Notification.Models;

/// <summary>
/// Represents the base information for a shipment in a notification order chain.
/// </summary>
/// <remarks>
/// This class serves as the foundation for tracking notification deliveries, providing
/// essential identifiers to correlate shipments across systems. It is used both for
/// primary notifications and their associated reminders in notification chains.
/// </remarks>
public class NotificationOrderChainShipmentExt
{
    /// <summary>
    /// Gets or sets the unique identifier for this shipment.
    /// </summary>
    [JsonPropertyName("shipmentId")]
    public required Guid ShipmentId { get; set; }

    /// <summary>
    /// Gets or sets the sender's reference identifier.
    /// </summary>
    /// <remarks>
    /// An optional identifier provided by the sender to correlate this notification shipment with records in their system.
    /// </remarks>
    [JsonPropertyName("sendersReference")]
    public string? SendersReference { get; set; }
}
