using Altinn.AccessMgmt.PersistenceEF.Constants;

namespace Altinn.AccessMgmt.PersistenceEF.Tests.Constants;

public class PackageConstantsTest
{
    [Fact(Skip = "Name not unique for packages")]
    public void TryGetByAll_WithExistingUrnSuffixWithColon_ReturnsTrue()
    {
        var value = ":" + PackageConstants.Agriculture.Entity.Urn.Split(":").LastOrDefault();
        bool result = PackageConstants.TryGetByAll(value, out var package);
        Assert.True(result);
        Assert.Equal(package, PackageConstants.Agriculture);
    }

    [Fact(Skip = "Name not unique for packages")]
    public void TryGetByAll_WithExistingUrnSuffixWithoutColon_ReturnsTrue()
    {
        var value = PackageConstants.Agriculture.Entity.Urn.Split(":").LastOrDefault();
        bool result = PackageConstants.TryGetByAll(value, out var package);
        Assert.True(result);
        Assert.Equal(package, PackageConstants.Agriculture);
    }

    [Fact(Skip = "Name not unique for packages")]
    public void TryGetByAll_WithExistingUrn_ReturnsTrue()
    {
        bool result = PackageConstants.TryGetByAll(PackageConstants.Agriculture.Entity.Urn, out var package);
        Assert.True(result);
        Assert.Equal(package, PackageConstants.Agriculture);
    }

    [Fact(Skip = "Name not unique for packages")]
    public void TryGetByAll_WithExistingName_ReturnsTrue()
    {
        bool result = PackageConstants.TryGetByAll(PackageConstants.Agriculture.Entity.Name, out var package);
        Assert.True(result);
        Assert.Equal(package, PackageConstants.Agriculture);
    }

    [Fact(Skip = "Name not unique for packages")]
    public void TryGetByAll_WithExistingId_ReturnsTrue()
    {
        bool result = PackageConstants.TryGetByAll(PackageConstants.Agriculture.Entity.Id.ToString(), out var package);
        Assert.True(result);
        Assert.Equal(package, PackageConstants.Agriculture);
    }

    [Fact(Skip = "Name not unique for packages")]
    public void TryGetByAll_WithRandomId_ReturnsFalse()
    {
        bool result = PackageConstants.TryGetByAll(Guid.CreateVersion7().ToString(), out var package);
        Assert.False(result);
        Assert.Null(package);
    }

    [Fact(Skip = "Name not unique for packages")]
    public void TryGetByAll_WithNull_ReturnsFalse()
    {
        bool result = PackageConstants.TryGetByAll(null, out var package);
        Assert.False(result);
        Assert.Null(package);
    }
}
