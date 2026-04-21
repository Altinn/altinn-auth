using Altinn.AccessMgmt.Core.Outbox;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;

namespace Altinn.AccessMgmt.Core.Notifications;

public static class RightholderRemovedNotification
{
    public const string Handler = "rightholder_removed";

    public static async Task Upsert(
        AppDbContext db,
        Guid from,
        Guid to,
        int notifyRemovedRightholderPendingInSeconds = 120,
        CancellationToken cancellationToken = default)
    {
        await db.OutboxMessages.UpsertOutboxAsync(
            refId: $"{Handler}_{from}_{to}",
            handler: Handler,
            updateValueFactory: (_, data) => data,
            addValueFactory: (msg) => AddValue(msg, notifyRemovedRightholderPendingInSeconds, from, to),
            cancellationToken: cancellationToken
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

    private static RightholderRemovedNotificationMessage AddValue(OutboxMessage msg, int notifyRemovedRightholderPendingInSeconds, Guid from, Guid to)
    {
        var processAfter = DateTime.UtcNow.Add(TimeSpan.FromSeconds(notifyRemovedRightholderPendingInSeconds));
        msg.Schedule = processAfter;
        msg.Timeout = TimeSpan.FromMinutes(1);
        
        return new()
        {
            From = from,
            To = to,
        };
    }
}
