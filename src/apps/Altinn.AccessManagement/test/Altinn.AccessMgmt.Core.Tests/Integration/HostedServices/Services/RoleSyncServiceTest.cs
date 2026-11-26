using System.Net;
using System.Runtime.CompilerServices;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessMgmt.Core.HostedServices.Contracts;
using Altinn.AccessMgmt.Core.HostedServices.Leases;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Host.Lease;
using Altinn.Authorization.Integration.Platform;
using Altinn.Authorization.Integration.Platform.Register;
using Altinn.Register.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Altinn.AccessMgmt.Core.Tests.Integration.HostedServices.Services;

/// <summary>
/// <see cref="Altinn.AccessMgmt.Core.HostedServices.Services.RoleSyncService"/>
/// </summary>
/// <remarks>
/// Covers which system entity the audit trail records when the ER import adds or removes the
/// <c>EstateAdministrator</c> (bostyrer) role on an estate (konkursbo): the role itself is imported as
/// <see cref="SystemEntityConstants.RegisterImportSystem"/>, and the estate access that follows for the
/// bankruptcy estate administrators of the bostyrer as
/// <see cref="SystemEntityConstants.BankruptcyEstateAdministratorSync"/>.
/// </remarks>
[IntegrationTest]
public class RoleSyncServiceTest : IClassFixture<ApiFixture>
{
    private static readonly Guid Bostyrer = Guid.Parse("b0a7e5c1-0003-4000-8000-000000000001");
    private static readonly Guid Administrator = Guid.Parse("b0a7e5c1-0003-4000-8000-000000000002");
    private static readonly Guid Estate = Guid.Parse("b0a7e5c1-0003-4000-8000-000000000003");

    private static readonly FakeRegister Register = new();

    public RoleSyncServiceTest(ApiFixture fixture)
    {
        Fixture = fixture;

        // The test drives the sync itself. Keep the hosted service off, so local user secrets cannot start one
        // that streams from the same fake register.
        Fixture.WithDisabledFeatureFlag(AccessMgmtFeatureFlags.HostedServicesRegisterSync);
        Fixture.WithDisabledFeatureFlag(AccessMgmtFeatureFlags.HostedServicesRegisterSyncImport);
        Fixture.ConfigureServices(services =>
        {
            services.RemoveAll<IAltinnRegister>();
            services.AddSingleton<IAltinnRegister>(Register);
        });

        Fixture.EnsureSeedOnce<RoleSyncServiceTest>(db =>
        {
            db.Entities.AddRange(
                Person(Bostyrer, "Bodil Bostyrer", "19017700101", 50970201, 20970201),
                Person(Administrator, "Anne Administrator", "19017700102", 50970202, 20970202),
                new Entity
                {
                    Id = Estate,
                    Name = "Konkursbo Tre",
                    TypeId = EntityTypeConstants.Organization,
                    VariantId = EntityVariantConstants.KBO,
                    OrganizationIdentifier = "399970201",
                    RefId = "399970201",
                    PartyId = 50970203,
                });

            db.Assignments.Add(new Assignment { FromId = Bostyrer, ToId = Administrator, RoleId = RoleConstants.BankruptcyEstateAdministrator });
            db.SaveChanges();
        });
    }

    public ApiFixture Fixture { get; }

    /// <summary>
    /// The ER import makes Bodil bostyrer on the estate and later removes her. Expects the bostyrer role to be
    /// audited as the register import, and the grant and revoke of Anne's estate access as the bankruptcy estate
    /// administrator sync.
    /// </summary>
    [Fact]
    public async Task SyncRoles_EstateAdministratorAddedThenRemoved_AuditsEstateAccessAsBankruptcyEstateAdministratorSync()
    {
        var lease = new FakeLease();
        var registerImport = SystemEntityConstants.RegisterImportSystem.Id;
        var estateSync = SystemEntityConstants.BankruptcyEstateAdministratorSync.Id;

        Register.Events = [EstateAdministratorEvent(ExternalRoleAssignmentEvent.EventType.Added, versionId: 1)];
        await ResolveService().SyncRoles(lease, cancellationToken: TestContext.Current.CancellationToken);

        await Fixture.QueryDb(async db =>
        {
            var bostyrerRole = await db.Assignments.AsNoTracking()
                .SingleAsync(a => a.FromId == Estate && a.ToId == Bostyrer && a.RoleId == RoleConstants.EstateAdministrator, TestContext.Current.CancellationToken);
            Assert.Equal(registerImport, bostyrerRole.Audit_ChangedBySystem);

            var estateAccess = await db.Assignments.AsNoTracking()
                .SingleAsync(a => a.FromId == Estate && a.ToId == Administrator && a.RoleId == RoleConstants.Rightholder, TestContext.Current.CancellationToken);
            Assert.Equal(estateSync, estateAccess.Audit_ChangedBySystem);

            var packages = await db.AssignmentPackages.AsNoTracking()
                .Where(p => p.AssignmentId == estateAccess.Id)
                .ToListAsync(TestContext.Current.CancellationToken);
            Assert.NotEmpty(packages);
            Assert.All(packages, p => Assert.Equal(estateSync, p.Audit_ChangedBySystem));
        });

        Register.Events = [EstateAdministratorEvent(ExternalRoleAssignmentEvent.EventType.Removed, versionId: 2)];
        await ResolveService().SyncRoles(lease, cancellationToken: TestContext.Current.CancellationToken);

        await Fixture.QueryDb(async db =>
        {
            Assert.False(await db.Assignments.AsNoTracking()
                .AnyAsync(a => a.FromId == Estate && a.ToId == Administrator, TestContext.Current.CancellationToken));

            var removedBostyrerRole = await db.AuditAssignments.AsNoTracking()
                .SingleAsync(a => a.FromId == Estate && a.ToId == Bostyrer && a.RoleId == RoleConstants.EstateAdministrator && a.Audit_DeletedBySystem != null, TestContext.Current.CancellationToken);
            Assert.Equal(registerImport, removedBostyrerRole.Audit_DeletedBySystem);

            var removedEstateAccess = await db.AuditAssignments.AsNoTracking()
                .SingleAsync(a => a.FromId == Estate && a.ToId == Administrator && a.RoleId == RoleConstants.Rightholder && a.Audit_DeletedBySystem != null, TestContext.Current.CancellationToken);
            Assert.Equal(estateSync, removedEstateAccess.Audit_DeletedBySystem);
        });
    }

    private IRoleSyncService ResolveService()
    {
        using var scope = Fixture.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IRoleSyncService>();
    }

    private static ExternalRoleAssignmentEvent EstateAdministratorEvent(ExternalRoleAssignmentEvent.EventType type, ulong versionId) => new()
    {
        VersionId = versionId,
        Type = type,
        RoleSource = default,
        RoleIdentifier = RoleConstants.EstateAdministrator.Entity.Code,
        FromParty = Estate,
        ToParty = Bostyrer,
    };

    private static Entity Person(Guid id, string name, string ssn, int partyId, int userId) => new()
    {
        Id = id,
        Name = name,
        TypeId = EntityTypeConstants.Person,
        VariantId = EntityVariantConstants.Person,
        PersonIdentifier = ssn,
        RefId = ssn,
        DateOfBirth = new(1977, 1, 19),
        PartyId = partyId,
        UserId = userId,
    };

    /// <summary>
    /// A register that streams <see cref="Events"/> as a single page with no next link.
    /// </summary>
    internal sealed class FakeRegister : IAltinnRegister
    {
        public List<ExternalRoleAssignmentEvent> Events { get; set; } = [];

        public Task<IAsyncEnumerable<PlatformResponse<PageStream<Party>>>> StreamParties(IEnumerable<string> fields, string nextPage = null, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IAsyncEnumerable<PlatformResponse<PageStream<ExternalRoleAssignmentEvent>>>> StreamRoles(IEnumerable<string> fields, string nextPage = null, CancellationToken cancellationToken = default)
        {
            var page = new PlatformResponse<PageStream<ExternalRoleAssignmentEvent>>
            {
                IsSuccessful = true,
                StatusCode = HttpStatusCode.OK,
                Content = new PageStream<ExternalRoleAssignmentEvent>
                {
                    Stats = new PageStream<ExternalRoleAssignmentEvent>.StatsStream(),
                    Links = new PageStream<ExternalRoleAssignmentEvent>.LinksStream { Next = null },
                    Data = [.. Events],
                },
            };

            return Task.FromResult(ToAsync(page, cancellationToken));
        }

        private static async IAsyncEnumerable<PlatformResponse<PageStream<ExternalRoleAssignmentEvent>>> ToAsync(
            PlatformResponse<PageStream<ExternalRoleAssignmentEvent>> page,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return page;
            await Task.Yield();
        }
    }

    /// <summary>
    /// A lease whose register data reports the initial ingest as done, so the sync runs.
    /// </summary>
    internal sealed class FakeLease : ILease
    {
        public RegisterLease Data { get; } = new() { IsDbIngested = true };

        public ValueTask DisposeAsync() => default;

        public Task<T> Get<T>(CancellationToken cancellationToken = default)
            where T : class, new() =>
            Task.FromResult(Data as T ?? new T());

        public Task Update<T>(T data, CancellationToken cancellationToken = default)
            where T : class, new() =>
            Task.CompletedTask;

        public Task Update<T>(Action<T> configureData, CancellationToken cancellationToken = default)
            where T : class, new() =>
            Task.CompletedTask;
    }
}
