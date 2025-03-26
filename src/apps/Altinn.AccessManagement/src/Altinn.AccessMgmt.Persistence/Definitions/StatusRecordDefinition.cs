using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Definitions;

namespace Altinn.AccessMgmt.Repo.Definitions;

/// <inheritdoc/>
public class StatusRecordDefinition : BaseDbDefinition<StatusRecord>, IDbDefinition
{
    /// <inheritdoc/>
    public StatusRecordDefinition(DbDefinitionRegistry definitionRegistry) : base(definitionRegistry)
    {
    }

    /// <inheritdoc/>
    public void Define()
    {
        definitionRegistry.Define<StatusRecord>(def =>
        {
            def.RegisterPrimaryKey([t => t.Id]);

            def.RegisterProperty(t => t.Id);
            def.RegisterProperty(t => t.Name);
            def.RegisterProperty(t => t.State);
            def.RegisterProperty(t => t.Message);
            def.RegisterProperty(t => t.Payload);
            def.RegisterProperty(t => t.RetryLimit);
            def.RegisterProperty(t => t.RetryCount);
            def.RegisterProperty(t => t.Timestamp);
        });
    }
}
