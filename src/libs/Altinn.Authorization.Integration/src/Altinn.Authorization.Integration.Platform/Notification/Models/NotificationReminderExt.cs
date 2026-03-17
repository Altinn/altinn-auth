using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Altinn.Authorization.Integration.Platform.Notification.Models;

/// <summary>
/// Represents a reminder notification that can be scheduled to follow an initial notification order.
/// </summary>
/// <remarks>
/// This class enables configuration of follow-up notifications that can be triggered based on
/// specific conditions or time delays after the initial notification. Each reminder can be
/// customized with its own recipient details and timing parameters.
/// </remarks>
public record NotificationReminderExt
{
    /// <summary>
    /// Gets or sets the sender's reference for this reminder.
    /// </summary>
    /// <remarks>
    /// A unique identifier used by the sender to correlate this reminder with their internal systems.
    /// </remarks>
    [JsonPropertyName("sendersReference")]
    public string? SendersReference { get; init; }

    /// <summary>
    /// Gets or sets the condition endpoint used to determine if the reminder should be sent.
    /// </summary>
    /// <remarks>
    /// When specified, the system will call this endpoint before sending the reminder.
    /// The reminder will only be sent if the endpoint returns a positive response.
    /// This allows for dynamic decision-making about whether the reminder is still relevant.
    /// </remarks>
    [JsonPropertyName("conditionEndpoint")]
    public Uri? ConditionEndpoint { get; init; }

    /// <summary>
    /// Gets or sets the number of days to delay this reminder.
    /// </summary>
    [DefaultValue(1)]
    [JsonPropertyName("delayDays")]
    public int? DelayDays { get; init; }

    /// <summary>
    /// Gets or sets the earliest date and time when the reminder should be delivered.
    /// </summary>
    /// <remarks>
    /// Allows scheduling reminder for future delivery. The system will not deliver the reminder
    /// before this time, but may deliver it later depending on system load and availability.
    /// Defaults to the current UTC time if not specified.
    /// </remarks>
    [JsonPropertyName("requestedSendTime")]
    public DateTime? RequestedSendTime { get; init; }

    /// <summary>
    /// Gets or sets the recipient information for this reminder.
    /// </summary>
    /// <remarks>
    /// Specifies the target recipient through one of the supported channels:
    /// email address, SMS number, national identity number, or organization number.
    /// The reminder can be directed to a different recipient than the initial notification.
    /// </remarks>
    [Required]
    [JsonPropertyName("recipient")]
    public required NotificationRecipientExt Recipient { get; init; }
}
