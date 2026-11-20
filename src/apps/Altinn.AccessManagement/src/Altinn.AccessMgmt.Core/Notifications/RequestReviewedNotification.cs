using System.Diagnostics;
using Altinn.AccessMgmt.Core.Outbox;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement.Request;

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
    /// <param name="requestStatus">
    /// The outcome of the review: approved, partially approved or rejected.
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
        RequestStatusV2 requestStatus,
        int notifyInSeconds = DefaultNotifyInSeconds,
        CancellationToken ct = default)
    {
        await db.OutboxMessages.UpsertOutboxAsync(
            refId: $"{Handler}_{requesterId}_{recipientId}",
            handler: Handler,
            addValueFactory: msg => AddValue(requesterId, recipientId, resourceId, packageId, requestStatus, msg, notifyInSeconds),
            updateValueFactory: (msg, data) => UpdateValue(requesterId, recipientId, resourceId, packageId, requestStatus, msg, data, notifyInSeconds),
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

    /// <summary>
    /// Creates the content of a new outbox message holding the first reviewed resource or package
    /// for a requester and recipient, and schedules the message <paramref name="notifyInSeconds"/> from now.
    /// </summary>
    /// <param name="reviewerId">The entity that reviewed the request.</param>
    /// <param name="recipientId">The entity that receives the notification.</param>
    /// <param name="resourceId">The reviewed resource, or null when a package was reviewed.</param>
    /// <param name="packageId">The reviewed package, or null when a resource was reviewed.</param>
    /// <param name="requestStatus">The outcome of the review.</param>
    /// <param name="msg">The outbox message whose schedule and timeout are set.</param>
    /// <param name="notifyInSeconds">The delay before the message is processed.</param>
    /// <returns>The message content, with the outcome stored as both <c>RequestStatus</c> and <c>IsApproved</c>.</returns>
    private static RequestReviewNotificationMessage AddValue(
        Guid reviewerId,
        Guid recipientId,
        Guid? resourceId,
        Guid? packageId,
        RequestStatusV2 requestStatus,
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
            Resources = resourceId.HasValue && resourceId.Value != Guid.Empty ? [new() { Ref = resourceId.Value, RequestStatus = requestStatus, IsApproved = IsApproved(requestStatus) }] : [],
            Packages = packageId.HasValue && packageId.Value != Guid.Empty ? [new() { Ref = packageId.Value, RequestStatus = requestStatus, IsApproved = IsApproved(requestStatus) }] : [],
            Updated = 0,
        };
    }

    /// <summary>
    /// Adds a reviewed resource or package to a pending outbox message, so that one email covers
    /// every review between the same requester and recipient.
    /// </summary>
    /// <remarks>
    /// An item that is already in the message gets the latest outcome. Each update proposes a schedule of now plus
    /// <paramref name="notifyInSeconds"/> divided by one more than the number of updates, and keeps the current schedule
    /// if that is later, so a burst of reviews is sent as one email without waiting the full delay after each review.
    /// When the stored content can't be read, a new message is created as in <see cref="AddValue"/>.
    /// </remarks>
    /// <param name="reviewerId">The entity that reviewed the request.</param>
    /// <param name="recipientId">The entity that receives the notification.</param>
    /// <param name="resourceId">The reviewed resource, or null when a package was reviewed.</param>
    /// <param name="packageId">The reviewed package, or null when a resource was reviewed.</param>
    /// <param name="requestStatus">The outcome of the review.</param>
    /// <param name="msg">The outbox message whose schedule is updated.</param>
    /// <param name="data">The content already stored in the message.</param>
    /// <param name="notifyInSeconds">The delay used to compute the new schedule.</param>
    /// <returns>The updated message content.</returns>
    private static RequestReviewNotificationMessage UpdateValue(
        Guid reviewerId,
        Guid recipientId,
        Guid? resourceId,
        Guid? packageId,
        RequestStatusV2 requestStatus,
        OutboxMessage msg,
        RequestReviewNotificationMessage data,
        int notifyInSeconds
    )
    {
        if (data is null)
        {
            Activity.Current?.AddTag(nameof(RequestReviewedNotification), $"Current outbox message {nameof(RequestReviewNotificationMessage)} is null? Creating new object.");
            return AddValue(reviewerId, recipientId, resourceId, packageId, requestStatus, msg, notifyInSeconds);
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
                    RequestStatus = requestStatus,
                    IsApproved = IsApproved(requestStatus),
                });
            }
            else
            {
                existing.RequestStatus = requestStatus;
                existing.IsApproved = IsApproved(requestStatus);
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
                    RequestStatus = requestStatus,
                    IsApproved = IsApproved(requestStatus),
                });
            }
            else
            {
                existing.RequestStatus = requestStatus;
                existing.IsApproved = IsApproved(requestStatus);
            }

            data.Packages = list;
        }

        var processAfter = TimeSpan.FromSeconds(notifyInSeconds);
        var now = DateTime.UtcNow;

        var candidate = now.Add(processAfter / (data.Updated + 1));

        msg.Schedule = candidate < msg.Schedule ? msg.Schedule : candidate;

        return data;
    }

    /// <summary>
    /// Maps a review outcome to the legacy <c>IsApproved</c> flag, which is kept so that
    /// messages written before <c>RequestStatus</c> existed can still be read.
    /// </summary>
    private static bool IsApproved(RequestStatusV2 requestStatus)
        => requestStatus is RequestStatusV2.Approved or RequestStatusV2.PartialApprove;
}
