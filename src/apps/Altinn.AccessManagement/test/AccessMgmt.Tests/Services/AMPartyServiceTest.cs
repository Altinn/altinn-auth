using System.Linq.Expressions;
using Altinn.AccessManagement.Core.Models.Party;
using Altinn.AccessMgmt.Persistence.Core.Helpers;
using Altinn.AccessMgmt.Persistence.Core.Models;
using Altinn.AccessMgmt.Persistence.Models;
using Altinn.AccessMgmt.Persistence.Repositories.Contracts;
using Altinn.AccessMgmt.Persistence.Services;
using Altinn.Authorization.Api.Contracts.Register;
using Moq;

namespace AccessMgmt.Tests.Services;

/// <summary>
/// Unit tests for <see cref="AMPartyService"/> — pure Moq, no database.
/// </summary>
public class AMPartyServiceTest
{
    private static readonly Guid PersonTypeId = Guid.Parse("bfe09e70-e868-44b3-8d81-dfe0e13e058a");
    private static readonly Guid OrgTypeId = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d");
    private static readonly Guid UnknownTypeId = Guid.NewGuid();

    private static (AMPartyService svc, Mock<IEntityLookupRepository> repo) MakeSut()
    {
        var repo = new Mock<IEntityLookupRepository>();
        repo.Setup(r => r.CreateFilterBuilder()).Returns(new GenericFilterBuilder<EntityLookup>());
        return (new AMPartyService(repo.Object), repo);
    }

    private static QueryResponse<ExtEntityLookup> Lookup(params ExtEntityLookup[] items)
        => new() { Data = items };

    private static ExtEntityLookup MakeLookup(string key, string value, Guid entityId, string name, Guid typeId, string refId = "ref")
        => new()
        {
            EntityId = entityId,
            Key = key,
            Value = value,
            Entity = new Entity { Id = entityId, Name = name, TypeId = typeId, RefId = refId }
        };

    private static void SetupFilterResult(Mock<IEntityLookupRepository> repo, QueryResponse<ExtEntityLookup> result)
    {
        repo.Setup(r => r.GetExtended(
                It.IsAny<IEnumerable<GenericFilter>>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string>()))
            .ReturnsAsync(result);
    }

    private static void SetupExpressionResult(Mock<IEntityLookupRepository> repo, QueryResponse<ExtEntityLookup> result)
    {
        repo.Setup(r => r.GetExtended(
                It.IsAny<Expression<Func<ExtEntityLookup, Guid>>>(),
                It.IsAny<Guid>(),
                It.IsAny<RequestOptions>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string>()))
            .ReturnsAsync(result);
    }

    #region GetByOrgNo

    [Fact]
    public async Task GetByOrgNo_EmptyResult_ReturnsNull()
    {
        var (svc, repo) = MakeSut();
        SetupFilterResult(repo, Lookup());

        var result = await svc.GetByOrgNo(OrganizationNumber.Parse("937884117"));

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByOrgNo_SingleResult_ReturnsMinimalPartyWithOrgId()
    {
        var entityId = Guid.NewGuid();
        var (svc, repo) = MakeSut();
        SetupFilterResult(repo, Lookup(MakeLookup("OrganizationIdentifier", "937884117", entityId, "Test Org", OrgTypeId)));

        var result = await svc.GetByOrgNo(OrganizationNumber.Parse("937884117"));

        result.Should().NotBeNull();
        result!.PartyUuid.Should().Be(entityId);
        result.OrganizationId.Should().Be("937884117");
        result.Name.Should().Be("Test Org");
    }

    [Fact]
    public async Task GetByOrgNo_MultipleResults_ThrowsInvalidOperationException()
    {
        var (svc, repo) = MakeSut();
        SetupFilterResult(repo, Lookup(
            MakeLookup("OrganizationIdentifier", "937884117", Guid.NewGuid(), "Org A", OrgTypeId),
            MakeLookup("OrganizationIdentifier", "937884117", Guid.NewGuid(), "Org B", OrgTypeId)));

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.GetByOrgNo(OrganizationNumber.Parse("937884117")));
    }

    #endregion

    #region GetByPartyId

    [Fact]
    public async Task GetByPartyId_EmptyResult_ReturnsNull()
    {
        var (svc, repo) = MakeSut();
        SetupFilterResult(repo, Lookup());

        var result = await svc.GetByPartyId(12345);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByPartyId_MultipleResults_ThrowsInvalidOperationException()
    {
        var (svc, repo) = MakeSut();
        SetupFilterResult(repo, Lookup(
            MakeLookup("PartyId", "12345", Guid.NewGuid(), "A", OrgTypeId),
            MakeLookup("PartyId", "12345", Guid.NewGuid(), "B", OrgTypeId)));

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.GetByPartyId(12345));
    }

    [Fact]
    public async Task GetByPartyId_PersonType_SetsPersonId()
    {
        var entityId = Guid.NewGuid();
        var (svc, repo) = MakeSut();
        SetupFilterResult(repo, Lookup(MakeLookup("PartyId", "99001", entityId, "Person Name", PersonTypeId, "12345678901")));

        var result = await svc.GetByPartyId(99001);

        result.Should().NotBeNull();
        result!.PersonId.Should().Be("12345678901");
        result.OrganizationId.Should().BeNull();
        result.PartyId.Should().Be(99001);
    }

    [Fact]
    public async Task GetByPartyId_OrgType_SetsOrganizationId()
    {
        var entityId = Guid.NewGuid();
        var (svc, repo) = MakeSut();
        SetupFilterResult(repo, Lookup(MakeLookup("PartyId", "99002", entityId, "Org Name", OrgTypeId, "919272567")));

        var result = await svc.GetByPartyId(99002);

        result.Should().NotBeNull();
        result!.OrganizationId.Should().Be("919272567");
        result.PersonId.Should().BeNull();
    }

    [Fact]
    public async Task GetByPartyId_UnknownType_BothIdsNull()
    {
        var entityId = Guid.NewGuid();
        var (svc, repo) = MakeSut();
        SetupFilterResult(repo, Lookup(MakeLookup("PartyId", "99003", entityId, "Unknown", UnknownTypeId, "some-ref")));

        var result = await svc.GetByPartyId(99003);

        result.Should().NotBeNull();
        result!.PersonId.Should().BeNull();
        result.OrganizationId.Should().BeNull();
    }

    #endregion

    #region GetByPersonNo

    [Fact]
    public async Task GetByPersonNo_EmptyResult_ReturnsNull()
    {
        var (svc, repo) = MakeSut();
        SetupFilterResult(repo, Lookup());

        var result = await svc.GetByPersonNo(PersonIdentifier.Parse("02013299997"));

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByPersonNo_MultipleResults_ThrowsInvalidOperationException()
    {
        var (svc, repo) = MakeSut();
        SetupFilterResult(repo, Lookup(
            MakeLookup("PersonIdentifier", "02013299997", Guid.NewGuid(), "A", PersonTypeId),
            MakeLookup("PersonIdentifier", "02013299997", Guid.NewGuid(), "B", PersonTypeId)));

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.GetByPersonNo(PersonIdentifier.Parse("02013299997")));
    }

    [Fact]
    public async Task GetByPersonNo_SingleResult_SetsPersonId()
    {
        var entityId = Guid.NewGuid();
        var (svc, repo) = MakeSut();
        SetupFilterResult(repo, Lookup(MakeLookup("PersonIdentifier", "02013299997", entityId, "John Doe", PersonTypeId)));

        var result = await svc.GetByPersonNo(PersonIdentifier.Parse("02013299997"));

        result.Should().NotBeNull();
        result!.PartyUuid.Should().Be(entityId);
        result.PersonId.Should().Be("02013299997");
        result.Name.Should().Be("John Doe");
    }

    #endregion

    #region GetByUuid

    [Fact]
    public async Task GetByUuid_EmptyResult_ReturnsNull()
    {
        var (svc, repo) = MakeSut();
        SetupExpressionResult(repo, Lookup());

        var result = await svc.GetByUuid(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByUuid_OrgIdentifierInDict_SetsOrganizationId()
    {
        var partyUuid = Guid.NewGuid();
        var (svc, repo) = MakeSut();
        SetupExpressionResult(repo, Lookup(
            MakeLookup("OrganizationIdentifier", "919272567", partyUuid, "Org Name", OrgTypeId)));

        var result = await svc.GetByUuid(partyUuid);

        result.Should().NotBeNull();
        result!.OrganizationId.Should().Be("919272567");
        result.PersonId.Should().BeNull();
    }

    [Fact]
    public async Task GetByUuid_PersonIdentifierInDict_SetsPersonId()
    {
        var partyUuid = Guid.NewGuid();
        var (svc, repo) = MakeSut();
        SetupExpressionResult(repo, Lookup(
            MakeLookup("PersonIdentifier", "02013299997", partyUuid, "John Doe", PersonTypeId)));

        var result = await svc.GetByUuid(partyUuid);

        result.Should().NotBeNull();
        result!.PersonId.Should().Be("02013299997");
        result.OrganizationId.Should().BeNull();
    }

    [Fact]
    public async Task GetByUuid_NameInDict_OverridesEntityName()
    {
        var partyUuid = Guid.NewGuid();
        var (svc, repo) = MakeSut();
        SetupExpressionResult(repo, Lookup(
            MakeLookup("Name", "Display Name Override", partyUuid, "Entity Name", UnknownTypeId)));

        var result = await svc.GetByUuid(partyUuid);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Display Name Override");
    }

    #endregion

    #region GetByUserId

    [Fact]
    public async Task GetByUserId_EmptyResult_ReturnsNull()
    {
        var (svc, repo) = MakeSut();
        SetupFilterResult(repo, Lookup());

        var result = await svc.GetByUserId(42);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByUserId_MultipleResults_ThrowsInvalidOperationException()
    {
        var (svc, repo) = MakeSut();
        SetupFilterResult(repo, Lookup(
            MakeLookup("UserId", "42", Guid.NewGuid(), "A", PersonTypeId),
            MakeLookup("UserId", "42", Guid.NewGuid(), "B", PersonTypeId)));

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.GetByUserId(42));
    }

    [Fact]
    public async Task GetByUserId_PersonType_SetsPersonId()
    {
        var entityId = Guid.NewGuid();
        var (svc, repo) = MakeSut();
        SetupFilterResult(repo, Lookup(MakeLookup("UserId", "42", entityId, "Person Name", PersonTypeId, "02013299997")));

        var result = await svc.GetByUserId(42);

        result.Should().NotBeNull();
        result!.PersonId.Should().Be("02013299997");
    }

    [Fact]
    public async Task GetByUserId_OtherType_PersonIdNull()
    {
        var entityId = Guid.NewGuid();
        var (svc, repo) = MakeSut();
        SetupFilterResult(repo, Lookup(MakeLookup("UserId", "42", entityId, "Unknown Name", UnknownTypeId, "some-ref")));

        var result = await svc.GetByUserId(42);

        result.Should().NotBeNull();
        result!.PersonId.Should().BeNull();
    }

    #endregion
}
