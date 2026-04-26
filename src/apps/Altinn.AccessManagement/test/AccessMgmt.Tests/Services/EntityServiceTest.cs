using System.Linq.Expressions;
using Altinn.AccessMgmt.Persistence.Core.Helpers;
using Altinn.AccessMgmt.Persistence.Core.Models;
using Altinn.AccessMgmt.Persistence.Models;
using Altinn.AccessMgmt.Persistence.Repositories.Contracts;
using Altinn.AccessMgmt.Persistence.Services;
using Moq;

namespace AccessMgmt.Tests.Services;

/// <summary>
/// Unit tests for <see cref="EntityService"/> — pure Moq, no database.
/// </summary>
public class EntityServiceTest
{
    private static (EntityService svc, Mock<IEntityRepository> entityRepo, Mock<IEntityLookupRepository> lookupRepo) MakeSut()
    {
        var entityRepo = new Mock<IEntityRepository>();
        var lookupRepo = new Mock<IEntityLookupRepository>();
        lookupRepo.Setup(r => r.CreateFilterBuilder()).Returns(new GenericFilterBuilder<EntityLookup>());
        return (new EntityService(entityRepo.Object, lookupRepo.Object), entityRepo, lookupRepo);
    }

    private static QueryResponse<ExtEntityLookup> LookupResponse(params ExtEntityLookup[] items)
        => new() { Data = items };

    private static QueryResponse<ExtEntity> ExtEntityResponse(params ExtEntity[] items)
        => new() { Data = items };

    private static ExtEntityLookup MakeLookup(Guid entityId, string key, string value, string name)
        => new()
        {
            EntityId = entityId,
            Key = key,
            Value = value,
            Entity = new Entity { Id = entityId, Name = name }
        };

    #region GetByOrgNo

    [Fact]
    public async Task GetByOrgNo_EmptyResult_ReturnsNull()
    {
        var (svc, _, lookupRepo) = MakeSut();
        lookupRepo.Setup(r => r.GetExtended(
                It.IsAny<IEnumerable<GenericFilter>>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string>()))
            .ReturnsAsync(LookupResponse());

        var result = await svc.GetByOrgNo("919272567");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByOrgNo_MultipleResults_ThrowsException()
    {
        var (svc, _, lookupRepo) = MakeSut();
        lookupRepo.Setup(r => r.GetExtended(
                It.IsAny<IEnumerable<GenericFilter>>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string>()))
            .ReturnsAsync(LookupResponse(
                MakeLookup(Guid.NewGuid(), "OrgNo", "919272567", "A"),
                MakeLookup(Guid.NewGuid(), "OrgNo", "919272567", "B")));

        await Assert.ThrowsAsync<Exception>(() => svc.GetByOrgNo("919272567"));
    }

    [Fact]
    public async Task GetByOrgNo_SingleResult_ReturnsEntity()
    {
        var entityId = Guid.NewGuid();
        var (svc, _, lookupRepo) = MakeSut();
        lookupRepo.Setup(r => r.GetExtended(
                It.IsAny<IEnumerable<GenericFilter>>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string>()))
            .ReturnsAsync(LookupResponse(MakeLookup(entityId, "OrgNo", "919272567", "Test Org")));

        var result = await svc.GetByOrgNo("919272567");

        result.Should().NotBeNull();
        result!.Id.Should().Be(entityId);
        result.Name.Should().Be("Test Org");
    }

    #endregion

    #region GetByPersNo / GetByProfile (NotImplemented)

    [Fact]
    public async Task GetByPersNo_ThrowsNotImplementedException()
    {
        var (svc, _, _) = MakeSut();

        await Assert.ThrowsAsync<NotImplementedException>(() => svc.GetByPersNo("02013299997"));
    }

    [Fact]
    public async Task GetByProfile_ThrowsNotImplementedException()
    {
        var (svc, _, _) = MakeSut();

        await Assert.ThrowsAsync<NotImplementedException>(() => svc.GetByProfile("someProfileId"));
    }

    #endregion

    #region GetChildren

    [Fact]
    public async Task GetChildren_ReturnsEntities()
    {
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var (svc, entityRepo, _) = MakeSut();
        entityRepo.Setup(r => r.GetExtended(
                It.IsAny<Expression<Func<ExtEntity, Guid?>>>(),
                It.IsAny<Guid?>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string>()))
            .ReturnsAsync(ExtEntityResponse(new ExtEntity { Id = childId, Name = "Child" }));

        var result = await svc.GetChildren(parentId);

        result.Should().ContainSingle(e => e.Id == childId);
    }

    #endregion

    #region GetParent

    [Fact]
    public async Task GetParent_ReturnsEntity()
    {
        var parentId = Guid.NewGuid();
        var (svc, entityRepo, _) = MakeSut();
        entityRepo.Setup(r => r.GetExtended(
                It.IsAny<Guid>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string>()))
            .ReturnsAsync(new ExtEntity { Id = parentId, Name = "Parent" });

        var result = await svc.GetParent(parentId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(parentId);
    }

    #endregion
}
