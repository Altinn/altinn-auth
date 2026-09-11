using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessMgmt.Core;
using Altinn.AccessMgmt.Core.Services;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.AccessMgmt.Core.Tests.Integration.Services;

/// <summary>
/// Integration tests for <see cref="ConnectionService.Get"/> covering ADOS subunit inheritance.
/// Reproduces the reported Connection API gap: the Daglig-leder (ManagingDirector) of a mainunit
/// must inherit roles and access packages for the mainunit's ADOS subunit, surfaced as a nested
/// sub-connection, exactly like BEDR/AAFY subunits. The ADOS subunit inheritance feature flag is
/// ENABLED for these tests.
/// </summary>
[IntegrationTest]
public class ConnectionServiceGetAdosSubunitTest : IClassFixture<ApiFixture>
{
    private static readonly Entity MainUnit = new()
    {
        Id = Guid.Parse("0196b150-0000-7000-8000-000000000001"),
        TypeId = EntityTypeConstants.Organization,
        VariantId = EntityVariantConstants.ORGL,
        Name = "ADOS Mainunit Api",
        OrganizationIdentifier = "399950081",
        RefId = "399950081",
        PartyId = 50950081,
    };

    private static readonly Entity SubUnit = new()
    {
        Id = Guid.Parse("0196b150-0000-7000-8000-000000000002"),
        TypeId = EntityTypeConstants.Organization,
        VariantId = EntityVariantConstants.ADOS,
        Name = "ADOS Subunit Api",
        OrganizationIdentifier = "399950082",
        RefId = "399950082",
        ParentId = Guid.Parse("0196b150-0000-7000-8000-000000000001"),
        PartyId = 50950082,
    };

    private static readonly Entity ManagingDirector = new()
    {
        Id = Guid.Parse("0196b150-0000-7000-8000-000000000003"),
        TypeId = EntityTypeConstants.Person,
        VariantId = EntityVariantConstants.Person,
        Name = "Ada Direktor",
        PersonIdentifier = "24019099950",
        RefId = "24019099950",
        PartyId = 50950083,
        UserId = 50950083,
        DateOfBirth = new DateOnly(1990, 1, 24),
    };

    public ConnectionServiceGetAdosSubunitTest(ApiFixture fixture)
    {
        Fixture = fixture;
        Fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.AdosSubunitInheritance);
        Fixture.EnsureSeedOnce<ConnectionServiceGetAdosSubunitTest>(db =>
        {
            db.Entities.AddRange(MainUnit, SubUnit, ManagingDirector);
            db.SaveChanges();

            // Ada is Managing Director (DAGL) of the ADOS mainunit.
            db.Assignments.Add(new Assignment()
            {
                FromId = MainUnit.Id,
                ToId = ManagingDirector.Id,
                RoleId = RoleConstants.ManagingDirector,
            });

            db.SaveChanges();
        });
    }

    public ApiFixture Fixture { get; }

    [Fact]
    public async Task Get_FromOthers_AdosInheritanceEnabled_AdosSubunitInheritsManagingDirectorRolesAndPackages()
    {
        using var scope = Fixture.Services.CreateScope();
        var service = (ConnectionService)scope.ServiceProvider.GetRequiredService<IConnectionService>();

        var result = await service.Get(
            party: ManagingDirector.Id,
            fromId: null,
            toId: ManagingDirector.Id,
            includeAccessPackages: true,
            configureConnections: options =>
            {
                options.AllowedReadToEntityTypes = [EntityTypeConstants.Organization, EntityTypeConstants.Person, EntityTypeConstants.SystemUser];
            },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(result.IsProblem);

        var connections = result.Value.ToList();

        // The mainunit appears as a top-level connection the person receives access from.
        var mainUnit = Assert.Single(connections, c => c.Party.Id == MainUnit.Id);

        // The ADOS subunit is nested as an inherited sub-connection of the mainunit.
        var subUnit = Assert.Single(mainUnit.Connections, c => c.Party.Id == SubUnit.Id);

        // The inherited ManagingDirector access on the ADOS subunit must carry its access packages.
        Assert.NotEmpty(subUnit.Packages);
    }
}
