namespace Altinn.Authorization.Integration.Platform.Notification.Models;

/// <summary>
/// Enum describing available notification channels.
/// </summary>
public enum NotificationChannelExt
{
    /// <summary>
    /// The selected channel for the notification is email.
    /// </summary>
    Email,

    /// <summary>
    /// The selected channel for the notification is sms.
    /// </summary>
    Sms,

    /// <summary>
    /// The selected channel for the notification is email preferred. 
    /// </summary>
    /// <remarks>
    /// Notification should primarily be sent through email, and SMS should be used if email is not available.
    /// </remarks>
    EmailPreferred,

    /// <summary>
    /// The selected channel for the notification is SMS preferred. 
    /// </summary>
    /// <remarks>
    /// Notification should primarily be sent through SMS, and email should be used if email is not available.
    /// </remarks>
    SmsPreferred,

    /// <summary>
    /// The selected channel for the notification is both email and SMS.
    /// </summary>
    /// <remarks>
    /// Notification will be sent through both email and SMS channels simultaneously, regardless of recipient's preferred contact method.
    /// </remarks>
    EmailAndSms
}
