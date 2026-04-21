using Altinn.AccessMgmt.Core.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Models.Base;
using Microsoft.FeatureManagement;

namespace Altinn.AccessMgmt.Core.Outbox;

public class AccessAddedNotificationHandler(AppDbContext db, IFeatureManager featureManager) : IOutboxHandler
{
    public async Task<OutboxStatus> Handle(OutboxMessage message, CancellationToken cancellationToken)
    {
        if (await featureManager.IsDisabledAsync(AccessMgmtFeatureFlags.AccessMgmtCoreOutboxAccessNotifyAdded, cancellationToken))
        {
            db.OutboxMessageLogs.Add(message, $"Feature flag '{AccessMgmtFeatureFlags.AccessMgmtCoreOutboxAccessNotifyAdded}' is disabled.");
            await db.SaveChangesAsync(cancellationToken);
            return OutboxStatus.Completed;
        }

        return OutboxStatus.Completed;
    }
}

public class AccessAddedNotificationMessage
{
    public Guid FromId { get; set; }

    public Guid ToId { get; set; }

    public List<Guid> PackageIds { get; set; }

    public List<Guid> ResourceIds { get; set; }

    public int Updated { get; set; }
}
