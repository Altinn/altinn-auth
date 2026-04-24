using Altinn.AccessMgmt.Core.Notifications;

namespace Altinn.AccessMgmt.Core.Appsettings;

public class CoreAppsettings
{
    public CoreAppsettings()
    {
    }

    public CoreAppsettings(Action<CoreAppsettings> configureAppsettings)
    {
        if (configureAppsettings is { })
        {
            configureAppsettings(this);
        }
    }

    public RequestOptions Request { get; set; } = new();

    public NotificationsOptions Notifications { get; set; } = new();

    /// <summary>
    /// Configuration options for various notification delays.
    /// </summary>
    public class NotificationsOptions
    {
        /// <summary>
        /// Gets or sets the delay in seconds before notifying when a request is reviewed.
        /// </summary>
        /// <seealso cref="RequestReviewedNotification"/>
        public int RequestReviewedNotifyInSeconds { get; set; } = RequestReviewedNotification.DefaultNotifyInSeconds;

        /// <summary>
        /// Gets or sets the delay in seconds before notifying when a request is pending.
        /// </summary>
        /// <seealso cref="RequestPendingNotification"/>
        public int RequestPendingNotifyInSeconds { get; set; } = RequestPendingNotification.DefaultNotifyInSeconds;

        /// <summary>
        /// Gets or sets the delay in seconds before notifying when a rightholder is added.
        /// </summary>
        /// <seealso cref="RightholderAddedNotification"/>
        public int RightholderAddedNotifyInSeconds { get; set; } = RightholderAddedNotification.DefaultNotifyInSeconds;

        /// <summary>
        /// Gets or sets the delay in seconds before notifying when a rightholder is removed.
        /// </summary>
        /// <seealso cref="RightholderRemovedNotification"/>
        public int RightholderRemovedNotifyInSeconds { get; set; } = RightholderRemovedNotification.DefaultNotifyInSeconds;

        /// <summary>
        /// Gets or sets the delay in seconds before notifying when access is added.
        /// </summary>
        /// <seealso cref="AccessAddedNotification"/>
        public int AccessAddedNotifyInSeconds { get; set; } = AccessAddedNotification.DefaultNotifyInSeconds;

        /// <summary>
        /// Gets or sets the delay in seconds before notifying when access is removed.
        /// </summary>
        /// <seealso cref="AccessRemovedNotification"/>
        public int AccessRemovedNotifyInSeconds { get; set; } = AccessRemovedNotification.DefaultNotifyInSeconds;

        /// <summary>
        /// Gets or sets the delay in seconds before notifying when an agent is removed.
        /// </summary>
        /// <seealso cref="AgentRemovedNotification"/>
        public int AgentRemovedNotifyInSeconds { get; set; } = AgentRemovedNotification.DefaultNotifyInSeconds;

        /// <summary>
        /// Gets or sets the delay in seconds before notifying when an agent is added.
        /// </summary>
        /// <seealso cref="AgentAddedNotification"/>
        public int AgentAddedNotifyInSeconds { get; set; } = AgentAddedNotification.DefaultNotifyInSeconds;

        /// <summary>
        /// Gets or sets the delay in seconds before notifying when a client is added.
        /// </summary>
        /// <seealso cref="ClientAddedNotification"/>
        public int ClientAddedNotifyInSeconds { get; set; } = ClientAddedNotification.DefaultNotifyInSeconds;

        /// <summary>
        /// Gets or sets the delay in seconds before notifying when a client is removed.
        /// </summary>
        /// <seealso cref="ClientRemovedNotification"/>
        public int ClientRemovedNotifyInSeconds { get; set; } = ClientRemovedNotification.DefaultNotifyInSeconds;
    }

    /// <summary>
    /// Configuration options for request notifications.
    /// </summary>
    [Obsolete("will be substituted with " + nameof(NotificationsOptions))]
    public class RequestOptions
    {
        /// <summary>
        /// Gets or sets the delay in seconds before notifying when a request is approved.
        /// </summary>
        public int NotifyRequestApprovedInSeconds { get; set; } = 60 * 15;

        /// <summary>
        /// Gets or sets the delay in seconds before notifying when a request is pending.
        /// </summary>
        public int NotifyRequestPendingInSeconds { get; set; } = 60 * 15;
    }
}
