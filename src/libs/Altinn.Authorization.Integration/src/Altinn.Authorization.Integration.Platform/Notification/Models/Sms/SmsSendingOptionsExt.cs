using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Altinn.Authorization.Integration.Platform.Notification.Models.Sms;

/// <summary>
/// Defines SMS configuration settings used in notification order requests.
/// </summary>
public class SmsSendingOptionsExt
{
    /// <summary>
    /// Gets or sets the sender identifier displayed in the recipient's SMS message.
    /// </summary>
    /// <remarks>
    /// Can be either a phone number or an alphanumeric sender identifier, subject to carrier and regional restrictions.
    /// </remarks>
    [JsonPropertyName("sender")]
    public string? Sender { get; set; }

    /// <summary>
    /// Gets or sets the text content of the SMS message.
    /// </summary>
    /// <remarks>
    /// Plain text content with length constraints determined by carrier limitations and character encoding.
    /// </remarks>
    [Required]
    [JsonPropertyName("body")]
    public required string Body { get; set; }

    /// <summary>
    /// Gets or sets the policy controlling when the SMS should be delivered.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="SendingTimePolicyExt.Daytime"/> to respect standard business hours (08:00-17:00 CET).
    /// </remarks>
    [JsonPropertyName("sendingTimePolicy")]
    [DefaultValue(SendingTimePolicyExt.Daytime)]
    public SendingTimePolicyExt SendingTimePolicy { get; set; } = SendingTimePolicyExt.Daytime;
}
