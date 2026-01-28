using Altinn.AccessMgmt.PersistenceEF.Constants;

namespace Altinn.AccessMgmt.PersistenceEF.Tests.Constants;

public class RoleConstantsTest
{
    [Fact]
    public void TryGetByAll_WithExistingUrn_ReturnsTrue()
    {
        bool result = RoleConstants.TryGetByAll(RoleConstants.Agent.Entity.Urn, out var role);
        Assert.True(result);
        Assert.Equal(role, RoleConstants.Agent);
    }

    [Fact]
    public void TryGetByAll_WithExistingCode_ReturnsTrue()
    {
        bool result = RoleConstants.TryGetByAll(RoleConstants.Agent.Entity.Code, out var role);
        Assert.True(result);
        Assert.Equal(role, RoleConstants.Agent);
    }

    [Fact]
    public void TryGetByAll_WithExistingLegacyCode_ReturnsTrue()
    {
        bool result = RoleConstants.TryGetByAll(RoleConstants.ManagingDirector.Entity.LegacyCode, out var role);
        Assert.True(result);
        Assert.Equal(role, RoleConstants.ManagingDirector);
    }

    [Fact]
    public void TryGetByAll_WithExistingId_ReturnsTrue()
    {
        bool result = RoleConstants.TryGetByAll(RoleConstants.Agent.Id.ToString(), out var role);
        Assert.True(result);
        Assert.Equal(role, RoleConstants.Agent);
    }

    [Fact]
    public void TryGetByAll_WithRandomId_ReturnsFalse()
    {
        bool result = RoleConstants.TryGetByAll(Guid.CreateVersion7().ToString(), out var role);
        Assert.False(result);
        Assert.Null(role);
    }

    [Fact]
    public void TryGetByAll_WithNull_ReturnsFalse()
    {
        bool result = RoleConstants.TryGetByAll(null, out var role);
        Assert.False(result);
        Assert.Null(role);
    }
}
