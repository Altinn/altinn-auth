using Altinn.AccessManagement.Tests.Fixtures;
using Altinn.AccessMgmt.Core.Services;
using Altinn.AccessMgmt.PersistenceEF.Audit;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.Party;
using Microsoft.EntityFrameworkCore;

namespace AccessMgmt.Tests.Services;

/// <summary>
/// Integration tests for <see cref="PartyService.AddParty"/> using
/// <see cref="PostgresFixture"/>. The service interacts with
/// <see cref="AppDbContext"/> directly, so mock-based unit tests are not
/// meaningful; these tests verify the validation branches against a real
/// (seeded) database.
/// </summary>
public class PartyServiceTest : IClassFixture<PostgresFixture>
{
    private readonly AppDbContext _db;
    private readonly PartyService _service;

    public PartyServiceTest(PostgresFixture fixture)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(fixture.SharedDb.Admin.ToString())
            .Options;

        _db = new AppDbContext(options)
        {
            AuditAccessor = new AuditAccessor
            {
                AuditValues = new AuditValues(
                    changedBy: Guid.NewGuid(),
                    changedBySystem: SystemEntityConstants.StaticDataIngest),
            },
        };

        _service = new PartyService(_db);
    }

    private static PartyBaseDto SystemUserParty(Guid? id = null, string variant = "StandardSystem")
        => new()
        {
            PartyUuid = id ?? Guid.CreateVersion7(),
            EntityType = "Systembruker",
            EntityVariantType = variant,
            DisplayName = "Integration Test Party",
        };

    [Fact]
    public async Task AddParty_EntityAlreadyExists_ReturnsSuccess_PartyCreatedFalse()
    {
        var existingId = Guid.CreateVersion7();
        _db.Entities.Add(new Entity
        {
            Id = existingId,
            Name = "Existing",
            TypeId = EntityTypeConstants.SystemUser,
            VariantId = EntityVariantConstants.StandardSystem,
            RefId = existingId.ToString(),
        });
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _service.AddParty(
            SystemUserParty(existingId),
            TestContext.Current.CancellationToken);

        result.IsProblem.Should().BeFalse();
        result.Value.PartyUuid.Should().Be(existingId);
        result.Value.PartyCreated.Should().BeFalse();
    }

    [Fact]
    public async Task AddParty_UnsupportedEntityType_ReturnsProblem()
    {
        var party = SystemUserParty();
        party.EntityType = "Organisation";

        var result = await _service.AddParty(party, TestContext.Current.CancellationToken);

        result.IsProblem.Should().BeTrue();
        result.Problem!.ErrorCode.ToString().Should().Be(
            Altinn.AccessManagement.Core.Errors.Problems.UnsupportedEntityType.ErrorCode.ToString());
    }

    [Fact]
    public async Task AddParty_EntityTypeNotFound_ReturnsProblem()
    {
        // "systembruker" passes the case-insensitive check but the DB lookup
        // is case-sensitive against the seeded "Systembruker", so it misses.
        var party = SystemUserParty();
        party.EntityType = "systembruker";

        var result = await _service.AddParty(party, TestContext.Current.CancellationToken);

        result.IsProblem.Should().BeTrue();
        result.Problem!.ErrorCode.ToString().Should().Be(
            Altinn.AccessManagement.Core.Errors.Problems.EntityTypeNotFound.ErrorCode.ToString());
    }

    [Fact]
    public async Task AddParty_EntityVariantNotFound_ReturnsProblem()
    {
        var party = SystemUserParty(variant: "NoSuchVariant_xyz123");

        var result = await _service.AddParty(party, TestContext.Current.CancellationToken);

        result.IsProblem.Should().BeTrue();
        result.Problem!.ErrorCode.ToString().Should().Be(
            Altinn.AccessManagement.Core.Errors.Problems.EntityVariantNotFoundOrInvalid.ErrorCode.ToString());
    }

    [Fact]
    public async Task AddParty_ValidSystemUser_CreatesEntity_ReturnsPartyCreatedTrue()
    {
        var party = SystemUserParty();

        var result = await _service.AddParty(party, TestContext.Current.CancellationToken);

        result.IsProblem.Should().BeFalse();
        result.Value.PartyUuid.Should().Be(party.PartyUuid);
        result.Value.PartyCreated.Should().BeTrue();

        var inserted = await _db.Entities
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == party.PartyUuid, TestContext.Current.CancellationToken);
        inserted.Should().NotBeNull();
        inserted!.TypeId.Should().Be(EntityTypeConstants.SystemUser);
        inserted.VariantId.Should().Be(EntityVariantConstants.StandardSystem);
    }
}
