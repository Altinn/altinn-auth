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

    private static readonly Entity Organization = new()
    {
        Id = Guid.Parse("01970002-0000-7000-8000-000000000004"),
        TypeId = EntityTypeConstants.Organization,
        VariantId = EntityVariantConstants.AS,
        Name = "Deleted SystemUser Test Org",
        RefId = "DELETED-SYSTEMUSER-TEST-ORG-01",
    };

    private ApiFixture Fixture { get; }

    public AssignmentServiceClearAssignmentsForDeletedSystemUserTest(ApiFixture fixture)
    {
        Fixture = fixture;
        Fixture.EnsureSeedOnce<AssignmentServiceClearAssignmentsForDeletedSystemUserTest>(db =>
        {
            db.Entities.AddRange(DeletedSystemUser, Organization);
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
    public async Task ReturnsZero_WhenNoAssignmentsExist()
    {
        using var scope = Fixture.Services.CreateScope();
        var removed = await ResolveService(scope).ClearAssignmentsForDeletedSystemUser(Guid.CreateVersion7(), TestAudit, TestContext.Current.CancellationToken);

        Assert.Equal(0, removed);
    }
}
