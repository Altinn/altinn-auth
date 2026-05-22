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
/// Provides helper methods for managing outbox notifications related to instance shares being revoked.
/// </summary>
/// <remarks>
/// Encapsulates the logic for creating, scheduling, and cancelling outbox messages
/// handled by <c>instance_removed</c>.
/// </remarks>
public static class InstanceRemovedNotification
{
    public const string Handler = "instance_removed";

    public const int DefaultNotifyInSeconds = 60 * 10;

    /// <summary>
    /// Creates or updates a pending outbox message for an instance removed notification.
    /// </summary>
    /// <remarks>
    /// This method performs an upsert operation for an outbox message identified by the
    /// combination of <paramref name="fromId"/> and <paramref name="toId"/>.
    ///
    /// If a matching pending message already exists, it is updated with the new instance.
    /// If no matching message exists, a new one is created with a scheduled processing time
    /// based on <paramref name="notifyInSeconds"/>.
    /// </remarks>
    /// <param name="db">
    /// The <see cref="AppDbContext"/> used to access the outbox messages.
    /// </param>
    /// <param name="fromId">
    /// The identifier of the entity that revoked the instance share.
    /// </param>
    /// <param name="toId">
    /// The identifier of the entity whose instance share was revoked.
    /// </param>
    /// <param name="resourceId">
    /// The identifier of the resource associated with the instance.
    /// </param>
    /// <param name="instanceId">
    /// The identifier of the instance whose share was revoked.
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
        Guid resourceId,
        string instanceId,
        int notifyInSeconds = DefaultNotifyInSeconds,
        CancellationToken ct = default)
    {
        await db.OutboxMessages.UpsertOutboxAsync(
            refId: $"{Handler}_{fromId}_{toId}",
            handler: Handler,
            addValueFactory: msg => AddValue(fromId, toId, resourceId, instanceId, msg, notifyInSeconds),
            updateValueFactory: (msg, data) => UpdateValue(fromId, toId, resourceId, instanceId, msg, data, notifyInSeconds),
            cancellationToken: ct
        );

        await InstanceAddedNotification.RemoveValue(db, fromId, toId, resourceId, instanceId, ct);
    }

    /// <summary>
    /// Removes a specific instance from an existing instance removed notification.
    /// </summary>
    /// <remarks>
    /// This method updates an existing outbox message by removing the specified instance
    /// from its payload. This is typically used when an instance share is re-granted before the removal notification is sent.
    /// </remarks>
    /// <param name="db">
    /// The <see cref="AppDbContext"/> used to access the outbox messages.
    /// </param>
    /// <param name="fromId">
    /// The identifier of the entity that revoked the instance share.
    /// </param>
    /// <param name="toId">
    /// The identifier of the entity whose instance share was revoked.
    /// </param>
    /// <param name="resourceId">
    /// The identifier of the resource associated with the instance.
    /// </param>
    /// <param name="instanceId">
    /// The identifier of the instance to remove from the notification.
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
        Guid resourceId,
        string instanceId,
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

        var data = JsonSerializer.Deserialize<InstanceRemovedNotificationMessage>(message.Data);
        var updatedData = RemoveUpdateValue(resourceId, instanceId, data);
        updatedData.Updated++;
        message.Data = JsonSerializer.Serialize(updatedData);
    }

    private static InstanceRemovedNotificationMessage RemoveUpdateValue(
        Guid resourceId,
        string instanceId,
        InstanceRemovedNotificationMessage data
    )
    {
        if (data.Instances.FirstOrDefault(r => r.ResourceId == resourceId) is { } value)
        {
            value.InstanceIds.RemoveAll(i => i == instanceId);
        }

        return data;
    }

    /// <summary>
    /// Cancels a pending instance removed notification by removing its outbox message.
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
    /// The identifier of the entity that revoked the instance share.
    /// </param>
    /// <param name="to">
    /// The identifier of the entity whose instance share was revoked.
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

    private static InstanceRemovedNotificationMessage AddValue(
        Guid fromId,
        Guid toId,
        Guid resourceId,
        string instanceId,
        OutboxMessage msg,
        int notifyInSeconds
    )
    {
        var processAfter = TimeSpan.FromSeconds(notifyInSeconds);
        var now = DateTime.UtcNow;
        var schedule = now.Add(processAfter);
        msg.Schedule = schedule;
        msg.Timeout = TimeSpan.FromMinutes(1);

        return new()
        {
            FromId = fromId,
            ToId = toId,
            Instances = [
                new()
                {
                    InstanceIds = [instanceId],
                    ResourceId = resourceId,
                }
            ],
            Updated = 0,
        };
    }

    private static InstanceRemovedNotificationMessage UpdateValue(
        Guid fromId,
        Guid toId,
        Guid resourceId,
        string instanceId,
        OutboxMessage msg,
        InstanceRemovedNotificationMessage data,
        int notifyInSeconds
    )
    {
        if (data is null)
        {
            Activity.Current?.AddTag(nameof(InstanceRemovedNotification), $"Current outbox message {nameof(InstanceRemovedNotification)} is null? Creating new object.");
            return AddValue(fromId, toId, resourceId, instanceId, msg, notifyInSeconds);
        }

        data.Updated++;
        data.Instances ??= [];

        if (string.IsNullOrEmpty(instanceId))
        {
            return data;
        }

        var processAfter = TimeSpan.FromSeconds(notifyInSeconds);
        var now = DateTime.UtcNow;

        var candidate = now.Add(processAfter / (data.Updated + 1));
        msg.Schedule = candidate < msg.Schedule ? msg.Schedule : candidate;

        if (data.Instances.FirstOrDefault(r => r.ResourceId == resourceId) is { } value)
        {
            if (value.InstanceIds.Contains(instanceId))
            {
                return data;
            }

            value.InstanceIds.Add(instanceId);
            return data;
        }

        data.Instances.Add(new()
        {
            ResourceId = resourceId,
            InstanceIds = [instanceId],
        });

        return data;
    }
}
