using System.Diagnostics;
using Altinn.AccessMgmt.Core.Outbox;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;

namespace Altinn.AccessMgmt.Core.Notifications;

/// <summary>
/// Provides helper methods for managing outbox notifications related to reviewed access requests.
/// </summary>
/// <remarks>
/// Encapsulates the logic for creating, scheduling, and cancelling outbox messages
/// handled by <c>request_reviewed</c>.
/// </remarks>
public static class RequestReviewedNotification
{
    public const string Handler = "request_reviewed";

    public const int DefaultNotifyInSeconds = 60 * 10;

    /// <summary>
    /// Creates or updates a pending outbox message for a request review notification.
    /// </summary>
    /// <remarks>
    /// This method performs an upsert operation for an outbox message identified by the
    /// combination of <paramref name="requesterId"/> and <paramref name="recipientId"/>.
    ///
    /// If a matching pending message already exists, it is updated with the new resource or package and approval status.
    /// If no matching message exists, a new one is created with a scheduled processing time
    /// based on <paramref name="notifyInSeconds"/>.
    /// </remarks>
    /// <param name="db">
    /// The <see cref="AppDbContext"/> used to access the outbox messages.
    /// </param>
    /// <param name="requesterId">
    /// The identifier of the entity that made the original request.
    /// </param>
    /// <param name="recipientId">
    /// The identifier of the entity receiving the review notification (the requester).
    /// </param>
    /// <param name="resourceId">
    /// Optional identifier of the resource being reviewed.
    /// </param>
    /// <param name="packageId">
    /// Optional identifier of the package being reviewed.
    /// </param>
    /// <param name="isApproved">
    /// Indicates whether the request was approved or denied.
    /// </param>
    /// <param name="notifyInSeconds">
    /// The delay, in seconds, before the outbox message should be processed.
    /// Defaults to 600 seconds (10 minutes).
    /// </param>
    /// <param name="ct">
    /// A token used to observe cancellation while querying the database.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous upsert operation.
    /// </returns>
    public static async Task Upsert(
        AppDbContext db,
        Guid requesterId,
        Guid recipientId,
        Guid? resourceId,
        Guid? packageId,
        bool isApproved,
        int notifyInSeconds = DefaultNotifyInSeconds,
        CancellationToken ct = default)
    {
        await db.OutboxMessages.UpsertOutboxAsync(
            refId: $"{Handler}_{requesterId}_{recipientId}",
            handler: Handler,
            addValueFactory: msg => AddValue(requesterId, recipientId, resourceId, packageId, isApproved, msg, notifyInSeconds),
            updateValueFactory: (msg, data) => UpdateValue(requesterId, recipientId, resourceId, packageId, isApproved, msg, data, notifyInSeconds),
            cancellationToken: ct
        );
    }

    /// <summary>
    /// Cancels a pending request review notification by removing its outbox message.
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
    /// The identifier of the reviewer.
    /// </param>
    /// <param name="to">
    /// The identifier of the recipient (the original requester).
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

    private static RequestReviewNotificationMessage AddValue(
        Guid reviewerId,
        Guid recipientId,
        Guid? resourceId,
        Guid? packageId,
        bool isApproved,
        OutboxMessage msg,
        int notifyInSeconds
    )
    {
        var processAfter = TimeSpan.FromSeconds(notifyInSeconds);
        var now = DateTime.UtcNow;
        var schedule = now.Add(processAfter);
        msg.Schedule = schedule;
        msg.Timeout = TimeSpan.FromMinutes(1);

        return new RequestReviewNotificationMessage()
        {
            RecipientId = recipientId,
            ReviewerId = reviewerId,
            Resources = resourceId.HasValue && resourceId.Value != Guid.Empty ? [new() { Ref = resourceId.Value, IsApproved = isApproved }] : [],
            Packages = packageId.HasValue && packageId.Value != Guid.Empty ? [new() { Ref = packageId.Value, IsApproved = isApproved }] : [],
            Updated = 0,
        };
    }

    private static RequestReviewNotificationMessage UpdateValue(
        Guid reviewerId,
        Guid recipientId,
        Guid? resourceId,
        Guid? packageId,
        bool isApproved,
        OutboxMessage msg,
        RequestReviewNotificationMessage data,
        int notifyInSeconds
    )
    {
        if (data is null)
        {
            Activity.Current?.AddTag(nameof(RequestReviewedNotification), $"Current outbox message {nameof(RequestReviewNotificationMessage)} is null? Creating new object.");
            return AddValue(reviewerId, recipientId, resourceId, packageId, isApproved, msg, notifyInSeconds);
        }

        data.Updated++;

        if (resourceId.HasValue)
        {
            var list = data.Resources ?? [];
            var existing = list.FirstOrDefault(r => r.Ref == resourceId.Value);

            if (existing is null)
            {
                list.Add(new()
                {
                    Ref = resourceId.Value,
                    IsApproved = isApproved
                });
            }
            else
            {
                existing.IsApproved = isApproved;
            }

            data.Resources = list;
        }

        if (packageId.HasValue)
        {
            var list = data.Packages ?? [];
            var existing = list.FirstOrDefault(r => r.Ref == packageId.Value);

            if (existing is null)
            {
                list.Add(new()
                {
                    Ref = packageId.Value,
                    IsApproved = isApproved
                });
            }
            else
            {
                existing.IsApproved = isApproved;
            }

            data.Packages = list;
        }

        var processAfter = TimeSpan.FromSeconds(notifyInSeconds);
        var now = DateTime.UtcNow;

        var candidate = now.Add(processAfter / (data.Updated + 1));

        msg.Schedule = candidate < msg.Schedule ? msg.Schedule : candidate;

        return data;
    }
}
