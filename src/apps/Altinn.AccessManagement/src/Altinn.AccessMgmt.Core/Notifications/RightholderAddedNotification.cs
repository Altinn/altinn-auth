using Altinn.AccessMgmt.Core.Outbox;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;

namespace Altinn.AccessMgmt.Core.Notifications;

/// <summary>
/// Provides helper methods for managing outbox notifications related to adding a rightholder.
/// </summary>
/// <remarks>
/// Encapsulates the logic for creating, scheduling, and cancelling outbox messages
/// handled by <c>rightholder_added</c>.
/// </remarks>
public static class RightholderAddedNotification
{
    public const string Handler = "rightholder_added";

    /// <summary>
    /// Creates or updates a pending outbox message for a rightholder addition notification.
    /// </summary>
    /// <remarks>
    /// This method performs an upsert operation for an outbox message identified by the
    /// combination of <paramref name="from"/> and <paramref name="to"/>.
    ///
    /// If a matching pending message already exists, its payload is left unchanged.
    /// If no matching message exists, a new one is created with a scheduled processing time
    /// based on <paramref name="notifyAddRightholderPendingInSeconds"/>.
    /// </remarks>
    /// <param name="db">
    /// The <see cref="AppDbContext"/> used to access the outbox messages.
    /// </param>
    /// <param name="from">
    /// The identifier of the entity granting rights.
    /// </param>
    /// <param name="to">
    /// The identifier of the entity receiving rights.
    /// </param>
    /// <param name="notifyAddRightholderPendingInSeconds">
    /// The delay, in seconds, before the outbox message should be processed.
    /// Defaults to 120 seconds.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to observe cancellation while querying the database.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous upsert operation.
    /// </returns>
    public static async Task Upsert(
        AppDbContext db,
        Guid from,
        Guid to,
        int notifyAddRightholderPendingInSeconds = 120,
        CancellationToken cancellationToken = default)
    {
        await db.OutboxMessages.UpsertOutboxAsync(
            refId: $"{Handler}_{from}_{to}",
            handler: Handler,
            updateValueFactory: (_, data) => data,
            addValueFactory: (msg) => AddValue(msg, notifyAddRightholderPendingInSeconds, from, to),
            cancellationToken: cancellationToken
        );
    }

    /// <summary>
    /// Cancels a pending rightholder addition notification by removing its outbox message.
    /// </summary>
    /// <remarks>
    /// This method attempts to locate a pending outbox message matching the specified
    /// <paramref name="from"/> and <paramref name="to"/> identifiers.
    ///
    /// If such a message exists, it is removed from the database.
    /// If no matching pending message is found, no action is taken.
    /// </remarks>
    /// <param name="db">
    /// The <see cref="AppDbContext"/> used to access the outbox messages.
    /// </param>
    /// <param name="from">
    /// The identifier of the entity granting rights.
    /// </param>
    /// <param name="to">
    /// The identifier of the entity receiving rights.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to observe cancellation while querying the database.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// </returns>
    public static async Task Cancel(AppDbContext db, Guid from, Guid to, CancellationToken cancellationToken = default)
    {
        await db.OutboxMessages.CancelOutboxAsync(
            refId: $"{Handler}_{from}_{to}",
            handler: Handler,
            cancellationToken
        );
    }

    /// <summary>
    /// Creates the payload and schedules processing for a new rightholder added notification.
    /// </summary>
    /// <param name="msg">
    /// The outbox message being initialized.
    /// </param>
    /// <param name="notifyAddRightholderPendingInSeconds">
    /// The delay, in seconds, before the message should be processed.
    /// </param>
    /// <param name="from">
    /// The identifier of the entity granting rights.
    /// </param>
    /// <param name="to">
    /// The identifier of the entity receiving rights.
    /// </param>
    /// <returns>
    /// A <see cref="RightholderAddedNotificationMessage"/> payload.
    /// </returns>
    private static RightholderAddedNotificationMessage AddValue(OutboxMessage msg, int notifyAddRightholderPendingInSeconds, Guid from, Guid to)
    {
        var processAfter = DateTime.UtcNow.Add(TimeSpan.FromSeconds(notifyAddRightholderPendingInSeconds));
        msg.Schedule = processAfter;
        msg.Timeout = TimeSpan.FromMinutes(1);
        
        return new()
        {
            From = from,
            To = to,
        };
    }
}
