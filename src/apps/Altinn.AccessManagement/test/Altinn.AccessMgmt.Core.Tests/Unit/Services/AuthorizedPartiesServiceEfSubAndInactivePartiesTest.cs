using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Models.Profile;
using Altinn.AccessManagement.Core.Services;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessMgmt.Core;
using Altinn.AccessMgmt.Core.Appsettings;
using Altinn.AccessMgmt.Core.Services;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Queries.Connection.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement.Enums;
using Microsoft.FeatureManagement;
using Moq;

namespace Altinn.AccessMgmt.Core.Tests.Unit.Services;

/// <summary>
/// Unit tests for the <c>includeSubParties</c> and <c>includeInactiveParties</c> filters in <see cref="AuthorizedPartiesServiceEf"/>
/// and their mapping to the ConnectionQuery filter in <see cref="AuthorizedPartyRepoServiceEf"/>.
///
/// - With the feature flag off, both filters are forced to true, whatever the request says.
/// - With includeSubParties=false, subunits are left out, also where the subject has direct access to the subunit.
/// - includeSubParties=false maps to IncludeSubConnections=false, includeInactiveParties=false maps to ExcludeDeleted=true.
/// </summary>
[UnitTest]
public class AuthorizedPartiesServiceEfSubAndInactivePartiesTest
{
    private static readonly Entity Subject = new()
    {
        Id = Guid.NewGuid(),
        Name = "Subject AS",
        TypeId = EntityTypeConstants.Organization.Id,
        VariantId = EntityVariantConstants.AS.Id,
    };

    private static readonly Entity MainUnit = new()
    {
        Id = Guid.NewGuid(),
        Name = "Main unit AS",
        TypeId = EntityTypeConstants.Organization.Id,
        VariantId = EntityVariantConstants.AS.Id,
    };

    private static readonly Entity Subunit = new()
    {
        Id = Guid.NewGuid(),
        Name = "Subunit BEDR",
        TypeId = EntityTypeConstants.Organization.Id,
        VariantId = EntityVariantConstants.BEDR.Id,
        ParentId = MainUnit.Id,
        Parent = MainUnit,
    };

    private static ConnectionQueryExtendedRecord Connection(Entity from, ConnectionReason reason) => new()
    {
        FromId = from.Id,
        From = from,
        ToId = Subject.Id,
        To = Subject,
        RoleId = RoleConstants.ManagingDirector.Id,
        AssignmentId = Guid.NewGuid(),
        Reason = reason,
    };

    private static AuthorizedPartiesFilters Filters(AuthorizedPartiesIncludeFilter includeSubParties, AuthorizedPartiesIncludeFilter includeInactiveParties) => new()
    {
        IncludeRoles = false,
        IncludeAccessPackages = false,
        IncludeResources = false,
        IncludeInstances = false,
        IncludePartiesViaKeyRoles = AuthorizedPartiesIncludeFilter.True,
        IncludeSubParties = includeSubParties,
        IncludeInactiveParties = includeInactiveParties,
    };

    private static readonly Entity UserSubject = new()
    {
        Id = Guid.NewGuid(),
        Name = "Person",
        TypeId = EntityTypeConstants.Person.Id,
        VariantId = EntityVariantConstants.Person.Id,
        UserId = 20001234,
    };

    private static (AuthorizedPartiesServiceEf Service, List<AuthorizedPartiesFilters> SentToRepo) CreateService(bool featureEnabled, List<ConnectionQueryExtendedRecord> connections, ProfileSettingPreference? profileSettings = null)
    {
        var sentToRepo = new List<AuthorizedPartiesFilters>();

        var repo = new Mock<IAuthorizedPartyRepoServiceEf>();
        repo.Setup(r => r.GetConnectionsFromOthers(It.IsAny<Guid>(), It.IsAny<AuthorizedPartiesFilters>(), true, It.IsAny<CancellationToken>()))
            .Callback<Guid, AuthorizedPartiesFilters, bool, CancellationToken>((_, filters, _, _) => sentToRepo.Add(filters))
            .ReturnsAsync(connections);

        var contextRetrieval = new Mock<IContextRetrievalService>();
        contextRetrieval.Setup(c => c.GetNewUserProfile(UserSubject.UserId!.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profileSettings == null ? null! : new NewUserProfile { UserId = UserSubject.UserId!.Value, ProfileSettingPreference = profileSettings });

        var featureManager = new Mock<IFeatureManager>();
        featureManager.Setup(f => f.IsEnabledAsync(AccessMgmtFeatureFlags.AuthorizedPartiesSubAndInactivePartiesFilters))
            .ReturnsAsync(featureEnabled);

        var service = new AuthorizedPartiesServiceEf(
            contextRetrievalService: contextRetrieval.Object,
            repoService: repo.Object,
            memoryCache: null!,
            lifecycleFeatures: new AppLifecycleFeatures(),
            featureManager: featureManager.Object);

        return (service, sentToRepo);
    }

    [Fact]
    public async Task IncludeSubPartiesFalse_LeavesOutSubunitWithDirectAccess()
    {
        // The subject has direct access to both the main unit and the subunit
        var (service, _) = CreateService(featureEnabled: true, [Connection(MainUnit, ConnectionReason.Assignment), Connection(Subunit, ConnectionReason.Assignment)]);

        var result = await service.GetAuthorizedPartiesByEntity(Subject, Filters(AuthorizedPartiesIncludeFilter.False, AuthorizedPartiesIncludeFilter.True), TestContext.Current.CancellationToken);

        var mainUnit = result.Should().ContainSingle().Which;
        mainUnit.PartyUuid.Should().Be(MainUnit.Id);
        mainUnit.Subunits.Should().BeEmpty();
    }

    [Fact]
    public async Task IncludeSubPartiesFalse_OnlySubunitAccess_ReturnsNoHierarchyParent()
    {
        var (service, _) = CreateService(featureEnabled: true, [Connection(Subunit, ConnectionReason.Assignment)]);

        var result = await service.GetAuthorizedPartiesByEntity(Subject, Filters(AuthorizedPartiesIncludeFilter.False, AuthorizedPartiesIncludeFilter.True), TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task IncludeSubPartiesTrue_NestsSubunitUnderMainUnit()
    {
        var (service, _) = CreateService(featureEnabled: true, [Connection(MainUnit, ConnectionReason.Assignment), Connection(Subunit, ConnectionReason.Hierarchy)]);

        var result = await service.GetAuthorizedPartiesByEntity(Subject, Filters(AuthorizedPartiesIncludeFilter.True, AuthorizedPartiesIncludeFilter.True), TestContext.Current.CancellationToken);

        var mainUnit = result.Should().ContainSingle().Which;
        mainUnit.Subunits.Should().ContainSingle(s => s.PartyUuid == Subunit.Id);
    }

    [Fact]
    public async Task FeatureEnabled_PassesFiltersToRepo()
    {
        var (service, sentToRepo) = CreateService(featureEnabled: true, []);

        await service.GetAuthorizedPartiesByEntity(Subject, Filters(AuthorizedPartiesIncludeFilter.False, AuthorizedPartiesIncludeFilter.False), TestContext.Current.CancellationToken);

        var filters = sentToRepo.Should().ContainSingle().Which;
        filters.IncludeSubParties.Should().Be(AuthorizedPartiesIncludeFilter.False);
        filters.IncludeInactiveParties.Should().Be(AuthorizedPartiesIncludeFilter.False);
    }

    [Fact]
    public async Task FeatureDisabled_IgnoresFiltersAndNestsSubunit()
    {
        var (service, sentToRepo) = CreateService(featureEnabled: false, [Connection(MainUnit, ConnectionReason.Assignment), Connection(Subunit, ConnectionReason.Assignment)]);

        var result = await service.GetAuthorizedPartiesByEntity(Subject, Filters(AuthorizedPartiesIncludeFilter.False, AuthorizedPartiesIncludeFilter.False), TestContext.Current.CancellationToken);

        var filters = sentToRepo.Should().ContainSingle().Which;
        filters.IncludeSubParties.Should().Be(AuthorizedPartiesIncludeFilter.True);
        filters.IncludeInactiveParties.Should().Be(AuthorizedPartiesIncludeFilter.True);

        var mainUnit = result.Should().ContainSingle().Which;
        mainUnit.Subunits.Should().ContainSingle(s => s.PartyUuid == Subunit.Id);
    }

    [Fact]
    public async Task AutoResolvedForNonUser_KeepsExplicitFilters()
    {
        // An organization has no profile, so auto resolves to true. Filters the caller set explicitly must not be overwritten.
        var (service, sentToRepo) = CreateService(featureEnabled: true, []);
        var filters = Filters(AuthorizedPartiesIncludeFilter.False, AuthorizedPartiesIncludeFilter.False);
        filters.IncludePartiesViaKeyRoles = AuthorizedPartiesIncludeFilter.Auto;

        await service.GetAuthorizedPartiesByEntity(Subject, filters, TestContext.Current.CancellationToken);

        var sent = sentToRepo.Should().ContainSingle().Which;
        sent.IncludePartiesViaKeyRoles.Should().Be(AuthorizedPartiesIncludeFilter.True);
        sent.IncludeSubParties.Should().Be(AuthorizedPartiesIncludeFilter.False);
        sent.IncludeInactiveParties.Should().Be(AuthorizedPartiesIncludeFilter.False);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task AutoForUser_ProfileSettingsDecideFilters(bool shouldShowSubEntities, bool shouldShowDeletedEntities)
    {
        var profile = new ProfileSettingPreference
        {
            ShowClientUnits = true,
            ShouldShowSubEntities = shouldShowSubEntities,
            ShouldShowDeletedEntities = shouldShowDeletedEntities,
        };
        var (service, sentToRepo) = CreateService(featureEnabled: true, [], profile);

        await service.GetAuthorizedPartiesByEntity(UserSubject, Filters(AuthorizedPartiesIncludeFilter.Auto, AuthorizedPartiesIncludeFilter.Auto), TestContext.Current.CancellationToken);

        var sent = sentToRepo.Should().ContainSingle().Which;
        sent.IncludeSubParties.Should().Be(shouldShowSubEntities ? AuthorizedPartiesIncludeFilter.True : AuthorizedPartiesIncludeFilter.False);
        sent.IncludeInactiveParties.Should().Be(shouldShowDeletedEntities ? AuthorizedPartiesIncludeFilter.True : AuthorizedPartiesIncludeFilter.False);

        // And the resolved filters reach the ConnectionQuery as IncludeSubConnections and ExcludeDeleted
        var queryFilter = AuthorizedPartyRepoServiceEf.BuildFromOthersFilter(UserSubject.Id, sent, enrichEntities: true, includeDelegationResources: false);
        queryFilter.IncludeSubConnections.Should().Be(shouldShowSubEntities);
        queryFilter.ExcludeDeleted.Should().Be(!shouldShowDeletedEntities);
    }

    [Fact]
    public async Task AutoForUser_ProfileHidingSubEntities_LeavesOutSubunitWithDirectAccess()
    {
        var profile = new ProfileSettingPreference { ShowClientUnits = true, ShouldShowSubEntities = false, ShouldShowDeletedEntities = true };
        var (service, _) = CreateService(featureEnabled: true, [Connection(MainUnit, ConnectionReason.Assignment), Connection(Subunit, ConnectionReason.Assignment)], profile);

        var result = await service.GetAuthorizedPartiesByEntity(UserSubject, Filters(AuthorizedPartiesIncludeFilter.Auto, AuthorizedPartiesIncludeFilter.Auto), TestContext.Current.CancellationToken);

        var mainUnit = result.Should().ContainSingle().Which;
        mainUnit.Subunits.Should().BeEmpty();
    }

    [Fact]
    public async Task AutoForUser_ProfileShowingSubEntities_NestsSubunit()
    {
        var profile = new ProfileSettingPreference { ShowClientUnits = true, ShouldShowSubEntities = true, ShouldShowDeletedEntities = true };
        var (service, _) = CreateService(featureEnabled: true, [Connection(MainUnit, ConnectionReason.Assignment), Connection(Subunit, ConnectionReason.Assignment)], profile);

        var result = await service.GetAuthorizedPartiesByEntity(UserSubject, Filters(AuthorizedPartiesIncludeFilter.Auto, AuthorizedPartiesIncludeFilter.Auto), TestContext.Current.CancellationToken);

        var mainUnit = result.Should().ContainSingle().Which;
        mainUnit.Subunits.Should().ContainSingle(s => s.PartyUuid == Subunit.Id);
    }

    [Fact]
    public async Task AutoForUser_FeatureDisabled_IgnoresProfileHidingSubAndDeletedEntities()
    {
        var profile = new ProfileSettingPreference { ShowClientUnits = true, ShouldShowSubEntities = false, ShouldShowDeletedEntities = false };
        var (service, sentToRepo) = CreateService(featureEnabled: false, [], profile);

        await service.GetAuthorizedPartiesByEntity(UserSubject, Filters(AuthorizedPartiesIncludeFilter.Auto, AuthorizedPartiesIncludeFilter.Auto), TestContext.Current.CancellationToken);

        var sent = sentToRepo.Should().ContainSingle().Which;
        sent.IncludeSubParties.Should().Be(AuthorizedPartiesIncludeFilter.True);
        sent.IncludeInactiveParties.Should().Be(AuthorizedPartiesIncludeFilter.True);
    }

    [Theory]
    [InlineData(AuthorizedPartiesIncludeFilter.True, AuthorizedPartiesIncludeFilter.True, true, false)]
    [InlineData(AuthorizedPartiesIncludeFilter.False, AuthorizedPartiesIncludeFilter.True, false, false)]
    [InlineData(AuthorizedPartiesIncludeFilter.True, AuthorizedPartiesIncludeFilter.False, true, true)]
    [InlineData(AuthorizedPartiesIncludeFilter.False, AuthorizedPartiesIncludeFilter.False, false, true)]
    public void BuildFromOthersFilter_MapsSubAndInactiveParties(AuthorizedPartiesIncludeFilter includeSubParties, AuthorizedPartiesIncludeFilter includeInactiveParties, bool expectedIncludeSubConnections, bool expectedExcludeDeleted)
    {
        var filter = AuthorizedPartyRepoServiceEf.BuildFromOthersFilter(Subject.Id, Filters(includeSubParties, includeInactiveParties), enrichEntities: true, includeDelegationResources: false);

        filter.IncludeSubConnections.Should().Be(expectedIncludeSubConnections);
        filter.ExcludeDeleted.Should().Be(expectedExcludeDeleted);

        // Innehaver connections are persons, not subunits, so includeSubParties must not remove them
        filter.IncludeInnehaverConnections.Should().BeTrue();
    }

    [Fact]
    public void BuildFromOthersFilter_NoFilters_IncludesEverything()
    {
        var filter = AuthorizedPartyRepoServiceEf.BuildFromOthersFilter(Subject.Id, filters: null, enrichEntities: true, includeDelegationResources: false);

        filter.IncludeSubConnections.Should().BeTrue();
        filter.ExcludeDeleted.Should().BeFalse();
    }
}
