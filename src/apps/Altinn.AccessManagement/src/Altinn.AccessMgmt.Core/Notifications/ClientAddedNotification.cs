using System.Diagnostics;
using Altinn.AccessMgmt.Core.Outbox;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;

namespace Altinn.AccessMgmt.Core.Notifications;

/// <summary>
/// Provides helper methods for managing outbox notifications related to adding an agent for a client.
/// </summary>
/// <remarks>
/// Encapsulates the logic for creating, scheduling, and cancelling outbox messages
/// handled by <c>client_added</c>.
/// </remarks>
public static class ClientAddedNotification
{
    public const string Handler = "client_added";

    public const int DefaultNotifyInSeconds = 60 * 15;

    /// <summary>
    /// Cancels a pending agent added to client notification by removing its outbox message.
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
    /// Creates or updates a pending outbox message for an agent added to client notification.
    /// </summary>
    /// <remarks>
    /// This method performs an upsert operation for an outbox message identified by the
    /// combination of <paramref name="providerId"/> and <paramref name="agentId"/>.
    ///
    /// If a matching pending message already exists, it is updated with the new client ID.
    /// If no matching message exists, a new one is created with a scheduled processing time
    /// based on <paramref name="notifyInSeconds"/>.
    /// </remarks>
    /// <param name="db">
    /// The <see cref="AppDbContext"/> used to access the outbox messages.
    /// </param>
    /// <param name="providerId">
    /// The identifier of the provider granting agent rights for the client.
    /// </param>
    /// <param name="clientId">
    /// The identifier of the client being added.
    /// </param>
    /// <param name="agentId">
    /// The identifier of the agent being added to the client.
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
            refId: $"{Handler}_{providerId}_{agentId}",
            handler: Handler,
            updateValueFactory: (msg, data) => UpdateValue(providerId, clientId, agentId, msg, data, notifyInSeconds),
            addValueFactory: (msg) => AddValue(msg, providerId, clientId, agentId, notifyInSeconds),
            cancellationToken: cancellationToken
        );
    }

    /// <summary>
    /// Creates the payload and schedules processing for a new client added notification.
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
    /// A <see cref="ClientAddedNotificationMessage"/> payload.
    /// </returns>
    private static ClientAddedNotificationMessage AddValue(OutboxMessage msg, Guid providerId, Guid clientId, Guid agentId, int notifyInSeconds)
    {
        var processAfter = DateTime.UtcNow.Add(TimeSpan.FromSeconds(notifyInSeconds));
        msg.Schedule = processAfter;
        msg.Timeout = TimeSpan.FromMinutes(1);

        return new()
        {
            ProviderId = providerId,
            AgentId = agentId,
            Clients = [clientId]
        };
    }

    private static ClientAddedNotificationMessage UpdateValue(
        Guid providerId,
        Guid clientId,
        Guid agentId,
        OutboxMessage msg,
        ClientAddedNotificationMessage data,
        int notifyInSeconds = DefaultNotifyInSeconds
    )
    {
        if (data is null)
        {
            Activity.Current?.AddTag(nameof(ClientAddedNotificationMessage), $"Current outbox message {nameof(ClientAddedNotificationMessage)} is null? Creating new object.");
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
