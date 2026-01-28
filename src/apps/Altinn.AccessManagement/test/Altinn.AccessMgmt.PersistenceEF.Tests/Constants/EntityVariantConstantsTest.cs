using Altinn.AccessMgmt.PersistenceEF.Constants;

namespace Altinn.AccessMgmt.PersistenceEF.Tests.Constants;

public class EntityVariantConstantsTest
{
    [Fact]
    public void TryGetByAll_WithExistingName_ReturnsTrue()
    {
        bool result = EntityTypeConstants.TryGetByAll(EntityTypeConstants.Person.Entity.Name, out var entityType);
        Assert.True(result);
        Assert.Equal(entityType, EntityTypeConstants.Person);
    }

    [Fact]
    public void TryGetByAll_WithExistingId_ReturnsTrue()
    {
        bool result = EntityTypeConstants.TryGetByAll(EntityTypeConstants.Person.Entity.Id.ToString(), out var entityType);
        Assert.True(result);
        Assert.Equal(entityType, EntityTypeConstants.Person);
    }

    [Fact]
    public void TryGetByAll_WithRandomId_ReturnsFalse()
    {
        bool result = EntityTypeConstants.TryGetByAll(Guid.CreateVersion7().ToString(), out var entityType);
        Assert.False(result);
        Assert.Null(entityType);
    }

    [Fact]
    public void TryGetByAll_WithNull_ReturnsFalse()
    {
        bool result = EntityVariantConstants.TryGetByAll(null, out var entityVariant);
        Assert.False(result);
        Assert.Null(entityVariant);
    }
}
