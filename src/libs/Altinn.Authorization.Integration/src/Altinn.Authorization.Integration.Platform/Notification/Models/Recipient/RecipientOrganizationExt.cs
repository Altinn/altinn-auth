using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Altinn.Authorization.Integration.Platform.Notification.Models.Recipient;

/// <summary>
/// Defines a model for sending notifications to an organization's contact person.
/// </summary>
/// <remarks>
/// This class enables notifications to be sent to organizations through their registered
/// contact information in the Norwegian Central Coordinating Register for Legal Entities (Enhetsregisteret).
/// </remarks>
public class RecipientOrganizationExt : RecipientBaseExt
{
    /// <summary>
    /// Gets or sets the organization number that identifies the recipient.
    /// </summary>
    /// <remarks>
    /// Used to identify the organization in the Norwegian Central Coordinating
    /// Register for Legal Entities (Enhetsregisteret) to retrieve their registered contact information.
    /// </remarks>
    [Required]
    [JsonPropertyName("orgNumber")]
    public required string OrgNumber { get; set; }

    /// <summary>
    /// Gets or sets the required channel scheme for delivering the notification.
    /// </summary>
    /// <remarks>
    /// Determines which communication channel(s) to use and their priority.
    /// Options include:
    /// <list type="bullet">
    /// <item><description><see cref="NotificationChannelExt.Email"/> - Use email only</description></item>
    /// <item><description><see cref="NotificationChannelExt.Sms"/> - Use SMS only</description></item>
    /// <item><description><see cref="NotificationChannelExt.EmailPreferred"/> - Try email first, fall back to SMS if email unavailable</description></item>
    /// <item><description><see cref="NotificationChannelExt.SmsPreferred"/> - Try SMS first, fall back to email if SMS unavailable</description></item>
    /// <item><description><see cref="NotificationChannelExt.EmailAndSms"/> - Use both email and SMS</description></item>
    /// </list>
    /// </remarks>
    [Required]
    [JsonPropertyName("channelSchema")]
    public override required NotificationChannelExt ChannelSchema { get; set; }
}
