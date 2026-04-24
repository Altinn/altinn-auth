using Altinn.AccessMgmt.Core.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Models.Base;
using Microsoft.FeatureManagement;

namespace Altinn.AccessMgmt.Core.Outbox;

public class ClientAddedNotificationHandler(AppDbContext db, IFeatureManager featureManager) : IOutboxHandler
{
    public async Task<OutboxStatus> Handle(OutboxMessage message, CancellationToken cancellationToken)
    {
        if (await featureManager.IsDisabledAsync(AccessMgmtFeatureFlags.OutboxClientAddedNotify, cancellationToken))
        {
            db.OutboxMessageLogs.Add(message, $"Feature flag '{AccessMgmtFeatureFlags.OutboxClientAddedNotify}' is disabled.");
            await db.SaveChangesAsync(cancellationToken);
            return OutboxStatus.Completed;
        }

        return OutboxStatus.Completed;
    }
}

public class ClientAddedNotificationMessage
{
    public Guid AgentId { get; set; }

    public Guid ProviderId { get; set; }

    public List<Guid> Clients { get; set; } = [];

    public int Updated { get; set; }
}
