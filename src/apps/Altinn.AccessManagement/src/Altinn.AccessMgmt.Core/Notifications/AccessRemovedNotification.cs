using System.Diagnostics;
using Altinn.AccessMgmt.Core.Outbox;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;

namespace Altinn.AccessMgmt.Core.Notifications;

public static class AccessRemovedNotification
{
    public const string Handler = "access_removed";

    public static async Task Upsert(
        AppDbContext db,
        Guid fromId,
        Guid toId,
        Guid? resourceId,
        Guid? packageId,
        int notifyAccessRemovedInSeconds = 60 * 15,
        CancellationToken ct = default)
    {
        await db.OutboxMessages.UpsertOutboxAsync(
            refId: $"{Handler}_{fromId}_{toId}",
            handler: Handler,
            addValueFactory: msg => AddValue(fromId, toId, resourceId, packageId, msg, notifyAccessRemovedInSeconds),
            updateValueFactory: (msg, data) => UpdateValue(fromId, toId, resourceId, packageId, msg, data, notifyAccessRemovedInSeconds),
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

    private static AccessRemovedNotificationMessage AddValue(
        Guid fromId,
        Guid toId,
        Guid? resourceId,
        Guid? packageId,
        OutboxMessage msg,
        int notifyAccessRemovedInSeconds
    )
    {
        var processAfter = TimeSpan.FromSeconds(notifyAccessRemovedInSeconds);
        var now = DateTime.UtcNow;
        var schedule = now.Add(processAfter);
        msg.Schedule = schedule;
        msg.Timeout = TimeSpan.FromMinutes(1);

        return new AccessRemovedNotificationMessage()
        {
            FromId = fromId,
            ToId = toId,
            ResourceIds = resourceId.HasValue && resourceId.Value != Guid.Empty ? [resourceId.Value] : [],
            PackageIds = packageId.HasValue && packageId.Value != Guid.Empty ? [packageId.Value] : [],
            Updated = 0,
        };
    }

    private static AccessRemovedNotificationMessage UpdateValue(
        Guid requesterId,
        Guid recipientId,
        Guid? resourceId,
        Guid? packageId,
        OutboxMessage msg,
        AccessRemovedNotificationMessage data,
        int notifyAccessRemovedInSeconds
    )
    {
        if (data is null)
        {
            Activity.Current?.AddTag(nameof(RequestPendingNotification), $"Current outbox message {nameof(AccessRemovedNotification)} is null? Creating new object.");
            return AddValue(requesterId, recipientId, resourceId, packageId, msg, notifyAccessRemovedInSeconds);
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

        var processAfter = TimeSpan.FromSeconds(notifyAccessRemovedInSeconds);
        var now = DateTime.UtcNow;

        var candidate = now.Add(processAfter / (data.Updated + 1));
        msg.Schedule = candidate < msg.Schedule ? msg.Schedule : candidate;

        return data;
    }
}
