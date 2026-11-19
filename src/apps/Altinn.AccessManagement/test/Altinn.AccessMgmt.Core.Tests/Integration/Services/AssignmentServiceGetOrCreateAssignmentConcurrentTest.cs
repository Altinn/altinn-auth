using Altinn.AccessManagement.TestUtils.Concurrency;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Audit;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Altinn.AccessMgmt.Core.Tests.Integration.Services;

/// <summary>
/// Integration tests for <see cref="Altinn.AccessMgmt.Core.Services.AssignmentService.GetOrCreateAssignment"/>
/// when several requests create the same assignment at the same time, as when several access requests
/// from the same person to the same party are approved in parallel.
/// </summary>
[IntegrationTest]
public class AssignmentServiceGetOrCreateAssignmentConcurrentTest : IClassFixture<ApiFixture>
{
    private const int ParallelCalls = 4;

    private const string AssignmentUniqueIndex = "ix_assignment_fromid_toid_roleid";

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

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

    /// <summary>
    /// Several calls create the same missing assignment. The barrier holds every first save until all calls have
    /// read the assignment as missing, so all of them insert it and all but one hit the unique index. Expects the
    /// losing calls to detach the failed assignment, return the committed one, and leave their context saveable.
    /// </summary>
    [Fact]
    public async Task GetOrCreateAssignment_RacingToCreateSamePair_LosersRecoverFromUniqueViolation()
    {
        var ct = TestContext.Current.CancellationToken;
        using var barrier = new SaveBarrier(participants: ParallelCalls, Timeout);

        var calls = Enumerable.Range(0, ParallelCalls).Select(_ => Task.Run(async () =>
        {
            using var scope = Fixture.Services.CreateEFScope(TestAudit);
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var probe = barrier.Attach(db);
            var service = scope.ServiceProvider.GetRequiredService<IAssignmentService>();

            var assignment = await service.GetOrCreateAssignment(Organization.Id, Person.Id, RoleConstants.Rightholder, TestAudit, ct);

            // A failed insert still tracked as Added would make this save fail on the same unique index.
            Assert.DoesNotContain(db.ChangeTracker.Entries<Assignment>(), e => e.State == EntityState.Added);
            Assert.Equal(0, await db.SaveChangesAsync(ct));

            return (Assignment: assignment, Probe: probe);
        })).ToList();

        var outcomes = await Task.WhenAll(calls).WaitAsync(Timeout, ct);

        Assert.All(outcomes, o => Assert.NotNull(o.Assignment));
        Assert.Single(outcomes.Select(o => o.Assignment.Id).Distinct());

        // Every call read the assignment as missing and staged it before any of them committed.
        Assert.All(outcomes, o => Assert.Single(o.Probe.Saves.First().OfType<Assignment>()));

        // All calls but one lost on the assignment's unique index.
        var losers = outcomes.Where(o => !o.Probe.Failures.IsEmpty).ToList();
        Assert.Equal(ParallelCalls - 1, losers.Count);
        Assert.All(losers, o =>
        {
            var conflict = Assert.IsType<PostgresException>(Assert.Single(o.Probe.Failures).InnerException);
            Assert.Equal(PostgresErrorCodes.UniqueViolation, conflict.SqlState);
            Assert.Equal(AssignmentUniqueIndex, conflict.ConstraintName);
        });

        await Fixture.QueryDb(async db =>
        {
            var stored = await db.Assignments.AsNoTracking()
                .Where(a => a.FromId == Organization.Id && a.ToId == Person.Id && a.RoleId == RoleConstants.Rightholder.Id)
                .ToListAsync(ct);

            Assert.Equal(outcomes[0].Assignment.Id, Assert.Single(stored).Id);
        });
    }
}
