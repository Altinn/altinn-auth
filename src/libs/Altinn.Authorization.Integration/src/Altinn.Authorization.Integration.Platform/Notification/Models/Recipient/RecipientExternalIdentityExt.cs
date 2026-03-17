using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Altinn.Authorization.Integration.Platform.Notification.Models.Recipient;

/// <summary>
/// Represents a notification recipient identified by an external identity.
/// </summary>
public class RecipientExternalIdentityExt : RecipientBaseExt
{
    /// <summary>
    /// The external identity of the recipient in URN format.
    /// </summary>
    [Required]
    [JsonPropertyName("externalIdentity")]
    public required string ExternalIdentity { get; set; }

    /// <summary>
    /// The channel scheme for delivering the notification.
    /// </summary>
    /// <seealso cref="NotificationChannelExt"/>
    [Required]
    [JsonPropertyName("channelSchema")]
    [DefaultValue(NotificationChannelExt.Email)]
    public override required NotificationChannelExt ChannelSchema { get; set; } = NotificationChannelExt.Email;
}
