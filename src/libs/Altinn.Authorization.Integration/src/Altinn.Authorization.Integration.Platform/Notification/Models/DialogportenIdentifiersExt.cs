using System.Text.Json.Serialization;

namespace Altinn.Authorization.Integration.Platform.Notification.Models;

/// <summary>
/// Represents unique identifiers for dialogs and transmissions within Dialogporten.
/// </summary>
public class DialogportenIdentifiersExt
{
    /// <summary>
    /// Gets or sets the identifier for a specific dialog within Dialogporten.
    /// </summary>
    [JsonPropertyName("dialogId")]
    public string? DialogId { get; set; }

    /// <summary>
    /// Gets or sets the identifier for a specific transmission within Dialogporten.
    /// </summary>
    [JsonPropertyName("transmissionId")]
    public string? TransmissionId { get; set; }
}
