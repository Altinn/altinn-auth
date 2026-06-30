using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.AccessMgmt.Core.Tests.Integration.Services;

/// <summary>
/// Integration tests for <see cref="Altinn.AccessMgmt.Core.Services.AssignmentService.ClearAssignmentsInAfterLife"/>.
/// </summary>
[IntegrationTest]
public class AssignmentServiceClearAssignmentsInAfterLifeTest : IClassFixture<ApiFixture>
{
    private static readonly AuditValues TestAudit = new(SystemEntityConstants.StaticDataIngest, SystemEntityConstants.StaticDataIngest);

    private static readonly Entity DeadPerson = new()
    {
        Id = Guid.Parse("01970001-0000-7000-8000-000000000001"),
        TypeId = EntityTypeConstants.Person,
        VariantId = EntityVariantConstants.Person,
        Name = "Dead Person",
        RefId = "AFTERLIFE-TEST-PERSON-01",
    };

    private static readonly Entity Organization = new()
    {
        Id = Guid.Parse("01970001-0000-7000-8000-000000000002"),
        TypeId = EntityTypeConstants.Organization,
        VariantId = EntityVariantConstants.AS,
        Name = "AfterLife Test Org",
        RefId = "AFTERLIFE-TEST-ORG-01",
    };

    private static readonly Entity OtherPerson = new()
    {
        Id = Guid.Parse("01970001-0000-7000-8000-000000000003"),
        TypeId = EntityTypeConstants.Person,
        VariantId = EntityVariantConstants.Person,
        Name = "Other Person",
        RefId = "AFTERLIFE-TEST-PERSON-02",
    };

    private ApiFixture Fixture { get; }

    public AssignmentServiceClearAssignmentsInAfterLifeTest(ApiFixture fixture)
    {
        Fixture = fixture;
        Fixture.EnsureSeedOnce<AssignmentServiceClearAssignmentsInAfterLifeTest>(db =>
        {
            db.Entities.AddRange(DeadPerson, Organization, OtherPerson);
            db.SaveChanges(TestAudit);
        });
    }

    private IAssignmentService ResolveService(IServiceScope scope)
    {
        return scope.ServiceProvider.GetRequiredService<IAssignmentService>();
    }

    [Fact]
    public async Task RemovesRightholderAssignment_WhenDeadPersonIsTo()
    {
        var assignmentId = Guid.CreateVersion7();
        await Fixture.QueryDb(async db =>
        {
            db.Assignments.Add(new Assignment { Id = assignmentId, FromId = Organization.Id, ToId = DeadPerson.Id, RoleId = RoleConstants.Rightholder });
            await db.SaveChangesAsync(TestAudit, TestContext.Current.CancellationToken);
        });

        using var scope = Fixture.Services.CreateScope();
        var svc = ResolveService(scope);
        await svc.ClearAssignmentsInAfterLife(DeadPerson.Id, TestAudit, TestContext.Current.CancellationToken);

        await Fixture.QueryDb(async db =>
        {
            var remaining = await db.Assignments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == assignmentId);
            Assert.Null(remaining);
        });
    }

    [Fact]
    public async Task RemovesRightholderAssignment_WhenDeadPersonIsFrom()
    {
        var assignmentId = Guid.CreateVersion7();
        await Fixture.QueryDb(async db =>
        {
            db.Assignments.Add(new Assignment { Id = assignmentId, FromId = DeadPerson.Id, ToId = OtherPerson.Id, RoleId = RoleConstants.Rightholder });
            await db.SaveChangesAsync(TestAudit, TestContext.Current.CancellationToken);
        });

        using var scope = Fixture.Services.CreateScope();
        var svc = ResolveService(scope);
        await svc.ClearAssignmentsInAfterLife(DeadPerson.Id, TestAudit, TestContext.Current.CancellationToken);

        await Fixture.QueryDb(async db =>
        {
            var remaining = await db.Assignments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == assignmentId);
            Assert.Null(remaining);
        });
    }

    [Fact]
    public async Task RemovesAppControlledRightholderAssignment_WhenDeadPersonIsTo()
    {
        var assignmentId = Guid.CreateVersion7();
        await Fixture.QueryDb(async db =>
        {
            db.Assignments.Add(new Assignment { Id = assignmentId, FromId = Organization.Id, ToId = DeadPerson.Id, RoleId = RoleConstants.AppControlledRightholder });
            await db.SaveChangesAsync(TestAudit, TestContext.Current.CancellationToken);
        });

        using var scope = Fixture.Services.CreateScope();
        var svc = ResolveService(scope);
        await svc.ClearAssignmentsInAfterLife(DeadPerson.Id, TestAudit, TestContext.Current.CancellationToken);

        await Fixture.QueryDb(async db =>
        {
            var remaining = await db.Assignments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == assignmentId);
            Assert.Null(remaining);
        });
    }

    [Fact]
    public async Task RemovesAppControlledRightholderAssignment_WhenDeadPersonIsFrom()
    {
        var assignmentId = Guid.CreateVersion7();
        await Fixture.QueryDb(async db =>
        {
            db.Assignments.Add(new Assignment { Id = assignmentId, FromId = DeadPerson.Id, ToId = OtherPerson.Id, RoleId = RoleConstants.AppControlledRightholder });
            await db.SaveChangesAsync(TestAudit, TestContext.Current.CancellationToken);
        });

        using var scope = Fixture.Services.CreateScope();
        var svc = ResolveService(scope);
        await svc.ClearAssignmentsInAfterLife(DeadPerson.Id, TestAudit, TestContext.Current.CancellationToken);

        await Fixture.QueryDb(async db =>
        {
            var remaining = await db.Assignments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == assignmentId);
            Assert.Null(remaining);
        });
    }

    [Fact]
    public async Task RemovesAltinn2RoleAssignment_WhenDeadPersonIsTo()
    {
        // MailArchive is an Altinn2-provided role
        var assignmentId = Guid.CreateVersion7();
        await Fixture.QueryDb(async db =>
        {
            db.Assignments.Add(new Assignment { Id = assignmentId, FromId = Organization.Id, ToId = DeadPerson.Id, RoleId = RoleConstants.MailArchive });
            await db.SaveChangesAsync(TestAudit, TestContext.Current.CancellationToken);
        });

        using var scope = Fixture.Services.CreateScope();
        var svc = ResolveService(scope);
        await svc.ClearAssignmentsInAfterLife(DeadPerson.Id, TestAudit, TestContext.Current.CancellationToken);

        await Fixture.QueryDb(async db =>
        {
            var remaining = await db.Assignments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == assignmentId);
            Assert.Null(remaining);
        });
    }

    [Fact]
    public async Task RemovesAltinn2RoleAssignment_WhenDeadPersonIsFrom()
    {
        // PrimaryIndustryAndFoodstuff is an Altinn2-provided role
        var assignmentId = Guid.CreateVersion7();
        await Fixture.QueryDb(async db =>
        {
            db.Assignments.Add(new Assignment { Id = assignmentId, FromId = DeadPerson.Id, ToId = OtherPerson.Id, RoleId = RoleConstants.PrimaryIndustryAndFoodstuff });
            await db.SaveChangesAsync(TestAudit, TestContext.Current.CancellationToken);
        });

        using var scope = Fixture.Services.CreateScope();
        var svc = ResolveService(scope);
        await svc.ClearAssignmentsInAfterLife(DeadPerson.Id, TestAudit, TestContext.Current.CancellationToken);

        await Fixture.QueryDb(async db =>
        {
            var remaining = await db.Assignments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == assignmentId);
            Assert.Null(remaining);
        });
    }

    [Fact]
    public async Task RemovesAgentAssignment_WhenDeadPersonIsTo()
    {
        var assignmentId = Guid.CreateVersion7();
        await Fixture.QueryDb(async db =>
        {
            db.Assignments.Add(new Assignment { Id = assignmentId, FromId = Organization.Id, ToId = DeadPerson.Id, RoleId = RoleConstants.Agent });
            await db.SaveChangesAsync(TestAudit, TestContext.Current.CancellationToken);
        });

        using var scope = Fixture.Services.CreateScope();
        var svc = ResolveService(scope);
        await svc.ClearAssignmentsInAfterLife(DeadPerson.Id, TestAudit, TestContext.Current.CancellationToken);

        await Fixture.QueryDb(async db =>
        {
            var remaining = await db.Assignments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == assignmentId);
            Assert.Null(remaining);
        });
    }

    [Fact]
    public async Task DoesNotRemoveUnrelatedRoleAssignment()
    {
        var assignmentId = Guid.CreateVersion7();
        await Fixture.QueryDb(async db =>
        {
            db.Assignments.Add(new Assignment { Id = assignmentId, FromId = Organization.Id, ToId = DeadPerson.Id, RoleId = RoleConstants.ManagingDirector });
            await db.SaveChangesAsync(TestAudit, TestContext.Current.CancellationToken);
        });

        using var scope = Fixture.Services.CreateScope();
        var svc = ResolveService(scope);
        await svc.ClearAssignmentsInAfterLife(DeadPerson.Id, TestAudit, TestContext.Current.CancellationToken);

        await Fixture.QueryDb(async db =>
        {
            var remaining = await db.Assignments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == assignmentId);
            Assert.NotNull(remaining);
        });
    }

    [Fact]
    public async Task DoesNotRemoveAssignmentsForOtherPersons()
    {
        var assignmentId = Guid.CreateVersion7();
        await Fixture.QueryDb(async db =>
        {
            db.Assignments.Add(new Assignment { Id = assignmentId, FromId = Organization.Id, ToId = OtherPerson.Id, RoleId = RoleConstants.Rightholder });
            await db.SaveChangesAsync(TestAudit, TestContext.Current.CancellationToken);
        });

        using var scope = Fixture.Services.CreateScope();
        var svc = ResolveService(scope);
        await svc.ClearAssignmentsInAfterLife(DeadPerson.Id, TestAudit, TestContext.Current.CancellationToken);

        await Fixture.QueryDb(async db =>
        {
            var remaining = await db.Assignments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == assignmentId);
            Assert.NotNull(remaining);
        });
    }

    [Fact]
    public async Task DoesNotThrow_WhenNoAssignmentsExist()
    {
        var nonExistentPerson = Guid.CreateVersion7();

        using var scope = Fixture.Services.CreateScope();
        var svc = ResolveService(scope);
        await svc.ClearAssignmentsInAfterLife(nonExistentPerson, TestAudit, TestContext.Current.CancellationToken);
    }
}
