using Altinn.AccessMgmt.Core.Services;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Queries;
using Altinn.Authorization.Api.Contracts.AccessManagement.ActivityLog;

namespace Altinn.AccessManagement.Tests.Unit.Services;

/// <summary>
/// Pins the activity log role matrix: which parts of the log each administrative role
/// unlocks, and how a filter is constrained to the caller's visible types.
/// </summary>
[UnitTest]
public class ActivityLogRoleMatrixTests
{
    [Fact]
    public void AllowedTypes_FollowsTheMatrix()
    {
        Assert.Equal(
            new HashSet<ActivityLogType> { ActivityLogType.Assignment, ActivityLogType.Request },
            ActivityLogRoleMatrix.AllowedTypes([RoleConstants.AccessManager.Id]));
        Assert.Equal([ActivityLogType.Delegation], ActivityLogRoleMatrix.AllowedTypes([RoleConstants.ClientAdministrator.Id]));
        Assert.Equal(ActivityLogRoleMatrix.AllTypes, ActivityLogRoleMatrix.AllowedTypes([RoleConstants.MainAdministrator.Id]));
        Assert.Equal(ActivityLogRoleMatrix.AllTypes, ActivityLogRoleMatrix.AllowedTypes([RoleConstants.MainAdministratorA2.Id]));
    }

    [Fact]
    public void AllowedTypes_UnionsOverRolesAndIgnoresUnknownRoles()
    {
        var combined = ActivityLogRoleMatrix.AllowedTypes([RoleConstants.AccessManager.Id, RoleConstants.ClientAdministrator.Id, RoleConstants.Rightholder.Id]);
        Assert.Equal(new HashSet<ActivityLogType> { ActivityLogType.Assignment, ActivityLogType.Request, ActivityLogType.Delegation }, combined);

        Assert.Empty(ActivityLogRoleMatrix.AllowedTypes([RoleConstants.Rightholder.Id, Guid.NewGuid()]));
        Assert.Empty(ActivityLogRoleMatrix.AllowedTypes([]));
        Assert.Empty(ActivityLogRoleMatrix.AllowedTypes(null));
    }

    [Fact]
    public void MaySeeMaskinportenSchema_RequiresTheMaskinportenAdministratorPackage()
    {
        Assert.True(ActivityLogRoleMatrix.MaySeeMaskinportenSchema([PackageConstants.MaskinportenAdministrator.Id]));
        Assert.True(ActivityLogRoleMatrix.MaySeeMaskinportenSchema([RoleConstants.AccessManager.Id, PackageConstants.MaskinportenAdministrator.Id]));
        Assert.False(ActivityLogRoleMatrix.MaySeeMaskinportenSchema([RoleConstants.MainAdministrator.Id]));
        Assert.False(ActivityLogRoleMatrix.MaySeeMaskinportenSchema([]));
        Assert.False(ActivityLogRoleMatrix.MaySeeMaskinportenSchema(null));
    }

    [Fact]
    public void TryConstrain_LimitsTypesToTheAllowedSet()
    {
        var allowed = ActivityLogRoleMatrix.AllowedTypes([RoleConstants.AccessManager.Id]);

        // No requested types: the allowed set becomes the type filter.
        Assert.True(ActivityLogRoleMatrix.TryConstrain(new ActivityLogQueryFilter(), allowed, out var constrained));
        Assert.Equal(allowed, constrained.Types.ToHashSet());

        // Requested types are intersected.
        var mixed = new ActivityLogQueryFilter { Types = [ActivityLogType.Assignment, ActivityLogType.Delegation] };
        Assert.True(ActivityLogRoleMatrix.TryConstrain(mixed, allowed, out constrained));
        Assert.Equal([ActivityLogType.Assignment], constrained.Types);

        // Only disallowed types requested: nothing is visible.
        var disallowed = new ActivityLogQueryFilter { Types = [ActivityLogType.Delegation] };
        Assert.False(ActivityLogRoleMatrix.TryConstrain(disallowed, allowed, out _));
    }

    [Fact]
    public void TryConstrain_FiltersCatalogKeysAndHandlesFullAndEmptyAccess()
    {
        var allowed = ActivityLogRoleMatrix.AllowedTypes([RoleConstants.AccessManager.Id]);

        var keys = new ActivityLogQueryFilter
        {
            ActivityTypeKeys =
            [
                new ActivityTypeKey(ActivityLogType.Assignment, null, ActivityLogTrigger.Created, null),
                new ActivityTypeKey(ActivityLogType.Delegation, null, ActivityLogTrigger.Created, null),
            ],
        };
        Assert.True(ActivityLogRoleMatrix.TryConstrain(keys, allowed, out var constrained));
        Assert.Equal(ActivityLogType.Assignment, Assert.Single(constrained.ActivityTypeKeys).Type);

        var onlyDisallowedKeys = new ActivityLogQueryFilter
        {
            ActivityTypeKeys = [new ActivityTypeKey(ActivityLogType.Delegation, null, ActivityLogTrigger.Created, null)],
        };
        Assert.False(ActivityLogRoleMatrix.TryConstrain(onlyDisallowedKeys, allowed, out _));

        // Full access leaves the filter untouched; no access constrains nothing.
        var filter = new ActivityLogQueryFilter { Types = [ActivityLogType.Delegation] };
        Assert.True(ActivityLogRoleMatrix.TryConstrain(filter, ActivityLogRoleMatrix.AllTypes, out constrained));
        Assert.Same(filter, constrained);
        Assert.False(ActivityLogRoleMatrix.TryConstrain(filter, new HashSet<ActivityLogType>(), out _));
    }
}
