using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Queries;
using Altinn.Authorization.Api.Contracts.AccessManagement.ActivityLog;

namespace Altinn.AccessMgmt.Core.Utils;

/// <summary>
/// Maps the shared <see cref="ActivityLogQueryParameters"/> onto the
/// <see cref="ActivityLogQueryFilter"/>, so every activity log endpoint builds its filter the
/// same way.
/// </summary>
public static class ActivityLogQueryMapper
{
    /// <summary>
    /// Resolves activity type catalog ids to their whole-combination keys. Returns false with
    /// the offending id when one is unknown; <paramref name="keys"/> is null when no ids were
    /// given.
    /// </summary>
    public static bool TryResolveTypeKeys(IReadOnlyCollection<Guid> typeIds, out List<ActivityTypeKey> keys, out Guid unknownId)
    {
        keys = null;
        unknownId = Guid.Empty;

        if (typeIds is not { Count: > 0 })
        {
            return true;
        }

        keys = new List<ActivityTypeKey>(typeIds.Count);
        foreach (var id in typeIds)
        {
            if (!ActivityTypeConstants.TryGetById(id, out var definition))
            {
                unknownId = id;
                keys = null;
                return false;
            }

            keys.Add(new ActivityTypeKey(definition.Entity.Type, definition.Entity.Subtype, definition.Entity.Trigger, definition.Entity.Status));
        }

        return true;
    }

    /// <summary>
    /// Builds the query filter from the bound parameters and the already resolved type keys.
    /// The party anchor is applied separately by the service.
    /// </summary>
    public static ActivityLogQueryFilter BuildFilter(ActivityLogQueryParameters parameters, List<ActivityTypeKey> activityTypeKeys) => new()
    {
        ActivityTypeKeys = activityTypeKeys,
        Types = parameters.Type,
        Subtypes = parameters.Subtype,
        Triggers = parameters.Trigger,
        Statuses = parameters.Status,
        ByIds = parameters.By,
        SourceIds = parameters.Source,
        OperationIds = parameters.Operation,
        FromIds = parameters.From,
        ToIds = parameters.To,
        ViaIds = parameters.Via,
        RoleIds = parameters.Role,
        PackageIds = parameters.Package,
        ResourceIds = parameters.Resource,
        InstanceIds = parameters.Instance,
        ItemIds = parameters.ItemId,
        ParentIds = parameters.ParentId,
        After = parameters.After,
        Before = parameters.Before,
    };
}
