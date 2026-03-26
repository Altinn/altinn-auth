using Altinn.AccessMgmt.PersistenceEF.Models.Base;

namespace Altinn.AccessMgmt.PersistenceEF.Models;

/// <summary>
/// Represents a persisted outbox message Log.
/// </summary>
public class OutboxMessageLog : BaseOutboxMessageLog
{
    public OutboxMessage OutboxMessage { get; set; }
}
