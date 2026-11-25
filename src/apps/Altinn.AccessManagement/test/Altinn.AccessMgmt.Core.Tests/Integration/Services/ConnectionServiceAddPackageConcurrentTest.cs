using Altinn.AccessManagement.TestUtils.Concurrency;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessMgmt.Core.Services;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Audit;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.ProblemDetails;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Altinn.AccessMgmt.Core.Tests.Integration.Services;

/// <summary>
/// Integration tests for the concurrent-write retry in <see cref="ConnectionService.AddPackage(Guid, Guid, Guid, Action{ConnectionOptions}, CancellationToken)"/>,
/// when two requests add the same access package to the same pair at the same time. A barrier holds the first save
/// of both requests until both have read the database, so they are certain to collide on a unique index.
/// </summary>
[IntegrationTest]
public class ConnectionServiceAddPackageConcurrentTest : IClassFixture<ApiFixture>
{
    private const string AssignmentUniqueIndex = "ix_assignment_fromid_toid_roleid";

    private const string AssignmentPackageUniqueIndex = "ix_assignmentpackage_assignmentid_packageid";

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Jinx Arcane is managing director of Kaos and may assign its access packages.
    /// </summary>
    private static readonly AuditValues CallerAudit = new(TestData.JinxArcane.Id, SystemEntityConstants.StaticDataIngest);

    private static readonly Action<ConnectionOptions> ConfigureConnections = options =>
    {
        options.AllowedWriteFromEntityTypes = [EntityTypeConstants.Organization, EntityTypeConstants.Person];
        options.AllowedWriteToEntityTypes = [EntityTypeConstants.Organization, EntityTypeConstants.Person];
        options.AllowedReadFromEntityTypes = [EntityTypeConstants.Organization, EntityTypeConstants.Person];
        options.AllowedReadToEntityTypes = [EntityTypeConstants.Organization, EntityTypeConstants.Person];
        options.FilterFromEntityTypes = [];
        options.FilterToEntityTypes = [];
    };

    public ConnectionServiceAddPackageConcurrentTest(ApiFixture fixture)
    {
        Fixture = fixture;
    }

    private ApiFixture Fixture { get; }

    /// <summary>
    /// Kaos and Alex the Artist are connected through the chair of the board role, but Alex has no rightholder
    /// assignment. Both calls read the assignment as missing and stage it with the package, so one of them hits the
    /// assignment's unique index. Expects the losing call to retry, find the assignment and package the other call
    /// committed, and return them without writing again.
    /// </summary>
    [Fact]
    public async Task AddPackage_RacingToCreateMissingAssignment_RetriesOnCommittedAssignment()
    {
        var ct = TestContext.Current.CancellationToken;
        var fromId = TestData.KaosMagicDesignAndArts.Id;
        var toId = TestData.AlexTheArtist.Id;
        var packageId = PackageConstants.CompanyRepresentativeFormTasks.Id;

        using var barrier = new SaveBarrier(participants: 2, Timeout);
        var first = InScope(barrier, service => service.AddPackage(fromId, toId, packageId, ConfigureConnections, ct));
        var second = InScope(barrier, service => service.AddPackage(fromId, toId, packageId, ConfigureConnections, ct));

        var results = await AssertBothSucceededWithSamePackage(first.Result, second.Result, ct);
        var probes = new[] { await first.Probe, await second.Probe };

        // Both calls read the assignment as missing and staged it before either saved.
        Assert.All(probes, p => Assert.Single(p.Saves.First().OfType<Assignment>()));

        // Exactly one call lost the race on the assignment's unique index.
        var loser = Assert.Single(probes, p => !p.Failures.IsEmpty);
        var winner = Assert.Single(probes, p => p.Failures.IsEmpty);
        AssertUniqueViolation(Assert.Single(loser.Failures), AssignmentUniqueIndex);
        Assert.Single(winner.Saves);

        // The retry found both committed rows and returned without saving again.
        Assert.Single(loser.Saves);

        await AssertSingleAssignmentWithPackage(fromId, toId, packageId, results[0].Value.Id, ct);
    }

    /// <summary>
    /// Kaos and Josephine already have a rightholder assignment without the package. Both calls read the package as
    /// missing and stage it, so one of them hits the assignment package's unique index. Expects the losing call to
    /// retry, find the package the other call committed, and return it.
    /// </summary>
    [Fact]
    public async Task AddPackage_RacingToAddSamePackage_RetriesOnCommittedPackage()
    {
        var ct = TestContext.Current.CancellationToken;
        var fromId = TestData.KaosMagicDesignAndArts.Id;
        var toId = TestData.JosephineYvonnesdottir.Id;
        var packageId = PackageConstants.AccountingAndEconomicReporting.Id;

        using var barrier = new SaveBarrier(participants: 2, Timeout);
        var first = InScope(barrier, service => service.AddPackage(fromId, toId, packageId, ConfigureConnections, ct));
        var second = InScope(barrier, service => service.AddPackage(fromId, toId, packageId, ConfigureConnections, ct));

        var results = await AssertBothSucceededWithSamePackage(first.Result, second.Result, ct);
        var probes = new[] { await first.Probe, await second.Probe };

        // Both calls found the existing assignment and staged only the package before either saved.
        Assert.All(probes, p =>
        {
            Assert.Empty(p.Saves.First().OfType<Assignment>());
            Assert.Single(p.Saves.First().OfType<AssignmentPackage>());
        });

        // Exactly one call lost the race. Both calls also stage the same pending notification, so the loser may hit
        // the outbox index first and the package index on the inner retry's save.
        var loser = Assert.Single(probes, p => !p.Failures.IsEmpty);
        Assert.Contains(loser.Failures, f => IsUniqueViolation(f, AssignmentPackageUniqueIndex));
        Assert.Single(Assert.Single(probes, p => p.Failures.IsEmpty).Saves);

        // Every save the loser made failed: the retry found the committed package and returned without writing.
        Assert.Equal(loser.Failures.Count, loser.Saves.Count);

        await AssertSingleAssignmentWithPackage(fromId, toId, packageId, results[0].Value.Id, ct);
    }

    /// <summary>
    /// Runs the operation on a thread of its own, with its own scope and therefore its own connection, and
    /// attaches the barrier to that scope's context.
    /// </summary>
    private (Task<Result<AssignmentPackageDto>> Result, Task<SaveProbe> Probe) InScope(
        SaveBarrier barrier,
        Func<IConnectionService, Task<Result<AssignmentPackageDto>>> operation)
    {
        var probe = new TaskCompletionSource<SaveProbe>(TaskCreationOptions.RunContinuationsAsynchronously);
        var result = Task.Run(async () =>
        {
            using var scope = Fixture.Services.CreateEFScope(CallerAudit);
            probe.SetResult(barrier.Attach(scope.ServiceProvider.GetRequiredService<AppDbContext>()));
            return await operation(scope.ServiceProvider.GetRequiredService<IConnectionService>());
        });

        return (result, probe.Task);
    }

    private static async Task<Result<AssignmentPackageDto>[]> AssertBothSucceededWithSamePackage(
        Task<Result<AssignmentPackageDto>> first,
        Task<Result<AssignmentPackageDto>> second,
        CancellationToken ct)
    {
        var results = await Task.WhenAll(first, second).WaitAsync(Timeout, ct);
        Assert.All(results, r => Assert.False(r.IsProblem, r.Problem?.Detail));
        Assert.Single(results.Select(r => r.Value.Id).Distinct());
        return results;
    }

    private static bool IsUniqueViolation(Exception exception, string index) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } conflict
        && conflict.ConstraintName == index;

    private static void AssertUniqueViolation(Exception exception, string index)
    {
        var conflict = Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, conflict.SqlState);
        Assert.Equal(index, conflict.ConstraintName);
    }

    private Task AssertSingleAssignmentWithPackage(Guid fromId, Guid toId, Guid packageId, Guid assignmentPackageId, CancellationToken ct) =>
        Fixture.QueryDb(async db =>
        {
            var assignments = await db.Assignments.AsNoTracking()
                .Where(a => a.FromId == fromId && a.ToId == toId && a.RoleId == RoleConstants.Rightholder.Id)
                .ToListAsync(ct);

            var assignment = Assert.Single(assignments);
            var packages = await db.AssignmentPackages.AsNoTracking()
                .Where(p => p.AssignmentId == assignment.Id && p.PackageId == packageId)
                .ToListAsync(ct);

            Assert.Equal(assignmentPackageId, Assert.Single(packages).Id);
        });
}
