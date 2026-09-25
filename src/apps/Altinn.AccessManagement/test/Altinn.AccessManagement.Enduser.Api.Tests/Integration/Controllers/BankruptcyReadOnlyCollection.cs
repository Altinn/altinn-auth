using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessManagement.TestUtils.Mocks;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Integration.Controllers;

/// <summary>
/// The <see cref="ApiFixture"/> shared by the read-only bankruptcy test classes. Registers the
/// service overrides in its own constructor, so they are in place before any member class touches the
/// host — a per-class <c>ConfigureServices</c> would be applied too late for whichever class happens
/// to be constructed second.
/// </summary>
public sealed class BankruptcyReadOnlyFixture : ApiFixture
{
    public BankruptcyReadOnlyFixture()
    {
        ConfigureServices(services =>
        {
            services.AddSingleton<IUserProfileLookupService, UserProfileLookupServiceMock>();
        });
    }

    /// <summary>
    /// Seeds the state every member class reads. Called from each member constructor and keyed on this
    /// type, so it runs exactly once no matter which class is constructed first.
    /// </summary>
    /// <remarks>
    /// The seed is shared, so it is described once here rather than per class. party =
    /// <see cref="TestEntities.PersonMatilde"/>:
    /// <list type="bullet">
    ///   <item><description>administrates Solsiden, Økern and Nordis.</description></item>
    ///   <item><description>has three agents: Paula (Solsiden with read + write access, Økern with read access), Kasper (Nordis), Ørjan (nothing delegated).</description></item>
    ///   <item><description>has <see cref="TestEntities.PersonMargit"/> as an agent as well: she is registered as deceased, and the connection is what lets the deceased check be reached rather than masked by a "no such party" error.</description></item>
    /// </list>
    /// <see cref="TestEntities.PersonHenrik"/> is deliberately left without any connection, and stands
    /// for "not an agent" and "no connection to the party" throughout.
    /// </remarks>
    public void EnsureSeeded() => EnsureSeedOnce<BankruptcyReadOnlyFixture>(db =>
    {
        var solsiden = EstateAdministrator(TestEntities.OrganizationSolsidenSameie.Id);
        var okern = EstateAdministrator(TestEntities.OrganizationOkernBorettslag.Id);
        var nordis = EstateAdministrator(TestEntities.OrganizationNordisAS.Id);

        var paula = Agent(TestEntities.PersonPaula.Id);
        var kasper = Agent(TestEntities.PersonKasper.Id);
        var orjan = Agent(TestEntities.PersonOrjan.Id);
        var margit = Agent(TestEntities.PersonMargit.Id);

        db.Assignments.AddRange(solsiden, okern, nordis, paula, kasper, orjan, margit);

        BankruptcyDelegationControllerTest.SeedEstateDelegation(db, solsiden, paula, PackageConstants.BankruptcyEstateReadAccess.Id, PackageConstants.BankruptcyEstateWriteAccess.Id);
        BankruptcyDelegationControllerTest.SeedEstateDelegation(db, okern, paula, PackageConstants.BankruptcyEstateReadAccess.Id);
        BankruptcyDelegationControllerTest.SeedEstateDelegation(db, nordis, kasper, PackageConstants.BankruptcyEstateReadAccess.Id);

        db.SaveChanges();
    });

    private static Assignment EstateAdministrator(Guid estate) => new()
    {
        FromId = estate,
        ToId = TestEntities.PersonMatilde.Id,
        RoleId = RoleConstants.EstateAdministrator,
    };

    private static Assignment Agent(Guid user) => new()
    {
        FromId = TestEntities.PersonMatilde.Id,
        ToId = user,
        RoleId = RoleConstants.Agent,
    };
}

/// <summary>
/// Shares a single <see cref="BankruptcyReadOnlyFixture"/> — one test host plus one seeded database —
/// across the read-only <c>BankruptcyDelegationController</c> test classes, instead of each building
/// its own host (the dominant integration-test setup cost).
/// </summary>
/// <remarks>
/// Members must be safe to share: they may only read, or write in ways that change nothing (for
/// example revoking something that was never granted). A class that creates, changes or removes rows
/// stays on its own <see cref="Xunit.IClassFixture{TFixture}"/>.
/// </remarks>
[CollectionDefinition(Name)]
public sealed class BankruptcyReadOnlyCollection : ICollectionFixture<BankruptcyReadOnlyFixture>
{
    /// <summary>Collection name referenced by member classes via <c>[Collection]</c>.</summary>
    public const string Name = "BankruptcyDelegationController read-only";
}
