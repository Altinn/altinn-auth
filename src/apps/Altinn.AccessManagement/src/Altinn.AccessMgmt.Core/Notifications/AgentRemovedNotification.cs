using Altinn.AccessMgmt.Core.Outbox;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;

namespace Altinn.AccessMgmt.Core.Notifications;

/// <summary>
/// Provides helper methods for managing outbox notifications related to removing an agent.
/// </summary>
/// <remarks>
/// Encapsulates the logic for creating, scheduling, and cancelling outbox messages
/// handled by <c>agent_removed</c>.
/// </remarks>
public static class AgentRemovedNotification
{
    public const string Handler = "agent_removed";

    public const int DefaultNotifyInSeconds = 60 * 2;

    /// <summary>
    /// Creates or updates a pending outbox message for an agent removal notification.
    /// </summary>
    /// <remarks>
    /// This method performs an upsert operation for an outbox message identified by the
    /// combination of <paramref name="providerId"/> and <paramref name="agentId"/>.
    ///
    /// If a matching pending message already exists, its payload is left unchanged.
    /// If no matching message exists, a new one is created with a scheduled processing time
    /// based on <paramref name="notifyInSeconds"/>.
    /// </remarks>
    /// <param name="db">
    /// The <see cref="AppDbContext"/> used to access the outbox messages.
    /// </param>
    /// <param name="providerId">
    /// The identifier of the provider removing the agent.
    /// </param>
    /// <param name="agentId">
    /// The identifier of the agent being removed.
    /// </param>
    /// <param name="notifyInSeconds">
    /// The delay, in seconds, before the outbox message should be processed.
    /// Defaults to 120 seconds (2 minutes).
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to observe cancellation while querying the database.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous upsert operation.
    /// </returns>
    public static async Task Upsert(
        AppDbContext db,
        Guid providerId,
        Guid agentId,
        int notifyInSeconds = DefaultNotifyInSeconds,
        CancellationToken cancellationToken = default)
    {
        await db.OutboxMessages.UpsertOutboxAsync(
            refId: $"{Handler}_{providerId}_{agentId}",
            handler: Handler,
            updateValueFactory: (_, data) => data,
            addValueFactory: (msg) => AddValue(msg, notifyInSeconds, providerId, agentId),
            cancellationToken: cancellationToken
        );

        await AgentAddedNotification.Cancel(db, providerId, agentId, cancellationToken);
    }

    /// <summary>
    /// Cancels a pending agent removal notification by removing its outbox message.
    /// </summary>
    /// <remarks>
    /// This method attempts to locate a pending outbox message matching the specified
    /// <paramref name="provider"/> and <paramref name="agent"/> identifiers.
    ///
    /// If such a message exists, it is removed from the database.
    /// If no matching pending message is found, no action is taken.
    /// </remarks>
    /// <param name="db">
    /// The <see cref="AppDbContext"/> used to access the outbox messages.
    /// </param>
    /// <param name="provider">
    /// The identifier of the provider.
    /// </param>
    /// <param name="agent">
    /// The identifier of the agent.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to observe cancellation while querying the database.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// </returns>
    public static async Task Cancel(AppDbContext db, Guid provider, Guid agent, CancellationToken cancellationToken = default)
    {
        await db.OutboxMessages.CancelOutboxAsync(
            refId: $"{Handler}_{provider}_{agent}",
            handler: Handler,
            cancellationToken
        );
    }

    /// <summary>
    /// Creates the payload and schedules processing for a new agent removed notification.
    /// </summary>
    /// <param name="msg">
    /// The outbox message being initialized.
    /// </param>
    /// <param name="notifyInSeconds">
    /// The delay, in seconds, before the message should be processed.
    /// </param>
    /// <param name="provider">
    /// The identifier of the provider.
    /// </param>
    /// <param name="agent">
    /// The identifier of the agent.
    /// </param>
    /// <returns>
    /// A <see cref="AgentRemovedNotificationMessage"/> payload.
    /// </returns>
    private static AgentRemovedNotificationMessage AddValue(OutboxMessage msg, int notifyInSeconds, Guid provider, Guid agent)
    {
        var processAfter = DateTime.UtcNow.Add(TimeSpan.FromSeconds(notifyInSeconds));
        msg.Schedule = processAfter;
        msg.Timeout = TimeSpan.FromMinutes(1);

        return new()
        {
            ProviderId = provider,
            AgentId = agent,
        };
    }
}
