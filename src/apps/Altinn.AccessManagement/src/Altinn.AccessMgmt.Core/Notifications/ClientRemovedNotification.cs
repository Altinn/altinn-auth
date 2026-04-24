using System.Diagnostics;
using Altinn.AccessMgmt.Core.Outbox;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;

namespace Altinn.AccessMgmt.Core.Notifications;

/// <summary>
/// Provides helper methods for managing outbox notifications related to removing an agent from a client.
/// </summary>
/// <remarks>
/// Encapsulates the logic for creating, scheduling, and cancelling outbox messages
/// handled by <c>client_removed</c>.
/// </remarks>
public static class ClientRemovedNotification
{
    public const string Handler = "client_removed";

    public const int DefaultNotifyInSeconds = 60 * 10;

    /// <summary>
    /// Creates or updates a pending outbox message for an agent removed from client notification.
    /// </summary>
    /// <remarks>
    /// This method performs an upsert operation for an outbox message identified by the
    /// combination of <paramref name="providerId"/>, <paramref name="agentId"/>, and <paramref name="clientId"/>.
    ///
    /// If a matching pending message already exists, it is updated with the new client ID.
    /// If no matching message exists, a new one is created with a scheduled processing time
    /// based on <paramref name="notifyInSeconds"/>.
    /// </remarks>
    /// <param name="db">
    /// The <see cref="AppDbContext"/> used to access the outbox messages.
    /// </param>
    /// <param name="providerId">
    /// The identifier of the provider removing the agent from the client.
    /// </param>
    /// <param name="clientId">
    /// The identifier of the client being removed.
    /// </param>
    /// <param name="agentId">
    /// The identifier of the agent being removed from the client.
    /// </param>
    /// <param name="notifyInSeconds">
    /// The delay, in seconds, before the outbox message should be processed.
    /// Defaults to 900 seconds (15 minutes).
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
        Guid clientId,
        Guid agentId,
        int notifyInSeconds = DefaultNotifyInSeconds,
        CancellationToken cancellationToken = default)
    {
        await db.OutboxMessages.UpsertOutboxAsync(
            refId: $"{Handler}_{providerId}_{agentId}_{clientId}",
            handler: Handler,
            updateValueFactory: (msg, data) => UpdateValue(providerId, clientId, agentId, notifyInSeconds, msg, data),
            addValueFactory: (msg) => AddValue(msg, providerId, clientId, agentId, notifyInSeconds),
            cancellationToken: cancellationToken
        );
    }

    /// <summary>
    /// Cancels a pending agent removed from client notification by removing its outbox message.
    /// </summary>
    /// <remarks>
    /// This method attempts to locate a pending outbox message matching the specified
    /// <paramref name="providerId"/>, <paramref name="clientId"/>, and <paramref name="agentId"/> identifiers.
    ///
    /// If such a message exists, it is removed from the database.
    /// If no matching pending message is found, no action is taken.
    /// </remarks>
    /// <param name="db">
    /// The <see cref="AppDbContext"/> used to access the outbox messages.
    /// </param>
    /// <param name="providerId">
    /// The identifier of the provider.
    /// </param>
    /// <param name="clientId">
    /// The identifier of the client.
    /// </param>
    /// <param name="agentId">
    /// The identifier of the agent.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to observe cancellation while querying the database.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// </returns>
    public static async Task Cancel(AppDbContext db, Guid providerId, Guid clientId, Guid agentId, CancellationToken cancellationToken = default)
    {
        await db.OutboxMessages.CancelOutboxAsync(
            refId: $"{Handler}_{providerId}_{agentId}_{clientId}",
            handler: Handler,
            cancellationToken
        );
    }

    /// <summary>
    /// Creates the payload and schedules processing for a new client removed notification.
    /// </summary>
    /// <param name="msg">
    /// The outbox message being initialized.
    /// </param>
    /// <param name="providerId">
    /// The identifier of the provider.
    /// </param>
    /// <param name="clientId">
    /// The identifier of the client.
    /// </param>
    /// <param name="agentId">
    /// The identifier of the agent.
    /// </param>
    /// <param name="notifyInSeconds">
    /// The delay, in seconds, before the message should be processed.
    /// </param>
    /// <returns>
    /// A <see cref="ClientRemovedNotificationMessage"/> payload.
    /// </returns>
    private static ClientRemovedNotificationMessage AddValue(OutboxMessage msg, Guid providerId, Guid clientId, Guid agentId, int notifyInSeconds)
    {
        var processAfter = DateTime.UtcNow.Add(TimeSpan.FromSeconds(notifyInSeconds));
        msg.Schedule = processAfter;
        msg.Timeout = TimeSpan.FromMinutes(1);

        return new()
        {
            ProviderId = providerId,
            AgentId = agentId,
            Clients = [clientId],
        };
    }

    private static ClientRemovedNotificationMessage UpdateValue(
        Guid providerId,
        Guid clientId,
        Guid agentId,
        int notifyInSeconds,
        OutboxMessage msg,
        ClientRemovedNotificationMessage data
    )
    {
        if (data is null)
        {
            Activity.Current?.AddTag(nameof(ClientRemovedNotificationMessage), $"Current outbox message {nameof(ClientRemovedNotificationMessage)} is null? Creating new object.");
            return AddValue(msg, providerId, clientId, agentId, notifyInSeconds);
        }

        data.Clients ??= [];

        if (!data.Clients.Contains(clientId))
        {
            data.Clients ??= [];
            data.Clients.Add(clientId);
        }

        data.Updated++;

        var processAfter = TimeSpan.FromSeconds(notifyInSeconds);
        var now = DateTime.UtcNow;
        var candidate = now.Add(processAfter / (data.Updated + 1));
        msg.Schedule = candidate < msg.Schedule ? msg.Schedule : candidate;

        return data;
    }
}
