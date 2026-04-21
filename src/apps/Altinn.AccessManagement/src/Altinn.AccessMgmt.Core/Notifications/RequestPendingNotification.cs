using System.Diagnostics;
using Altinn.AccessMgmt.Core.Outbox;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;

namespace Altinn.AccessMgmt.Core.Notifications;

public static class RequestPendingNotification
{
    public const string Handler = "request_pending";

    public static async Task Upsert(
        AppDbContext db,
        Guid requesterId,
        Guid recipientId,
        Guid? resourceId,
        Guid? packageId,
        int notifyRequestPendingInSeconds = 60 * 15,
        CancellationToken ct = default)
    {
        await db.OutboxMessages.UpsertOutboxAsync(
            refId: $"{Handler}_{requesterId}_{recipientId}",
            handler: Handler,
            addValueFactory: msg => AddValue(requesterId, recipientId, resourceId, packageId, msg, notifyRequestPendingInSeconds),
            updateValueFactory: (msg, data) => UpdateValue(requesterId, recipientId, resourceId, packageId, msg, data, notifyRequestPendingInSeconds),
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

    public static async Task RemoveValue(
        AppDbContext db,
        Guid requesterId,
        Guid recipientId,
        Guid? resourceId,
        Guid? packageId,
        CancellationToken ct = default
    )
    {
        await db.OutboxMessages.UpsertOutboxAsync<ResourceRequestPendingNotificationMessage>(
            refId: $"{Handler}_{requesterId}_{recipientId}",
            handler: Handler,
            addValueFactory: msg => new(),
            updateValueFactory: (msg, data) => RemoveValue(resourceId, packageId, data),
            cancellationToken: ct
        );
    }

    private static ResourceRequestPendingNotificationMessage RemoveValue(
        Guid? resourceId,
        Guid? packageId,
        ResourceRequestPendingNotificationMessage data
    )
    {
        if (resourceId.HasValue && resourceId.Value != Guid.Empty)
        {
            data.ResourceIds.RemoveAll(r => r == resourceId.Value);
        }

        if (packageId.HasValue && packageId.Value != Guid.Empty)
        {
            data.PackageIds.RemoveAll(p => p == packageId.Value);
        }

        return data;
    }

    private static ResourceRequestPendingNotificationMessage AddValue(
        Guid requesterId,
        Guid recipientId,
        Guid? resourceId,
        Guid? packageId,
        OutboxMessage msg,
        int notifyRequestPendingInSeconds
    )
    {
        var processAfter = TimeSpan.FromSeconds(notifyRequestPendingInSeconds);
        var now = DateTime.UtcNow;
        var schedule = now.Add(processAfter);
        msg.Schedule = schedule;
        msg.Timeout = TimeSpan.FromMinutes(1);

        return new ResourceRequestPendingNotificationMessage()
        {
            RecipientId = recipientId,
            RequesterId = requesterId,
            ResourceIds = resourceId.HasValue && resourceId.Value != Guid.Empty ? [resourceId.Value] : [],
            PackageIds = packageId.HasValue && packageId.Value != Guid.Empty ? [packageId.Value] : [],
            Updated = 1,
        };
    }

    private static ResourceRequestPendingNotificationMessage UpdateValue(
        Guid requesterId,
        Guid recipientId,
        Guid? resourceId,
        Guid? packageId,
        OutboxMessage msg,
        ResourceRequestPendingNotificationMessage data,
        int notifyRequestPendingInSeconds
    )
    {
        if (data is null)
        {
            Activity.Current?.AddTag(nameof(RequestPendingNotification), $"Current outbox message {nameof(ResourceRequestPendingNotificationMessage)} is null? Creating new object.");
            return AddValue(requesterId, recipientId, resourceId, packageId, msg, notifyRequestPendingInSeconds);
        }

        data.Updated++;

        if (resourceId.HasValue && !data.ResourceIds.Contains(resourceId.Value))
        {
            data.ResourceIds.Add(resourceId.Value);
        }

        if (packageId.HasValue && !data.PackageIds.Contains(packageId.Value))
        {
            data.PackageIds.Add(packageId.Value);
        }

        var processAfter = TimeSpan.FromSeconds(notifyRequestPendingInSeconds);
        var now = DateTime.UtcNow;

        var candidate = now.Add(processAfter / (data.Updated + 1));
        msg.Schedule = candidate < msg.Schedule ? msg.Schedule : candidate;

        return data;
    }
}
