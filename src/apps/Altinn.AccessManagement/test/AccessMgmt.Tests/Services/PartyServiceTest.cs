using System.Linq.Expressions;
using Altinn.AccessManagement.Core.Models.Party;
using Altinn.AccessMgmt.Persistence.Core.Models;
using Altinn.AccessMgmt.Persistence.Models;
using Altinn.AccessMgmt.Persistence.Repositories.Contracts;
using Altinn.AccessMgmt.Persistence.Services;
using Altinn.AccessMgmt.Persistence.Services.Models;
using Altinn.Authorization.ProblemDetails;
using Moq;

namespace AccessMgmt.Tests.Services;

/// <summary>
/// Unit tests for <see cref="PartyService"/> — pure Moq, no database.
/// </summary>
public class PartyServiceTest
{
    private static readonly Guid EntityTypeId = Guid.NewGuid();
    private static readonly Guid EntityVariantId = Guid.NewGuid();

    private record Mocks(
        Mock<IEntityRepository> EntityRepo,
        Mock<IEntityTypeRepository> TypeRepo,
        Mock<IEntityVariantRepository> VariantRepo);

    private static (PartyService svc, Mocks mocks) MakeSut()
    {
        var mocks = new Mocks(
            new Mock<IEntityRepository>(),
            new Mock<IEntityTypeRepository>(),
            new Mock<IEntityVariantRepository>());
        var svc = new PartyService(mocks.EntityRepo.Object, mocks.TypeRepo.Object, mocks.VariantRepo.Object);
        return (svc, mocks);
    }

    private static QueryResponse<T> Response<T>(params T[] items) => new() { Data = items };

    private static PartyBaseInternal MakeParty(string entityType = "Systembruker", string variantType = "Default")
        => new()
        {
            PartyUuid = Guid.NewGuid(),
            EntityType = entityType,
            EntityVariantType = variantType,
            DisplayName = "Test Party"
        };

    private static void SetupEntityExists(Mock<IEntityRepository> repo, bool exists)
    {
        var data = exists
            ? new[] { new Entity { Id = Guid.NewGuid() } }
            : Array.Empty<Entity>();

        repo.Setup(r => r.Get(
                It.IsAny<Expression<Func<Entity, Guid>>>(),
                It.IsAny<Guid>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string>()))
            .ReturnsAsync(Response(data));
    }

    private static void SetupEntityType(Mock<IEntityTypeRepository> repo, EntityType type)
    {
        var data = type != null ? new[] { type } : Array.Empty<EntityType>();
        repo.Setup(r => r.Get(
                It.IsAny<Expression<Func<EntityType, string>>>(),
                It.IsAny<string>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string>()))
            .ReturnsAsync(Response(data));
    }

    private static void SetupEntityVariant(Mock<IEntityVariantRepository> repo, EntityVariant variant)
    {
        var data = variant != null ? new[] { variant } : Array.Empty<EntityVariant>();
        repo.Setup(r => r.Get(
                It.IsAny<Expression<Func<EntityVariant, Guid>>>(),
                It.IsAny<Guid>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string>()))
            .ReturnsAsync(Response(data));
    }

    #region AddParty

    [Fact]
    public async Task AddParty_EntityAlreadyExists_ReturnsSuccessWithPartyCreatedFalse()
    {
        var (svc, mocks) = MakeSut();
        SetupEntityExists(mocks.EntityRepo, exists: true);

        var result = await svc.AddParty(MakeParty(), options: null, cancellationToken: TestContext.Current.CancellationToken);

        result.IsProblem.Should().BeFalse();
        result.Value.PartyCreated.Should().BeFalse();
    }

    [Fact]
    public async Task AddParty_EntityNotExists_UnsupportedType_ReturnsProblem()
    {
        var (svc, mocks) = MakeSut();
        SetupEntityExists(mocks.EntityRepo, exists: false);

        var result = await svc.AddParty(MakeParty(entityType: "Organisation"), options: null, cancellationToken: TestContext.Current.CancellationToken);

        result.IsProblem.Should().BeTrue();
    }

    [Fact]
    public async Task AddParty_EntityNotExists_TypeNotFound_ReturnsProblem()
    {
        var (svc, mocks) = MakeSut();
        SetupEntityExists(mocks.EntityRepo, exists: false);
        SetupEntityType(mocks.TypeRepo, type: null);

        var result = await svc.AddParty(MakeParty(), options: null, cancellationToken: TestContext.Current.CancellationToken);

        result.IsProblem.Should().BeTrue();
    }

    [Fact]
    public async Task AddParty_EntityNotExists_VariantNotFound_ReturnsProblem()
    {
        var (svc, mocks) = MakeSut();
        SetupEntityExists(mocks.EntityRepo, exists: false);
        SetupEntityType(mocks.TypeRepo, new EntityType { Id = EntityTypeId, Name = "Systembruker" });
        SetupEntityVariant(mocks.VariantRepo, variant: null);

        var result = await svc.AddParty(MakeParty(), options: null, cancellationToken: TestContext.Current.CancellationToken);

        result.IsProblem.Should().BeTrue();
    }

    [Fact]
    public async Task AddParty_EntityNotExists_AllValid_ReturnsSuccessWithPartyCreatedTrue()
    {
        var (svc, mocks) = MakeSut();
        var party = MakeParty();

        SetupEntityExists(mocks.EntityRepo, exists: false);
        SetupEntityType(mocks.TypeRepo, new EntityType { Id = EntityTypeId, Name = "Systembruker" });
        SetupEntityVariant(mocks.VariantRepo, new EntityVariant { Id = EntityVariantId, TypeId = EntityTypeId, Name = "Default" });

        mocks.EntityRepo.Setup(r => r.Create(
                It.IsAny<Entity>(),
                It.IsAny<ChangeRequestOptions>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string>()))
            .ReturnsAsync(1);

        var result = await svc.AddParty(party, options: null, cancellationToken: TestContext.Current.CancellationToken);

        result.IsProblem.Should().BeFalse();
        result.Value.PartyCreated.Should().BeTrue();
        result.Value.PartyUuid.Should().Be(party.PartyUuid);
    }

    #endregion
}
