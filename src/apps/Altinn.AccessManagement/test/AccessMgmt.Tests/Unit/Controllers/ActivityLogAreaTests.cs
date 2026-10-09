using Altinn.AccessManagement.Api.Enduser.Controllers.Base;
using Altinn.AccessMgmt.Core.Services;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.Authorization.Api.Contracts.AccessManagement.ActivityLog;

namespace Altinn.AccessManagement.Tests.Unit.Controllers;

/// <summary>
/// Pins the activity log area declarations: which slice of the log each enduser area serves,
/// which catalog entries it accepts as typeId input, and which filter fields it offers.
/// </summary>
[UnitTest]
public class ActivityLogAreaTests
{
    [Fact]
    public void Areas_AcceptOnlyTheirOwnCatalogEntries()
    {
        Assert.True(ActivityLogAreas.Connections.AcceptsCatalogEntry(ActivityLogType.Assignment, ActivityLogSubtype.Package));
        Assert.True(ActivityLogAreas.Connections.AcceptsCatalogEntry(ActivityLogType.Delegation, null));
        Assert.False(ActivityLogAreas.Connections.AcceptsCatalogEntry(ActivityLogType.Request, null));

        Assert.True(ActivityLogAreas.Request.AcceptsCatalogEntry(ActivityLogType.Request, ActivityLogSubtype.Package));
        Assert.False(ActivityLogAreas.Request.AcceptsCatalogEntry(ActivityLogType.Assignment, null));

        Assert.True(ActivityLogAreas.Maskinporten.AcceptsCatalogEntry(ActivityLogType.Assignment, null));
        Assert.True(ActivityLogAreas.Maskinporten.AcceptsCatalogEntry(ActivityLogType.Assignment, ActivityLogSubtype.Resource));
        Assert.False(ActivityLogAreas.Maskinporten.AcceptsCatalogEntry(ActivityLogType.Assignment, ActivityLogSubtype.Package));
        Assert.False(ActivityLogAreas.Maskinporten.AcceptsCatalogEntry(ActivityLogType.Assignment, ActivityLogSubtype.Instance));
        Assert.False(ActivityLogAreas.Maskinporten.AcceptsCatalogEntry(ActivityLogType.Delegation, ActivityLogSubtype.Resource));
    }

    [Fact]
    public void AreaCatalogSubsets_CoverTheWholeCatalog()
    {
        var all = ActivityTypeConstants.AllEntities().ToList();

        List<(ActivityLogType Type, ActivityLogSubtype? Subtype)> Subset(ActivityLogArea area) =>
            [.. all.Where(d => area.AcceptsCatalogEntry(d.Entity.Type, d.Entity.Subtype)).Select(d => (d.Entity.Type, d.Entity.Subtype))];

        var connections = Subset(ActivityLogAreas.Connections);
        var request = Subset(ActivityLogAreas.Request);
        var maskinporten = Subset(ActivityLogAreas.Maskinporten);

        Assert.NotEmpty(connections);
        Assert.NotEmpty(request);
        Assert.NotEmpty(maskinporten);

        // Connections and request together cover the whole catalog; the maskinporten slice is
        // the assignment main-row and resource entries (the shapes Supplier events produce).
        Assert.Equal(all.Count, connections.Count + request.Count);
        Assert.All(maskinporten, entry => Assert.Contains(entry, connections));
        Assert.All(maskinporten, entry => Assert.True(entry.Subtype is null or ActivityLogSubtype.Resource));
    }

    [Fact]
    public void AreaFields_MatchTheAreaSemantics()
    {
        Assert.Contains(ActivityLogFilterField.Via, ActivityLogAreas.Connections.Fields);
        Assert.Contains(ActivityLogFilterField.Role, ActivityLogAreas.Connections.Fields);

        Assert.DoesNotContain(ActivityLogFilterField.Via, ActivityLogAreas.Request.Fields);
        Assert.DoesNotContain(ActivityLogFilterField.Role, ActivityLogAreas.Request.Fields);

        // The maskinporten area is the Supplier-role slice, so role is not a filter field
        // there. Each area rides its own service surface, and the area's type set IS the
        // slice's — one source, so validation and the service clamp can never disagree.
        Assert.DoesNotContain(ActivityLogFilterField.Role, ActivityLogAreas.Maskinporten.Fields);
        Assert.Equal(ActivityLogSlice.Connections, ActivityLogAreas.Connections.Slice);
        Assert.Equal(ActivityLogSlice.Request, ActivityLogAreas.Request.Slice);
        Assert.Equal(ActivityLogSlice.Maskinporten, ActivityLogAreas.Maskinporten.Slice);
        Assert.Same(ActivityLogSlices.Connections, ActivityLogAreas.Connections.Types);
        Assert.Same(ActivityLogSlices.Request, ActivityLogAreas.Request.Types);
        Assert.Same(ActivityLogSlices.Maskinporten, ActivityLogAreas.Maskinporten.Types);

        foreach (var area in new[] { ActivityLogAreas.Connections, ActivityLogAreas.Request, ActivityLogAreas.Maskinporten })
        {
            Assert.Contains(ActivityLogFilterField.From, area.Fields);
            Assert.Contains(ActivityLogFilterField.To, area.Fields);
            Assert.Contains(ActivityLogFilterField.ActivityType, area.Fields);
        }
    }
}
