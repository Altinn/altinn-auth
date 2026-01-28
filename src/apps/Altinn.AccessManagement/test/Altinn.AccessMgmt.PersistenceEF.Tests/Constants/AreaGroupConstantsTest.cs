using Altinn.AccessMgmt.PersistenceEF.Constants;

namespace Altinn.AccessMgmt.PersistenceEF.Tests.Constants;

public class AreaGroupConstantsTest
{
    [Fact]
    public void TryGetByAll_WithExistingName_ReturnsTrue()
    {
        bool result = AreaGroupConstants.TryGetByAll(AreaGroupConstants.Industry.Entity.Name, out var areaGroup);
        Assert.True(result);
        Assert.Equal(areaGroup, AreaGroupConstants.Industry);
    }

    [Fact]
    public void TryGetByAll_WithExistingId_ReturnsTrue()
    {
        bool result = AreaGroupConstants.TryGetByAll(AreaGroupConstants.Industry.Entity.Id.ToString(), out var areaGroup);
        Assert.True(result);
        Assert.Equal(areaGroup, AreaGroupConstants.Industry);
    }

    [Fact]
    public void TryGetByAll_WithRandomId_ReturnsFalse()
    {
        bool result = AreaGroupConstants.TryGetByAll(Guid.CreateVersion7().ToString(), out var areaGroup);
        Assert.False(result);
        Assert.Null(areaGroup);
    }

    [Fact]
    public void TryGetByAll_WithNull_ReturnsFalse()
    {
        bool result = AreaGroupConstants.TryGetByAll(null, out var areaGroup);
        Assert.False(result);
        Assert.Null(areaGroup);
    }
}
