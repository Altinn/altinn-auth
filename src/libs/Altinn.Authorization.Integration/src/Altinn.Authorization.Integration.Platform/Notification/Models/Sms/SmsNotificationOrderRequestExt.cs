using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Altinn.Authorization.Integration.Platform.Notification.Models.Sms;

/// <summary>
/// Class representing an SMS notification order request.
/// </summary>
/// <remarks>
/// External representation to be used in the API.
/// </remarks>
public class SmsNotificationOrderRequestExt : NotificationOrderRequestBaseExt
{
    /// <summary>
    /// Gets or sets the sender number of the SMS.
    /// </summary>
    [JsonPropertyName("senderNumber")]
    public string? SenderNumber { get; set; }

    /// <summary>
    /// Gets or sets the body of the SMS.
    /// </summary>
    [JsonPropertyName("body")]
    [Required]
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Serializes the <see cref="SmsNotificationOrderRequestExt"/> to JSON.
    /// </summary>
    /// <returns>A JSON string representation of the object.</returns>
    public string Serialize()
    {
        return JsonSerializer.Serialize(this);
    }
}
