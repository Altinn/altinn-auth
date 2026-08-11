using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessManagement.TestUtils.Mocks;
using Altinn.AccessMgmt.Core.Services;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.AccessMgmt.Core.Tests.Integration.Services;

/// <summary>
/// Integration tests for <see cref="ConnectionService.GetResourceRights"/> covering the cases the public
/// endpoints cannot reach. Every endpoint resolves a concrete resource before calling in, so a result set
/// spanning several resources only occurs when the resource filter is left open, which is what these tests do.
/// </summary>
[IntegrationTest]
public class ConnectionServiceGetResourceRightsTest : IClassFixture<ApiFixture>
{
    private static readonly Entity Organization = new()
    {
        Id = Guid.Parse("0196b140-0000-7000-8000-000000000001"),
        TypeId = EntityTypeConstants.Organization,
        VariantId = EntityVariantConstants.AS,
        Name = "Skogly Kolonial AS",
        OrganizationIdentifier = "399950071",
        RefId = "399950071",
        PartyId = 50950071,
    };

    private static readonly Entity Rightholder = new()
    {
        Id = Guid.Parse("0196b140-0000-7000-8000-000000000002"),
        TypeId = EntityTypeConstants.Person,
        VariantId = EntityVariantConstants.Person,
        Name = "Frida Skogly",
        PersonIdentifier = "24019099938",
        RefId = "24019099938",
        PartyId = 50950072,
        UserId = 50950072,
        DateOfBirth = new DateOnly(1990, 1, 24),
    };

    public ConnectionServiceGetResourceRightsTest(ApiFixture fixture)
    {
        Fixture = fixture;
        Fixture.ConfigureServices(services =>
        {
            services.AddSingleton<IPolicyRetrievalPoint, PolicyRetrievalPointMock>();
        });
        Fixture.EnsureSeedOnce<ConnectionServiceGetResourceRightsTest>(db =>
        {
            db.Entities.AddRange(Organization, Rightholder);
            db.SaveChanges();

            var rightholderAssignment = new Assignment()
            {
                FromId = Organization.Id,
                ToId = Rightholder.Id,
                RoleId = RoleConstants.Rightholder,
            };

            db.Assignments.Add(rightholderAssignment);
            db.SaveChanges();

            db.AssignmentResources.Add(new AssignmentResource()
            {
                AssignmentId = rightholderAssignment.Id,
                ResourceId = TestData.SiriusSkattemelding.Id,
                PolicyPath = "sirius-skattemelding-v1/50083510/p50155461/delegationpolicy.xml",
                PolicyVersion = "1.0",
            });

            db.AssignmentResources.Add(new AssignmentResource()
            {
                AssignmentId = rightholderAssignment.Id,
                ResourceId = TestData.MattilsynetBakeryService.Id,
                PolicyPath = "mattilsynet-baker-konditorvare/50315678/p5049963/delegationpolicy.xml",
                PolicyVersion = "1.0",
            });

            db.SaveChanges();
        });
    }

    public ApiFixture Fixture { get; }

    [Fact]
    public async Task GetResourceRights_ForTwoResourcesHeldByTheSameRightholder_ReturnsRightsDescribingEachResource()
    {
        using var scope = Fixture.Services.CreateScope();
        var service = (ConnectionService)scope.ServiceProvider.GetRequiredService<IConnectionService>();

        List<ResourceRightDto> result = await service.GetResourceRights(
            fromId: Organization.Id,
            toId: Rightholder.Id,
            resourceId: null,
            roleId: RoleConstants.Rightholder.Id,
            includeClientDelegations: false,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);

        ResourceRightDto skattemelding = Assert.Single(result, r => r.Resource.Id == TestData.SiriusSkattemelding.Id);
        Assert.Equal(TestData.SiriusSkattemelding.RefId, skattemelding.Resource.RefId);
        Assert.NotEmpty(skattemelding.Rights);

        ResourceRightDto bakery = Assert.Single(result, r => r.Resource.Id == TestData.MattilsynetBakeryService.Id);
        Assert.Equal(TestData.MattilsynetBakeryService.RefId, bakery.Resource.RefId);
        Assert.NotEmpty(bakery.Rights);

        // Each entry carries only its own resource's rights; the two policies have no right key in common.
        Assert.Empty(skattemelding.Rights.Select(r => r.Right.Key).Intersect(bakery.Rights.Select(r => r.Right.Key)));
    }
}
