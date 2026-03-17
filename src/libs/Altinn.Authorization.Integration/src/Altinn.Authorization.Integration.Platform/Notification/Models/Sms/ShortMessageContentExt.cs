using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Altinn.Authorization.Integration.Platform.Notification.Models.Sms;

/// <summary>
/// Represents the content and sender information for an SMS (Short Message Service) message.
/// </summary>
public record ShortMessageContentExt
{
    /// <summary>
    /// The sender identifier displayed in the recipient's SMS message.
    /// </summary>
    [JsonPropertyName("sender")]
    public string? Sender { get; init; }

    /// <summary>
    /// The text content of the SMS message to be delivered to the recipient.
    /// </summary>
    [Required]
    [JsonPropertyName("body")]
    public required string Body { get; init; }
}
