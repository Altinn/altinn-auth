using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.AccessMgmt.Core.Tests.Integration.Services;

/// <summary>
/// Integration tests for <see cref="Altinn.AccessMgmt.Core.Services.AssignmentService.GetOrCreateAssignment"/>
/// when several requests create the same assignment at the same time, as when several access requests
/// from the same person to the same party are approved in parallel.
/// </summary>
[IntegrationTest]
public class AssignmentServiceGetOrCreateAssignmentConcurrentTest : IClassFixture<ApiFixture>
{
    private const int ParallelCalls = 8;

    private static readonly AuditValues TestAudit = new(SystemEntityConstants.StaticDataIngest, SystemEntityConstants.StaticDataIngest);

    private static readonly Entity Organization = new()
    {
        Id = Guid.Parse("01970003-0000-7000-8000-000000000001"),
        TypeId = EntityTypeConstants.Organization,
        VariantId = EntityVariantConstants.AS,
        Name = "GetOrCreateAssignment Concurrent Org",
        RefId = "GETORCREATE-CONCURRENT-ORG-01",
    };

    private static readonly Entity Person = new()
    {
        Id = Guid.Parse("01970003-0000-7000-8000-000000000002"),
        TypeId = EntityTypeConstants.Person,
        VariantId = EntityVariantConstants.Person,
        Name = "GetOrCreateAssignment Concurrent Person",
        RefId = "GETORCREATE-CONCURRENT-PERSON-01",
    };

    private ApiFixture Fixture { get; }

    public AssignmentServiceGetOrCreateAssignmentConcurrentTest(ApiFixture fixture)
    {
        Fixture = fixture;
        Fixture.EnsureSeedOnce<AssignmentServiceGetOrCreateAssignmentConcurrentTest>(db =>
        {
            db.Entities.AddRange(Organization, Person);
            db.SaveChanges(TestAudit);
        });
    }

    [Fact]
    public async Task GetOrCreateAssignment_ConcurrentCallsForSamePair_AllReturnSameAssignment()
    {
        using var start = new ManualResetEventSlim(false);

        var tasks = Enumerable.Range(0, ParallelCalls).Select(_ => Task.Run(async () =>
        {
            using var scope = Fixture.Services.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IAssignmentService>();
            start.Wait(TestContext.Current.CancellationToken);
            return await service.GetOrCreateAssignment(Organization.Id, Person.Id, RoleConstants.Rightholder, TestAudit, TestContext.Current.CancellationToken);
        })).ToList();

        start.Set();
        var assignments = await Task.WhenAll(tasks);

        Assert.All(assignments, Assert.NotNull);
        Assert.Single(assignments.Select(a => a.Id).Distinct());

        await Fixture.QueryDb(async db =>
        {
            var stored = await db.Assignments.AsNoTracking()
                .Where(a => a.FromId == Organization.Id && a.ToId == Person.Id && a.RoleId == RoleConstants.Rightholder.Id)
                .ToListAsync(TestContext.Current.CancellationToken);

            Assert.Single(stored);
            Assert.Equal(assignments[0].Id, stored[0].Id);
        });
    }
}
