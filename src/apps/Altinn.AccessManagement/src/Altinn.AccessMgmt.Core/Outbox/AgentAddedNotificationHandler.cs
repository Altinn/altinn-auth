using Altinn.AccessMgmt.Core.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Models.Base;
using Microsoft.FeatureManagement;

namespace Altinn.AccessMgmt.Core.Outbox;

public class AgentAddedNotificationHandler(AppDbContext db, IFeatureManager featureManager) : IOutboxHandler
{
    public async Task<OutboxStatus> Handle(OutboxMessage message, CancellationToken cancellationToken)
    {
        if (await featureManager.IsDisabledAsync(AccessMgmtFeatureFlags.OutboxAgentAddedNotify, cancellationToken))
        {
            db.OutboxMessageLogs.Add(message, $"Feature flag '{AccessMgmtFeatureFlags.OutboxAgentAddedNotify}' is disabled.");
            await db.SaveChangesAsync(cancellationToken);
            return OutboxStatus.Completed;
        }

        return OutboxStatus.Completed;
    }
}

public class AgentAddedNotificationMessage
{
    public Guid AgentId { get; set; }

    public Guid ProviderId { get; set; }
}
