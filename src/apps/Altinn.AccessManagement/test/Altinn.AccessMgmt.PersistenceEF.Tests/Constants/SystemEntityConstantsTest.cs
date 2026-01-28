using Altinn.AccessMgmt.PersistenceEF.Constants;

namespace Altinn.AccessMgmt.PersistenceEF.Tests.Constants;

public class SystemEntityConstantsTest
{
    [Fact]
    public void TryGetByAll_WithExistingName_ReturnsTrue()
    {
        bool result = SystemEntityConstants.TryGetByAll(SystemEntityConstants.EnduserApi.Entity.Name, out var entity);
        Assert.True(result);
        Assert.Equal(entity, SystemEntityConstants.EnduserApi);
    }

    [Fact]
    public void TryGetByAll_WithExistingId_ReturnsTrue()
    {
        bool result = SystemEntityConstants.TryGetByAll(SystemEntityConstants.EnduserApi.Id.ToString(), out var entity);
        Assert.True(result);
        Assert.Equal(entity, SystemEntityConstants.EnduserApi);
    }

    [Fact]
    public void TryGetByAll_WithRandomId_ReturnsFalse()
    {
        bool result = SystemEntityConstants.TryGetByAll(Guid.CreateVersion7().ToString(), out var entity);
        Assert.False(result);
        Assert.Null(entity);
    }

    [Fact]
    public void TryGetByAll_WithNull_ReturnsFalse()
    {
        bool result = SystemEntityConstants.TryGetByAll(null, out var entity);
        Assert.False(result);
        Assert.Null(entity);
    }
}
