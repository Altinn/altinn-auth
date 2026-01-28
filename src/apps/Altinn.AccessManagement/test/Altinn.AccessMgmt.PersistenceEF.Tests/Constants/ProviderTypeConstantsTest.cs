using Altinn.AccessMgmt.PersistenceEF.Constants;

namespace Altinn.AccessMgmt.PersistenceEF.Tests.Constants;

public class ProviderTypeConstantsTest
{
    [Fact]
    public void TryGetByAll_WithExistingName_ReturnsTrue()
    {
        bool result = ProviderTypeConstants.TryGetByAll(ProviderTypeConstants.System.Entity.Name, out var providerType);
        Assert.True(result);
        Assert.Equal(providerType, ProviderTypeConstants.System);
    }

    [Fact]
    public void TryGetByAll_WithExistingId_ReturnsTrue()
    {
        bool result = ProviderTypeConstants.TryGetByAll(ProviderTypeConstants.System.Entity.Id.ToString(), out var providerType);
        Assert.True(result);
        Assert.Equal(providerType, ProviderTypeConstants.System);
    }

    [Fact]
    public void TryGetByAll_WithRandomId_ReturnsFalse()
    {
        bool result = ProviderTypeConstants.TryGetByAll(Guid.CreateVersion7().ToString(), out var providerType);
        Assert.False(result);
        Assert.Null(providerType);
    }

    [Fact]
    public void TryGetByAll_WithNull_ReturnsFalse()
    {
        bool result = ProviderTypeConstants.TryGetByAll(null, out var providerType);
        Assert.False(result);
        Assert.Null(providerType);
    }
}
