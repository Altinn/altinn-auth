using System.Diagnostics;
using Altinn.AccessMgmt.Core.Outbox;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;

namespace Altinn.AccessMgmt.Core.Notifications;

public static class RequestReviewedNotification
{
    public const string Handler = "request_reviewed";

    public static async Task Upsert(
        AppDbContext db,
        Guid requesterId,
        Guid recipientId,
        Guid? resourceId,
        Guid? packageId,
        bool isApproved,
        int notifyRequestReviewedInSeconds = 60 * 15,
        CancellationToken ct = default)
    {
        await db.OutboxMessages.UpsertOutboxAsync(
            refId: $"{Handler}_{requesterId}_{recipientId}",
            handler: Handler,
            addValueFactory: msg => AddValue(requesterId, recipientId, resourceId, packageId, isApproved, msg, notifyRequestReviewedInSeconds),
            updateValueFactory: (msg, data) => UpdateValue(requesterId, recipientId, resourceId, packageId, isApproved, msg, data, notifyRequestReviewedInSeconds),
            cancellationToken: ct
        );
    }

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
        int notifyRequestReviewedInSeconds
    )
    {
        var processAfter = TimeSpan.FromSeconds(notifyRequestReviewedInSeconds);
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
        int notifyRequestPendingInSeconds
    )
    {
        if (data is null)
        {
            Activity.Current?.AddTag(nameof(RequestReviewedNotification), $"Current outbox message {nameof(RequestReviewNotificationMessage)} is null? Creating new object.");
            return AddValue(reviewerId, recipientId, resourceId, packageId, isApproved, msg, notifyRequestPendingInSeconds);
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

        var processAfter = TimeSpan.FromSeconds(notifyRequestPendingInSeconds);
        var now = DateTime.UtcNow;

        var candidate = now.Add(processAfter / (data.Updated + 1));

        msg.Schedule = candidate < msg.Schedule ? msg.Schedule : candidate;

        return data;
    }
}
