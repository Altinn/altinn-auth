using Altinn.AccessMgmt.PersistenceEF.Constants;

namespace Altinn.AccessMgmt.PersistenceEF.Tests.Constants;

public class ProviderConstantsTest
{
    [Fact]
    public void TryGetByAll_WithExistingName_ReturnsTrue()
    {
        bool result = ProviderConstants.TryGetByAll(ProviderConstants.Altinn3.Entity.Name, out var provider);
        Assert.True(result);
        Assert.Equal(provider, ProviderConstants.Altinn3);
    }

    [Fact]
    public void TryGetByAll_WithExistingId_ReturnsTrue()
    {
        bool result = ProviderConstants.TryGetByAll(ProviderConstants.Altinn3.Entity.Id.ToString(), out var provider);
        Assert.True(result);
        Assert.Equal(provider, ProviderConstants.Altinn3);
    }

    [Fact]
    public void TryGetByAll_WithRandomId_ReturnsFalse()
    {
        bool result = ProviderConstants.TryGetByAll(Guid.CreateVersion7().ToString(), out var provider);
        Assert.False(result);
        Assert.Null(provider);
    }

    [Fact]
    public void TryGetByAll_WithNull_ReturnsFalse()
    {
        bool result = ProviderConstants.TryGetByAll(null, out var provider);
        Assert.False(result);
        Assert.Null(provider);
    }
}
