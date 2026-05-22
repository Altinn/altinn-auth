using Altinn.AccessMgmt.Core.Services;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.Register;
using Moq;

namespace AccessMgmt.Tests.Services;

/// <summary>
/// Unit tests for <see cref="AMPartyService"/>.
/// The service is a thin mapping layer on top of <see cref="IEntityService"/>;
/// the tests cover the TypeId-based Person/Organization branch and null
/// propagation. Heavier DB-bound logic is left to integration tests against
/// <c>EntityService</c> at the controller level.
/// </summary>
public class AMPartyServiceTest
{
    private static readonly Guid PersonTypeId = EntityTypeConstants.Person.Id;
    private static readonly Guid OrgTypeId = EntityTypeConstants.Organization.Id;
    private static readonly Guid UnknownTypeId = Guid.Parse("00000000-0000-0000-0000-000000000099");

    private static (AMPartyService svc, Mock<IEntityService> entityService) MakeSut()
    {
        var entityService = new Mock<IEntityService>();
        return (new AMPartyService(entityService.Object), entityService);
    }

    private static Entity OrgEntity(int? partyId = 12345, string orgNo = "937884117", string name = "Test Org")
        => new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            TypeId = OrgTypeId,
            PartyId = partyId,
            OrganizationIdentifier = orgNo,
        };

    private static Entity PersonEntity(int? partyId = 12345, string persNo = "02013299997", string name = "Test Person")
        => new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            TypeId = PersonTypeId,
            PartyId = partyId,
            PersonIdentifier = persNo,
        };

    private static Entity UnknownEntity()
        => new()
        {
            Id = Guid.NewGuid(),
            Name = "Other",
            TypeId = UnknownTypeId,
            PartyId = 99,
        };

    [Fact]
    public async Task GetByOrgNo_EntityNotFound_ReturnsNull()
    {
        var (svc, entityService) = MakeSut();
        entityService.Setup(s => s.GetByOrgNo(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Entity)null);

        var result = await svc.GetByOrgNo(OrganizationNumber.Parse("937884117"), TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByOrgNo_EntityFound_ReturnsPartyWithOrgId()
    {
        var entity = OrgEntity();
        var (svc, entityService) = MakeSut();
        entityService.Setup(s => s.GetByOrgNo("937884117", It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var result = await svc.GetByOrgNo(OrganizationNumber.Parse("937884117"), TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.PartyUuid.Should().Be(entity.Id);
        result.OrganizationId.Should().Be("937884117");
        result.Name.Should().Be("Test Org");
        result.PartyType.Should().Be(OrgTypeId);
    }

    [Fact]
    public async Task GetByPartyId_EntityNotFound_ReturnsNull()
    {
        var (svc, entityService) = MakeSut();
        entityService.Setup(s => s.GetByPartyId(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Entity)null);

        var result = await svc.GetByPartyId(12345, TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByPartyId_PersonType_SetsPersonIdOnly()
    {
        var entity = PersonEntity(partyId: 99001);
        var (svc, entityService) = MakeSut();
        entityService.Setup(s => s.GetByPartyId("99001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var result = await svc.GetByPartyId(99001, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.PersonId.Should().Be("02013299997");
        result.OrganizationId.Should().BeNull();
        result.PartyId.Should().Be(99001);
    }

    [Fact]
    public async Task GetByPartyId_OrgType_SetsOrganizationIdOnly()
    {
        var entity = OrgEntity(partyId: 99002, orgNo: "919272567");
        var (svc, entityService) = MakeSut();
        entityService.Setup(s => s.GetByPartyId("99002", It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var result = await svc.GetByPartyId(99002, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.OrganizationId.Should().Be("919272567");
        result.PersonId.Should().BeNull();
    }

    [Fact]
    public async Task GetByPartyId_UnknownType_BothIdsNull()
    {
        var (svc, entityService) = MakeSut();
        entityService.Setup(s => s.GetByPartyId(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnknownEntity());

        var result = await svc.GetByPartyId(99, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.PersonId.Should().BeNull();
        result.OrganizationId.Should().BeNull();
    }

    [Fact]
    public async Task GetByPersonNo_EntityNotFound_ReturnsNull()
    {
        var (svc, entityService) = MakeSut();
        entityService.Setup(s => s.GetByPersNo(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Entity)null);

        var result = await svc.GetByPersonNo(PersonIdentifier.Parse("02013299997"), TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByPersonNo_EntityFound_ReturnsPartyWithPersonId()
    {
        var entity = PersonEntity(persNo: "02013299997");
        var (svc, entityService) = MakeSut();
        entityService.Setup(s => s.GetByPersNo("02013299997", It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var result = await svc.GetByPersonNo(PersonIdentifier.Parse("02013299997"), TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.PersonId.Should().Be("02013299997");
        result.OrganizationId.Should().BeNull();
        result.PartyType.Should().Be(PersonTypeId);
    }

    [Fact]
    public async Task GetByUserId_PersonType_SetsPersonId()
    {
        var entity = PersonEntity();
        var (svc, entityService) = MakeSut();
        entityService.Setup(s => s.GetByUserId("42", It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var result = await svc.GetByUserId(42, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.PersonId.Should().Be(entity.PersonIdentifier);
        result.OrganizationId.Should().BeNull();
    }

    [Fact]
    public async Task GetByUuid_PersonType_SetsPersonIdOnly()
    {
        var entity = PersonEntity();
        var (svc, entityService) = MakeSut();
        entityService.Setup(s => s.GetEntity(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var result = await svc.GetByUuid(entity.Id, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.PersonId.Should().Be(entity.PersonIdentifier);
        result.OrganizationId.Should().BeNull();
    }

    [Fact]
    public async Task GetByUuid_OrgType_SetsOrganizationIdOnly()
    {
        var entity = OrgEntity();
        var (svc, entityService) = MakeSut();
        entityService.Setup(s => s.GetEntity(entity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        var result = await svc.GetByUuid(entity.Id, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.OrganizationId.Should().Be(entity.OrganizationIdentifier);
        result.PersonId.Should().BeNull();
    }

    [Fact]
    public async Task GetByUuid_EntityNotFound_ReturnsNull()
    {
        var (svc, entityService) = MakeSut();
        entityService.Setup(s => s.GetEntity(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Entity)null);

        var result = await svc.GetByUuid(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }
}
