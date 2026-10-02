using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Queries;
using Altinn.Authorization.Api.Contracts.AccessManagement.ActivityLog;

namespace Altinn.AccessMgmt.Core.Services;

/// <summary>
/// Which parts of the activity log each administrative role may see: access managers see the
/// assignment part, client administrators the delegation part, and main administrators
/// everything. A caller's visibility is the union over their effective roles for the party.
/// Extend the matrix (per role, or with finer grains than <see cref="ActivityLogType"/>) as
/// the log gains data points.
/// </summary>
public static class ActivityLogRoleMatrix
{
    /// <summary>
    /// Every main record type in the log.
    /// </summary>
    public static readonly IReadOnlySet<ActivityLogType> AllTypes =
        new HashSet<ActivityLogType> { ActivityLogType.Assignment, ActivityLogType.Delegation, ActivityLogType.Request };

    // Access managers see the assignment part including the requests that lead to
    // assignments (issue #3981); client administrators see the delegation part.
    private static readonly IReadOnlyDictionary<Guid, IReadOnlySet<ActivityLogType>> Matrix = new Dictionary<Guid, IReadOnlySet<ActivityLogType>>
    {
        [RoleConstants.AccessManager.Id] = new HashSet<ActivityLogType> { ActivityLogType.Assignment, ActivityLogType.Request },
        [RoleConstants.ClientAdministrator.Id] = new HashSet<ActivityLogType> { ActivityLogType.Delegation },
        [RoleConstants.MainAdministrator.Id] = AllTypes,
        [RoleConstants.MainAdministratorA2.Id] = AllTypes,
    };

    // Maskinporten schema events are hidden by default everywhere; these unlock them when the
    // caller asks for them. Maskinporten administration is granted as an access package, not a
    // role, so the id here is a package id — the matrix keys are simply "granted thing" ids.
    private static readonly IReadOnlySet<Guid> MaskinportenSchemaViewers = new HashSet<Guid>
    {
        PackageConstants.MaskinportenAdministrator.Id,
    };

    /// <summary>
    /// Returns the union of log types the given effective roles and packages may see. Ids
    /// outside the matrix contribute nothing; an empty result means the caller may not see
    /// the log at all.
    /// </summary>
    public static IReadOnlySet<ActivityLogType> AllowedTypes(IEnumerable<Guid> roleOrPackageIds)
    {
        var allowed = new HashSet<ActivityLogType>();
        foreach (var id in roleOrPackageIds ?? [])
        {
            if (Matrix.TryGetValue(id, out var types))
            {
                allowed.UnionWith(types);
                if (allowed.Count == AllTypes.Count)
                {
                    break;
                }
            }
        }

        return allowed;
    }

    /// <summary>
    /// Whether the given effective roles and packages unlock Maskinporten schema events.
    /// </summary>
    public static bool MaySeeMaskinportenSchema(IEnumerable<Guid> roleOrPackageIds)
        => roleOrPackageIds?.Any(MaskinportenSchemaViewers.Contains) == true;

    /// <summary>
    /// Constrains a filter to the allowed types: the type list becomes the allowed set (or its
    /// intersection with what was requested), and explicitly requested catalog combinations
    /// outside the allowed types are dropped. Returns false when the request only asks for
    /// types the caller may not see — the result is known to be empty without querying.
    /// </summary>
    public static bool TryConstrain(ActivityLogQueryFilter filter, IReadOnlySet<ActivityLogType> allowed, out ActivityLogQueryFilter constrained)
    {
        constrained = filter;

        if (allowed.Count == 0)
        {
            return false;
        }

        if (allowed.Count == AllTypes.Count)
        {
            return true;
        }

        var types = filter.Types is { Count: > 0 }
            ? filter.Types.Where(allowed.Contains).ToList()
            : [.. allowed];

        if (types.Count == 0)
        {
            return false;
        }

        var keys = filter.ActivityTypeKeys;
        if (keys is { Count: > 0 })
        {
            keys = keys.Where(k => allowed.Contains(k.Type)).ToList();
            if (keys.Count == 0)
            {
                return false;
            }
        }

        constrained = filter with { Types = types, ActivityTypeKeys = keys };
        return true;
    }
}
