namespace Altinn.Authorization.Integration.Platform.Notification.Models;

/// <summary>
/// Defines policies that govern when a notification message is scheduled for delivery.
/// </summary>
public enum SendingTimePolicyExt : uint
{
    /// <summary>
    /// Allows message delivery at any time of day.
    /// </summary>
    Anytime = 1,

    /// <summary>
    /// Restricts message delivery to business hours (08:00-17:00 CET).
    /// </summary>
    Daytime = 2
}
