using System.Diagnostics;
using System.Text.Json;
using Altinn.AccessMgmt.Core.Outbox;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Models.Base;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.Core.Notifications;

/// <summary>
/// Provides helper methods for managing outbox notifications related to access being revoked.
/// </summary>
/// <remarks>
/// Encapsulates the logic for creating, scheduling, and cancelling outbox messages
/// handled by <c>access_removed</c>.
/// </remarks>
public static class AccessRemovedNotification
{
    public const string Handler = "access_removed";

    public const int DefaultNotifyInSeconds = 60 * 10;

    /// <summary>
    /// Creates or updates a pending outbox message for an access removed notification.
    /// </summary>
    /// <remarks>
    /// This method performs an upsert operation for an outbox message identified by the
    /// combination of <paramref name="fromId"/> and <paramref name="toId"/>.
    ///
    /// If a matching pending message already exists, it is updated with the new resource or package.
    /// If no matching message exists, a new one is created with a scheduled processing time
    /// based on <paramref name="notifyInSeconds"/>.
    /// </remarks>
    /// <param name="db">
    /// The <see cref="AppDbContext"/> used to access the outbox messages.
    /// </param>
    /// <param name="fromId">
    /// The identifier of the entity that removed access.
    /// </param>
    /// <param name="toId">
    /// The identifier of the entity whose access was removed.
    /// </param>
    /// <param name="resourceId">
    /// Optional identifier of the resource for which access was removed.
    /// </param>
    /// <param name="packageId">
    /// Optional identifier of the package for which access was removed.
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
        Guid fromId,
        Guid toId,
        Guid? resourceId,
        Guid? packageId,
        int notifyInSeconds = DefaultNotifyInSeconds,
        CancellationToken ct = default)
    {
        await db.OutboxMessages.UpsertOutboxAsync(
            refId: $"{Handler}_{fromId}_{toId}",
            handler: Handler,
            addValueFactory: msg => AddValue(fromId, toId, resourceId, packageId, msg, notifyInSeconds),
            updateValueFactory: (msg, data) => UpdateValue(fromId, toId, resourceId, packageId, msg, data, notifyInSeconds),
            cancellationToken: ct
        );

        await AccessAddedNotification.RemoveValue(db, fromId, toId, resourceId, packageId, ct);
    }

    /// <summary>
    /// Removes a specific resource or package from an existing access removed notification.
    /// </summary>
    /// <remarks>
    /// This method updates an existing outbox message by removing the specified resource or package
    /// from its payload. This is typically used when access is re-granted before the notification is sent.
    /// </remarks>
    /// <param name="db">
    /// The <see cref="AppDbContext"/> used to access the outbox messages.
    /// </param>
    /// <param name="fromId">
    /// The identifier of the entity that removed access.
    /// </param>
    /// <param name="toId">
    /// The identifier of the entity whose access was removed.
    /// </param>
    /// <param name="resourceId">
    /// Optional identifier of the resource to remove from the notification.
    /// </param>
    /// <param name="packageId">
    /// Optional identifier of the package to remove from the notification.
    /// </param>
    /// <param name="ct">
    /// A token used to observe cancellation while querying the database.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// </returns>
    public static async Task RemoveValue(
        AppDbContext db,
        Guid fromId,
        Guid toId,
        Guid? resourceId,
        Guid? packageId,
        CancellationToken ct = default
    )
    {
        var message = await db.OutboxMessages
            .AsTracking()
            .FirstOrDefaultAsync(
                o =>
                o.RefId == $"{Handler}_{fromId}_{toId}" &&
                o.Handler == Handler &&
                o.Status == OutboxStatus.Pending,
                ct);

        if (message is null)
        {
            return;
        }

        var data = JsonSerializer.Deserialize<AccessRemovedNotificationMessage>(message.Data);
        var updatedData = RemoveUpdateValue(resourceId, packageId, data);
        updatedData.Updated++;
        message.Data = JsonSerializer.Serialize(updatedData);
    }

    private static AccessRemovedNotificationMessage RemoveUpdateValue(
        Guid? resourceId,
        Guid? packageId,
        AccessRemovedNotificationMessage data
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

    /// <summary>
    /// Cancels a pending access removed notification by removing its outbox message.
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
    /// The identifier of the entity that removed access.
    /// </param>
    /// <param name="to">
    /// The identifier of the entity whose access was removed.
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

    private static AccessRemovedNotificationMessage AddValue(
        Guid fromId,
        Guid toId,
        Guid? resourceId,
        Guid? packageId,
        OutboxMessage msg,
        int notifyInSeconds
    )
    {
        var processAfter = TimeSpan.FromSeconds(notifyInSeconds);
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
        Guid fromId,
        Guid toId,
        Guid? resourceId,
        Guid? packageId,
        OutboxMessage msg,
        AccessRemovedNotificationMessage data,
        int notifyInSeconds
    )
    {
        if (data is null)
        {
            Activity.Current?.AddTag(nameof(AccessRemovedNotification), $"Current outbox message {nameof(AccessRemovedNotification)} is null? Creating new object.");
            return AddValue(fromId, toId, resourceId, packageId, msg, notifyInSeconds);
        }

        data.Updated++;

        if (resourceId.HasValue && !data.ResourceIds.Contains(resourceId.Value))
        {
            data.ResourceIds ??= [];
            data.ResourceIds.Add(resourceId.Value);
        }

        if (packageId.HasValue && !data.PackageIds.Contains(packageId.Value))
        {
            data.PackageIds ??= [];
            data.PackageIds.Add(packageId.Value);
        }

        var processAfter = TimeSpan.FromSeconds(notifyInSeconds);
        var now = DateTime.UtcNow;

        var candidate = now.Add(processAfter / (data.Updated + 1));
        msg.Schedule = candidate < msg.Schedule ? msg.Schedule : candidate;

        return data;
    }
}
