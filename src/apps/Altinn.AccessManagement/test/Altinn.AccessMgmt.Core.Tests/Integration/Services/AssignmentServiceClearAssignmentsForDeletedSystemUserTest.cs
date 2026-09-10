using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.AccessMgmt.Core.Tests.Integration.Services;

/// <summary>
/// Integration tests for <see cref="Altinn.AccessMgmt.Core.Services.AssignmentService.ClearAssignmentsForDeletedSystemUser"/>.
/// </summary>
[IntegrationTest]
public class AssignmentServiceClearAssignmentsForDeletedSystemUserTest : IClassFixture<ApiFixture>
{
    private static readonly AuditValues TestAudit = new(SystemEntityConstants.StaticDataIngest, SystemEntityConstants.StaticDataIngest);

    private static readonly Entity DeletedSystemUser = new()
    {
        Id = Guid.Parse("01970002-0000-7000-8000-000000000001"),
        TypeId = EntityTypeConstants.SystemUser,
        VariantId = EntityVariantConstants.StandardSystem,
        Name = "Deleted System User",
        RefId = "DELETED-SYSTEMUSER-TEST-01",
        IsDeleted = true,
    };

    private static readonly Entity DeletedAgentSystemUser = new()
    {
        Id = Guid.Parse("01970002-0000-7000-8000-000000000002"),
        TypeId = EntityTypeConstants.SystemUser,
        VariantId = EntityVariantConstants.AgentSystem,
        Name = "Deleted Agent System User",
        RefId = "DELETED-SYSTEMUSER-TEST-02",
        IsDeleted = true,
    };

    private static readonly Entity ActiveSystemUser = new()
    {
        Id = Guid.Parse("01970002-0000-7000-8000-000000000003"),
        TypeId = EntityTypeConstants.SystemUser,
        VariantId = EntityVariantConstants.StandardSystem,
        Name = "Active System User",
        RefId = "DELETED-SYSTEMUSER-TEST-03",
        IsDeleted = false,
    };

    private static readonly Entity Organization = new()
    {
        Id = Guid.Parse("01970002-0000-7000-8000-000000000004"),
        TypeId = EntityTypeConstants.Organization,
        VariantId = EntityVariantConstants.AS,
        Name = "Deleted SystemUser Test Org",
        RefId = "DELETED-SYSTEMUSER-TEST-ORG-01",
    };

    private static readonly Entity Facilitator = new()
    {
        Id = Guid.Parse("01970002-0000-7000-8000-000000000005"),
        TypeId = EntityTypeConstants.Organization,
        VariantId = EntityVariantConstants.AS,
        Name = "Deleted SystemUser Test Facilitator",
        RefId = "DELETED-SYSTEMUSER-TEST-ORG-02",
    };

    private static readonly Entity Person = new()
    {
        Id = Guid.Parse("01970002-0000-7000-8000-000000000006"),
        TypeId = EntityTypeConstants.Person,
        VariantId = EntityVariantConstants.Person,
        Name = "Deleted SystemUser Test Person",
        RefId = "DELETED-SYSTEMUSER-TEST-PERSON-01",
    };

    private ApiFixture Fixture { get; }

    public AssignmentServiceClearAssignmentsForDeletedSystemUserTest(ApiFixture fixture)
    {
        Fixture = fixture;
        Fixture.EnsureSeedOnce<AssignmentServiceClearAssignmentsForDeletedSystemUserTest>(db =>
        {
            db.Entities.AddRange(DeletedSystemUser, DeletedAgentSystemUser, ActiveSystemUser, Organization, Facilitator, Person);
            db.SaveChanges(TestAudit);
        });
    }

    private IAssignmentService ResolveService(IServiceScope scope)
    {
        return scope.ServiceProvider.GetRequiredService<IAssignmentService>();
    }

    private async Task<Guid> AddAssignment(Guid fromId, Guid toId, Guid roleId)
    {
        var assignmentId = Guid.CreateVersion7();
        await Fixture.QueryDb(async db =>
        {
            db.Assignments.Add(new Assignment { Id = assignmentId, FromId = fromId, ToId = toId, RoleId = roleId });
            await db.SaveChangesAsync(TestAudit, TestContext.Current.CancellationToken);
        });

        return assignmentId;
    }

    private async Task<Assignment> FindAssignment(Guid assignmentId)
    {
        Assignment result = null;
        await Fixture.QueryDb(async db =>
        {
            result = await db.Assignments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == assignmentId, TestContext.Current.CancellationToken);
        });

        return result;
    }

    [Fact]
    public async Task RemovesRightholderAssignment_WhenSystemUserIsTo()
    {
        var assignmentId = await AddAssignment(Organization.Id, DeletedSystemUser.Id, RoleConstants.Rightholder);

        using var scope = Fixture.Services.CreateScope();
        var removed = await ResolveService(scope).ClearAssignmentsForDeletedSystemUser(DeletedSystemUser.Id, TestAudit, TestContext.Current.CancellationToken);

        Assert.Equal(1, removed);
        Assert.Null(await FindAssignment(assignmentId));
    }

    [Fact]
    public async Task RemovesAgentAssignmentAndClientDelegation_WhenSystemUserIsAgent()
    {
        var clientAssignmentId = await AddAssignment(Organization.Id, Facilitator.Id, RoleConstants.Rightholder);
        var agentAssignmentId = await AddAssignment(Facilitator.Id, DeletedAgentSystemUser.Id, RoleConstants.Agent);
        var delegationId = Guid.CreateVersion7();
        await Fixture.QueryDb(async db =>
        {
            db.Delegations.Add(new Delegation { Id = delegationId, FromId = clientAssignmentId, ToId = agentAssignmentId, FacilitatorId = Facilitator.Id });
            await db.SaveChangesAsync(TestAudit, TestContext.Current.CancellationToken);
        });

        using var scope = Fixture.Services.CreateScope();
        await ResolveService(scope).ClearAssignmentsForDeletedSystemUser(DeletedAgentSystemUser.Id, TestAudit, TestContext.Current.CancellationToken);

        Assert.Null(await FindAssignment(agentAssignmentId));
        Assert.NotNull(await FindAssignment(clientAssignmentId));
        await Fixture.QueryDb(async db =>
        {
            var delegation = await db.Delegations.AsNoTracking().FirstOrDefaultAsync(d => d.Id == delegationId, TestContext.Current.CancellationToken);
            Assert.Null(delegation);
        });
    }

    [Fact]
    public async Task DoesNotRemoveAssignmentsForOtherSystemUsers()
    {
        // Facilitator as from-party keeps the (from, to, role) key distinct from the cleanup test, which leaves its active-user row behind.
        var assignmentId = await AddAssignment(Facilitator.Id, ActiveSystemUser.Id, RoleConstants.Rightholder);

        using var scope = Fixture.Services.CreateScope();
        await ResolveService(scope).ClearAssignmentsForDeletedSystemUser(DeletedSystemUser.Id, TestAudit, TestContext.Current.CancellationToken);

        Assert.NotNull(await FindAssignment(assignmentId));
    }

    [Fact]
    public async Task ReturnsZero_WhenNoAssignmentsExist()
    {
        using var scope = Fixture.Services.CreateScope();
        var removed = await ResolveService(scope).ClearAssignmentsForDeletedSystemUser(Guid.CreateVersion7(), TestAudit, TestContext.Current.CancellationToken);

        Assert.Equal(0, removed);
    }
}
