using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Altinn.Authorization.Integration.Platform.Notification.Models.Email;

/// <summary>
/// Defines email configuration settings used in notification order requests.
/// </summary>
public class EmailSendingOptionsExt
{
    /// <summary>
    /// Gets or sets the sender's email address.
    /// </summary>
    /// <remarks>
    /// This value determines which address will appear as the sender in the recipient's mailbox.
    /// </remarks>
    [JsonPropertyName("senderEmailAddress")]
    public string? SenderEmailAddress { get; set; }

    /// <summary>
    /// Gets or sets the subject line of the email.
    /// </summary>
    /// <remarks>
    /// Displayed as the email headline in the recipient's inbox.
    /// </remarks>
    [Required]
    [JsonPropertyName("subject")]
    public required string Subject { get; set; }

    /// <summary>
    /// Gets or sets the main body content of the email.
    /// </summary>
    /// <remarks>
    /// Contains the primary message content to be delivered to the recipient.
    /// May include plain text or HTML markup depending on the <see cref="ContentType"/> setting.
    /// </remarks>
    [Required]
    [JsonPropertyName("body")]
    public required string Body { get; set; }

    /// <summary>
    /// Gets or sets the content type (plain text or HTML) of the email.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="EmailContentTypeExt.Plain"/>.
    /// Determines how email clients will render the body content.
    /// </remarks>
    [JsonPropertyName("contentType")]
    [DefaultValue(EmailContentTypeExt.Plain)]
    public EmailContentTypeExt ContentType { get; set; } = EmailContentTypeExt.Plain;

    /// <summary>
    /// Gets or sets the policy defining when the email should be sent.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="SendingTimePolicyExt.Anytime"/> allowing delivery at any time.
    /// </remarks>
    [JsonPropertyName("sendingTimePolicy")]
    [DefaultValue(SendingTimePolicyExt.Anytime)]
    public SendingTimePolicyExt SendingTimePolicy { get; set; } = SendingTimePolicyExt.Anytime;
}
