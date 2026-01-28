using Altinn.AccessMgmt.PersistenceEF.Constants;

namespace Altinn.AccessMgmt.PersistenceEF.Tests.Constants;

public class AreaConstantsTest
{
    [Fact]
    public void TryGetByAll_WithExistingName_ReturnsTrue()
    {
        bool result = AreaConstants.TryGetByAll(AreaConstants.Personnel.Entity.Name, out var area);
        Assert.True(result);
        Assert.Equal(area, AreaConstants.Personnel);
    }

    [Fact]
    public void TryGetByAll_WithExistingId_ReturnsTrue()
    {
        bool result = AreaConstants.TryGetByAll(AreaConstants.Personnel.Entity.Id.ToString(), out var area);
        Assert.True(result);
        Assert.Equal(area, AreaConstants.Personnel);
    }

    [Fact]
    public void TryGetByAll_WithRandomId_ReturnsFalse()
    {
        bool result = AreaConstants.TryGetByAll(Guid.CreateVersion7().ToString(), out var area);
        Assert.False(result);
        Assert.Null(area);
    }

    [Fact]
    public void TryGetByAll_WithNull_ReturnsFalse()
    {
        bool result = AreaConstants.TryGetByAll(null, out var area);
        Assert.False(result);
        Assert.Null(area);
    }
}
