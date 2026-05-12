using Altinn.Authorization.ABAC.Constants;
using Altinn.Authorization.ABAC.Xacml;
using Altinn.Authorization.Api.Contracts.Authorization;
using Altinn.Platform.Authorization.Configuration;
using Altinn.Platform.Authorization.Constants;
using Altinn.Platform.Authorization.Models;
using Altinn.Platform.Authorization.Models.AccessManagement;
using Altinn.Platform.Authorization.Models.Oed;
using Altinn.Platform.Authorization.Repositories.Interface;
using Altinn.Platform.Authorization.Services.Implementation;
using Altinn.Platform.Authorization.Services.Interface;
using Altinn.Platform.Authorization.Services.Interfaces;
using Altinn.Platform.Profile.Models;
using Altinn.Platform.Register.Enums;
using Altinn.Platform.Register.Models;
using Authorization.Platform.Authorization.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using Moq;

namespace Altinn.Platform.Authorization.UnitTests;

/// <summary>
/// Unit tests for <see cref="ContextHandler"/> protected methods via a testable subclass.
/// </summary>
public class ContextHandlerUnitTest : IDisposable
{
    private readonly Mock<IInstanceMetadataRepository> _policyInfoRepoMock = new();
    private readonly Mock<IRoles> _rolesMock = new();
    private readonly Mock<IOedRoleAssignmentWrapper> _oedRolesMock = new();
    private readonly Mock<IParties> _partiesMock = new();
    private readonly Mock<IProfile> _profileMock = new();
    private readonly MemoryCache _memoryCache = new(new MemoryCacheOptions());
    private readonly Mock<IRegisterService> _registerServiceMock = new();
    private readonly Mock<IPolicyRetrievalPoint> _prpMock = new();
    private readonly Mock<IAccessManagementWrapper> _accessMgmtMock = new();
    private readonly Mock<IFeatureManager> _featureManagerMock = new();
    private readonly Mock<IResourceRegistry> _resourceRegistryMock = new();
    private readonly TestableContextHandler _sut;
    private TestableContextHandler _cachingSut;

    public ContextHandlerUnitTest()
    {
        _sut = new TestableContextHandler(
            _policyInfoRepoMock.Object,
            _rolesMock.Object,
            _oedRolesMock.Object,
            _partiesMock.Object,
            _profileMock.Object,
            _memoryCache,
            Options.Create(new GeneralSettings { RoleCacheTimeout = 5, MainUnitCacheTimeout = 5 }),
            _registerServiceMock.Object,
            _prpMock.Object,
            _accessMgmtMock.Object,
            _featureManagerMock.Object,
            _resourceRegistryMock.Object);
    }

    public void Dispose() => _memoryCache.Dispose();

    #region GetResourceAttributeValues

    [Fact]
    public void GetResourceAttributeValues_OrgAndApp_ParsedCorrectly()
    {
        var attrs = CreateResourceAttributes(
            (XacmlRequestAttribute.OrgAttribute, "ttd"),
            (XacmlRequestAttribute.AppAttribute, "myapp"));

        var result = _sut.TestGetResourceAttributeValues(attrs);

        Assert.Equal("ttd", result.OrgValue);
        Assert.Equal("myapp", result.AppValue);
    }

    [Fact]
    public void GetResourceAttributeValues_InstanceAttribute_SetsInstanceAndResourceInstance()
    {
        var attrs = CreateResourceAttributes(
            (XacmlRequestAttribute.InstanceAttribute, "50001337/abc-def"));

        var result = _sut.TestGetResourceAttributeValues(attrs);

        Assert.Equal("50001337/abc-def", result.InstanceValue);
        Assert.Equal($"{XacmlRequestAttribute.InstanceAttribute}:50001337/abc-def", result.ResourceInstanceValue);
        Assert.Equal("abc-def", result.AppInstanceIdValue);
    }

    [Fact]
    public void GetResourceAttributeValues_InstanceAttribute_SinglePart_NoResourceInstance()
    {
        var attrs = CreateResourceAttributes(
            (XacmlRequestAttribute.InstanceAttribute, "noslash"));

        var result = _sut.TestGetResourceAttributeValues(attrs);

        Assert.Equal("noslash", result.InstanceValue);
        Assert.Null(result.ResourceInstanceValue);
        Assert.Null(result.AppInstanceIdValue);
    }

    [Fact]
    public void GetResourceAttributeValues_ResourceRegistryInstanceAttribute_ParsesInstanceId()
    {
        string urnValue = $"{XacmlRequestAttribute.InstanceAttribute}:50001337/abc-def";
        var attrs = CreateResourceAttributes(
            (XacmlRequestAttribute.ResourceRegistryInstanceAttribute, urnValue));

        var result = _sut.TestGetResourceAttributeValues(attrs);

        Assert.Equal(urnValue, result.ResourceInstanceValue);
        Assert.Equal("50001337/abc-def", result.InstanceValue);
        Assert.Equal("abc-def", result.AppInstanceIdValue);
    }

    [Fact]
    public void GetResourceAttributeValues_PartyAttribute()
    {
        var attrs = CreateResourceAttributes(
            (XacmlRequestAttribute.PartyAttribute, "50001337"));

        var result = _sut.TestGetResourceAttributeValues(attrs);

        Assert.Equal("50001337", result.ResourcePartyValue);
    }

    [Fact]
    public void GetResourceAttributeValues_PartyUuidAttribute_ValidGuid()
    {
        var guid = Guid.NewGuid();
        var attrs = CreateResourceAttributes(
            (XacmlRequestAttribute.PartyUuidAttribute, guid.ToString()));

        var result = _sut.TestGetResourceAttributeValues(attrs);

        Assert.Equal(guid, result.PartyUuid);
    }

    [Fact]
    public void GetResourceAttributeValues_PartyUuidAttribute_InvalidGuid_RemainsDefault()
    {
        var attrs = CreateResourceAttributes(
            (XacmlRequestAttribute.PartyUuidAttribute, "not-a-guid"));

        var result = _sut.TestGetResourceAttributeValues(attrs);

        Assert.Equal(Guid.Empty, result.PartyUuid);
    }

    [Fact]
    public void GetResourceAttributeValues_TaskAttribute()
    {
        var attrs = CreateResourceAttributes(
            (XacmlRequestAttribute.TaskAttribute, "Task_1"));

        var result = _sut.TestGetResourceAttributeValues(attrs);

        Assert.Equal("Task_1", result.TaskValue);
    }

    [Fact]
    public void GetResourceAttributeValues_EndEventAttribute()
    {
        var attrs = CreateResourceAttributes(
            (XacmlRequestAttribute.EndEventAttribute, "EndEvent_1"));

        var result = _sut.TestGetResourceAttributeValues(attrs);

        Assert.Equal("EndEvent_1", result.EndEventValue);
    }

    [Fact]
    public void GetResourceAttributeValues_AppResourceAttribute()
    {
        var attrs = CreateResourceAttributes(
            (XacmlRequestAttribute.AppResourceAttribute, "events"));

        var result = _sut.TestGetResourceAttributeValues(attrs);

        Assert.Equal("events", result.AppResourceValue);
    }

    [Fact]
    public void GetResourceAttributeValues_ResourceRegistryAttribute_AppPrefix_SetsOrgAndApp()
    {
        var attrs = CreateResourceAttributes(
            (XacmlRequestAttribute.ResourceRegistryAttribute, "app_ttd_myapp"));

        var result = _sut.TestGetResourceAttributeValues(attrs);

        Assert.Equal("ttd", result.OrgValue);
        Assert.Equal("myapp", result.AppValue);
        Assert.Null(result.ResourceRegistryId);
    }

    [Fact]
    public void GetResourceAttributeValues_ResourceRegistryAttribute_NonAppPrefix_SetsResourceRegistryId()
    {
        var attrs = CreateResourceAttributes(
            (XacmlRequestAttribute.ResourceRegistryAttribute, "nav_pensjon"));

        var result = _sut.TestGetResourceAttributeValues(attrs);

        Assert.Equal("nav_pensjon", result.ResourceRegistryId);
        Assert.Null(result.OrgValue);
    }

    [Fact]
    public void GetResourceAttributeValues_OrganizationNumberAttribute()
    {
        var attrs = CreateResourceAttributes(
            (XacmlRequestAttribute.OrganizationNumberAttribute, "910514318"));

        var result = _sut.TestGetResourceAttributeValues(attrs);

        Assert.Equal("910514318", result.OrganizationNumber);
    }

    [Fact]
    public void GetResourceAttributeValues_LegacyOrganizationNumber_SetsWhenEmpty()
    {
        var attrs = CreateResourceAttributes(
            (XacmlRequestAttribute.LegacyOrganizationNumberAttribute, "910514318"));

        var result = _sut.TestGetResourceAttributeValues(attrs);

        Assert.Equal("910514318", result.OrganizationNumber);
    }

    [Fact]
    public void GetResourceAttributeValues_LegacyOrganizationNumber_SkippedWhenNewPresent()
    {
        var attrs = CreateResourceAttributes(
            (XacmlRequestAttribute.OrganizationNumberAttribute, "111111111"),
            (XacmlRequestAttribute.LegacyOrganizationNumberAttribute, "222222222"));

        var result = _sut.TestGetResourceAttributeValues(attrs);

        Assert.Equal("111111111", result.OrganizationNumber);
    }

    [Fact]
    public void GetResourceAttributeValues_PersonIdAttribute()
    {
        var attrs = CreateResourceAttributes(
            (XacmlRequestAttribute.PersonIdAttribute, "01017012345"));

        var result = _sut.TestGetResourceAttributeValues(attrs);

        Assert.Equal("01017012345", result.PersonId);
    }

    #endregion

    #region AddIfValueDoesNotExist

    [Fact]
    public void AddIfValueDoesNotExist_EmptyValue_AddsAttribute()
    {
        var attrs = CreateResourceAttributes();
        int before = attrs.Attributes.Count;

        _sut.TestAddIfValueDoesNotExist(attrs, XacmlRequestAttribute.OrgAttribute, null, "ttd");

        Assert.Equal(before + 1, attrs.Attributes.Count);
    }

    [Fact]
    public void AddIfValueDoesNotExist_ExistingValue_DoesNotAdd()
    {
        var attrs = CreateResourceAttributes();
        int before = attrs.Attributes.Count;

        _sut.TestAddIfValueDoesNotExist(attrs, XacmlRequestAttribute.OrgAttribute, "already-set", "ttd");

        Assert.Equal(before, attrs.Attributes.Count);
    }

    #endregion

    #region GetAttribute

    [Fact]
    public void GetAttribute_PartyAttribute_SetsIncludeInResult()
    {
        var attr = _sut.TestGetAttribute(XacmlRequestAttribute.PartyAttribute, "50001337");

        Assert.True(attr.IncludeInResult);
        Assert.Equal("50001337", attr.AttributeValues.First().Value);
    }

    [Fact]
    public void GetAttribute_NonPartyAttribute_DoesNotSetIncludeInResult()
    {
        var attr = _sut.TestGetAttribute(XacmlRequestAttribute.OrgAttribute, "ttd");

        Assert.False(attr.IncludeInResult);
        Assert.Equal("ttd", attr.AttributeValues.First().Value);
    }

    #endregion

    #region EnrichResourceParty

    [Fact]
    public async Task EnrichResourceParty_OrgNumber_LooksUpParty()
    {
        var party = new Party { PartyId = 50001337 };
        _registerServiceMock
            .Setup(r => r.PartyLookup("910514318", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(party);

        var resourceAttrs = new XacmlResourceAttributes { OrganizationNumber = "910514318" };
        var contextAttrs = CreateResourceAttributes();

        await _sut.TestEnrichResourceParty(contextAttrs, resourceAttrs, false, TestContext.Current.CancellationToken);

        Assert.Equal("50001337", resourceAttrs.ResourcePartyValue);
        Assert.Contains(contextAttrs.Attributes, a =>
            a.AttributeId.OriginalString == XacmlRequestAttribute.PartyAttribute &&
            a.AttributeValues.Any(v => v.Value == "50001337"));
    }

    [Fact]
    public async Task EnrichResourceParty_PersonId_ExternalRequest_LooksUpParty()
    {
        var party = new Party { PartyId = 50001338 };
        _registerServiceMock
            .Setup(r => r.PartyLookup(null, "01017012345", It.IsAny<CancellationToken>()))
            .ReturnsAsync(party);

        var resourceAttrs = new XacmlResourceAttributes { PersonId = "01017012345" };
        var contextAttrs = CreateResourceAttributes();

        await _sut.TestEnrichResourceParty(contextAttrs, resourceAttrs, true, TestContext.Current.CancellationToken);

        Assert.Equal("50001338", resourceAttrs.ResourcePartyValue);
    }

    [Fact]
    public async Task EnrichResourceParty_PersonId_InternalRequest_Throws()
    {
        var resourceAttrs = new XacmlResourceAttributes { PersonId = "01017012345" };
        var contextAttrs = CreateResourceAttributes();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.TestEnrichResourceParty(contextAttrs, resourceAttrs, false, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task EnrichResourceParty_PartyUuid_LooksUpParty()
    {
        var uuid = Guid.NewGuid();
        var party = new Party { PartyId = 50001339 };
        _registerServiceMock
            .Setup(r => r.GetPartiesAsync(It.Is<List<Guid>>(l => l.Contains(uuid)), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Party> { party });

        var resourceAttrs = new XacmlResourceAttributes { PartyUuid = uuid };
        var contextAttrs = CreateResourceAttributes();

        await _sut.TestEnrichResourceParty(contextAttrs, resourceAttrs, false, TestContext.Current.CancellationToken);

        Assert.Equal("50001339", resourceAttrs.ResourcePartyValue);
    }

    [Fact]
    public async Task EnrichResourceParty_AlreadyHasParty_NoLookup()
    {
        var resourceAttrs = new XacmlResourceAttributes { ResourcePartyValue = "50001337" };
        var contextAttrs = CreateResourceAttributes();

        await _sut.TestEnrichResourceParty(contextAttrs, resourceAttrs, false, TestContext.Current.CancellationToken);

        _registerServiceMock.Verify(r => r.PartyLookup(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Cached lookups

    [Fact]
    public async Task GetRoles_CachesResult()
    {
        var roles = new List<Role> { new Role { Value = "DAGL" } };
        _rolesMock.Setup(r => r.GetDecisionPointRolesForUser(1, 2)).ReturnsAsync(roles);

        var first = await _sut.TestGetRoles(1, 2);
        var second = await _sut.TestGetRoles(1, 2);

        Assert.Same(first, second);
        _rolesMock.Verify(r => r.GetDecisionPointRolesForUser(1, 2), Times.Once);
    }

    [Fact]
    public async Task GetUserProfileByUserId_CachesAndCrossPopulatesSsn()
    {
        var profile = new UserProfile
        {
            UserId = 42,
            Party = new Party { SSN = "01017012345", PartyTypeName = PartyType.Person }
        };
        _profileMock.Setup(p => p.GetUserProfile(42, It.IsAny<CancellationToken>())).ReturnsAsync(profile);

        var result = await _sut.TestGetUserProfileByUserId(42, TestContext.Current.CancellationToken);

        Assert.Equal(42, result.UserId);
        _profileMock.Verify(p => p.GetUserProfile(42, It.IsAny<CancellationToken>()), Times.Once);

        // Second call should come from cache
        var cached = await _sut.TestGetUserProfileByUserId(42, TestContext.Current.CancellationToken);
        Assert.Same(result, cached);
        _profileMock.Verify(p => p.GetUserProfile(42, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetUserProfileByPersonId_CachesAndCrossPopulatesUserId()
    {
        var profile = new UserProfile
        {
            UserId = 42,
            Party = new Party { SSN = "01017012345", PartyTypeName = PartyType.Person }
        };
        _profileMock.Setup(p => p.GetUserProfileByPersonId("01017012345", It.IsAny<CancellationToken>())).ReturnsAsync(profile);

        var result = await _sut.TestGetUserProfileByPersonId("01017012345", TestContext.Current.CancellationToken);

        Assert.Equal(42, result.UserId);

        // Second call should come from cache
        var cached = await _sut.TestGetUserProfileByPersonId("01017012345", TestContext.Current.CancellationToken);
        Assert.Same(result, cached);
        _profileMock.Verify(p => p.GetUserProfileByPersonId("01017012345", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetKeyRolePartyIds_CachesResult()
    {
        _partiesMock.Setup(p => p.GetKeyRoleParties(1, It.IsAny<CancellationToken>())).ReturnsAsync(new List<int> { 100, 200 });

        var first = await _sut.TestGetKeyRolePartyIds(1, TestContext.Current.CancellationToken);
        var second = await _sut.TestGetKeyRolePartyIds(1, TestContext.Current.CancellationToken);

        Assert.Same(first, second);
        _partiesMock.Verify(p => p.GetKeyRoleParties(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMainUnits_CachesResult()
    {
        _partiesMock.Setup(p => p.GetMainUnits(It.IsAny<MainUnitQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MainUnit> { new MainUnit { PartyId = 10 } });

        var first = await _sut.TestGetMainUnits(1, TestContext.Current.CancellationToken);
        var second = await _sut.TestGetMainUnits(1, TestContext.Current.CancellationToken);

        Assert.Same(first, second);
        _partiesMock.Verify(p => p.GetMainUnits(It.IsAny<MainUnitQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetOedRoleAssignments_CachesResult()
    {
        var assignments = new List<OedRoleAssignment> { new OedRoleAssignment { OedRoleCode = "role1" } };
        _oedRolesMock.Setup(o => o.GetOedRoleAssignments("from", "to")).ReturnsAsync(assignments);

        var first = await _sut.TestGetOedRoleAssignments("from", "to");
        var second = await _sut.TestGetOedRoleAssignments("from", "to");

        Assert.Same(first, second);
        _oedRolesMock.Verify(o => o.GetOedRoleAssignments("from", "to"), Times.Once);
    }

    #endregion

    #region Attribute builder helpers

    [Fact]
    public void GetRoleAttribute_BuildsCorrectly()
    {
        var roles = new List<Role> { new() { Value = "DAGL" }, new() { Value = "REGNA" } };

        var attr = _sut.TestGetRoleAttribute(roles);

        Assert.Equal(XacmlRequestAttribute.RoleAttribute, attr.AttributeId.OriginalString);
        Assert.Equal(2, attr.AttributeValues.Count);
        Assert.Equal("DAGL", attr.AttributeValues.First().Value);
        Assert.Equal("REGNA", attr.AttributeValues.Last().Value);
    }

    [Fact]
    public void GetPartyTypeAttribute_Organization()
    {
        var attr = _sut.TestGetPartyTypeAttribute(PartyType.Organisation);

        Assert.Equal(XacmlRequestAttribute.PartyTypeAttribute, attr.AttributeId.OriginalString);
        Assert.Equal(XacmlRequestAttribute.PartyTypeOrganizationValue, attr.AttributeValues.First().Value);
    }

    [Fact]
    public void GetPartyTypeAttribute_Person()
    {
        var attr = _sut.TestGetPartyTypeAttribute(PartyType.Person);

        Assert.Equal(XacmlRequestAttribute.PartyTypePersonValue, attr.AttributeValues.First().Value);
    }

    [Fact]
    public void GetPartyIdsAttribute_MultipleIds()
    {
        var attr = _sut.TestGetPartyIdsAttribute(new List<int> { 1, 2, 3 });

        Assert.Equal(XacmlRequestAttribute.PartyAttribute, attr.AttributeId.OriginalString);
        Assert.Equal(3, attr.AttributeValues.Count);
    }

    #endregion

    #region EnrichSubjectAttributes with AccessManagementAsPipForRoles

    [Fact]
    public async Task EnrichSubjectAttributes_WithFeatureFlag_EnrichesRolesFromAccessManagement()
    {
        // Arrange
        int subjectUserId = 1001;
        int resourcePartyId = 50001337;
        Guid subjectPartyUuid = Guid.Parse("00000000-0000-0000-0000-000000001001");
        Guid resourcePartyUuid = Guid.Parse("00000000-0000-0000-0000-000000050001");

        var pipResponse = new PipResponseDto
        {
            Roles =
            [
                RoleUrn.Parse("urn:altinn:external-role:ccr:daglig-leder"),
                RoleUrn.Parse("urn:altinn:rolecode:dagl"),
                RoleUrn.Parse("urn:altinn:rolecode:hadm"),
            ],
            AccessPackages = [],
        };

        var cachingWrapper = SetupEnrichSubjectWithCachingWrapper(subjectUserId, resourcePartyId, subjectPartyUuid, resourcePartyUuid, pipResponse, policyHasRoles: true, policyHasAccessPackages: false);

        var (request, resourceAttrs) = CreateEnrichSubjectRequest(subjectUserId, resourcePartyId, resourcePartyUuid);

        // Act
        await _cachingSut.TestEnrichSubjectAttributes(request, resourceAttrs, isExternalRequest: false, TestContext.Current.CancellationToken);

        // Assert
        var subjectAttrs = request.GetSubjectAttributes();
        AssertContainsAttributeValue(subjectAttrs, "urn:altinn:external-role:ccr", "daglig-leder");
        AssertContainsAttributeValue(subjectAttrs, "urn:altinn:rolecode", "dagl");
        AssertContainsAttributeValue(subjectAttrs, "urn:altinn:rolecode", "hadm");

        Assert.Equal(1, cachingWrapper.GetRolesAndAccessPackagesCallCount);
    }

    [Fact]
    public async Task EnrichSubjectAttributes_WithFeatureFlag_EnrichesAccessPackagesFromAccessManagement()
    {
        // Arrange
        int subjectUserId = 1001;
        int resourcePartyId = 50001337;
        Guid subjectPartyUuid = Guid.Parse("00000000-0000-0000-0000-000000001001");
        Guid resourcePartyUuid = Guid.Parse("00000000-0000-0000-0000-000000050001");

        var pipResponse = new PipResponseDto
        {
            Roles = [],
            AccessPackages =
            [
                AccessPackageUrn.Parse("urn:altinn:accesspackage:klientadministrator"),
                AccessPackageUrn.Parse("urn:altinn:accesspackage:tilgangsstyrer"),
            ],
        };

        var cachingWrapper = SetupEnrichSubjectWithCachingWrapper(subjectUserId, resourcePartyId, subjectPartyUuid, resourcePartyUuid, pipResponse, policyHasRoles: false, policyHasAccessPackages: true);

        var (request, resourceAttrs) = CreateEnrichSubjectRequest(subjectUserId, resourcePartyId, resourcePartyUuid);

        // Act
        await _cachingSut.TestEnrichSubjectAttributes(request, resourceAttrs, isExternalRequest: false, TestContext.Current.CancellationToken);

        // Assert
        var subjectAttrs = request.GetSubjectAttributes();
        AssertContainsAttributeValue(subjectAttrs, "urn:altinn:accesspackage", "klientadministrator");
        AssertContainsAttributeValue(subjectAttrs, "urn:altinn:accesspackage", "tilgangsstyrer");

        Assert.Equal(1, cachingWrapper.GetRolesAndAccessPackagesCallCount);
    }

    [Fact]
    public async Task EnrichSubjectAttributes_WithFeatureFlag_EnrichesRolesAndAccessPackages_CacheEnsuresSingleApiCall()
    {
        // Arrange
        int subjectUserId = 1001;
        int resourcePartyId = 50001337;
        Guid subjectPartyUuid = Guid.Parse("00000000-0000-0000-0000-000000001001");
        Guid resourcePartyUuid = Guid.Parse("00000000-0000-0000-0000-000000050001");

        var pipResponse = new PipResponseDto
        {
            Roles =
            [
                RoleUrn.Parse("urn:altinn:external-role:ccr:daglig-leder"),
                RoleUrn.Parse("urn:altinn:rolecode:dagl"),
            ],
            AccessPackages =
            [
                AccessPackageUrn.Parse("urn:altinn:accesspackage:hovedadministrator"),
            ],
        };

        var cachingWrapper = SetupEnrichSubjectWithCachingWrapper(subjectUserId, resourcePartyId, subjectPartyUuid, resourcePartyUuid, pipResponse, policyHasRoles: true, policyHasAccessPackages: true);

        var (request, resourceAttrs) = CreateEnrichSubjectRequest(subjectUserId, resourcePartyId, resourcePartyUuid);

        // Act
        await _cachingSut.TestEnrichSubjectAttributes(request, resourceAttrs, isExternalRequest: false, TestContext.Current.CancellationToken);

        // Assert
        var subjectAttrs = request.GetSubjectAttributes();
        AssertContainsAttributeValue(subjectAttrs, "urn:altinn:external-role:ccr", "daglig-leder");
        AssertContainsAttributeValue(subjectAttrs, "urn:altinn:rolecode", "dagl");
        AssertContainsAttributeValue(subjectAttrs, "urn:altinn:accesspackage", "hovedadministrator");

        // Both AddRoleAttributes and AddAccessPackageAttributes call GetRolesAndAccessPackages,
        // but the cache ensures only one actual fetch occurs
        Assert.Equal(1, cachingWrapper.GetRolesAndAccessPackagesCallCount);
    }

    [Fact]
    public async Task EnrichSubjectAttributes_WithoutFeatureFlag_UsesLegacyRolesEndpoint()
    {
        // Arrange
        int subjectUserId = 1001;
        int resourcePartyId = 50001337;
        Guid subjectPartyUuid = Guid.Parse("00000000-0000-0000-0000-000000001001");

        var legacyRoles = new List<Role> { new() { Value = "DAGL" }, new() { Value = "HADM" } };

        _featureManagerMock.Setup(f => f.IsEnabledAsync(FeatureFlags.AccessManagementAsPipForRoles)).ReturnsAsync(false);
        _featureManagerMock.Setup(f => f.IsEnabledAsync(FeatureFlags.UserAccessPackageAuthorization)).ReturnsAsync(false);
        _featureManagerMock.Setup(f => f.IsEnabledAsync(FeatureFlags.SystemUserAccessPackageAuthorization)).ReturnsAsync(false);

        _profileMock.Setup(p => p.GetUserProfile(subjectUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile { UserId = subjectUserId, Party = new Party { SSN = "01017012345", PartyTypeName = PartyType.Person, PartyUuid = subjectPartyUuid } });

        _rolesMock.Setup(r => r.GetDecisionPointRolesForUser(subjectUserId, resourcePartyId)).ReturnsAsync(legacyRoles);

        var policy = CreatePolicyWithSubjectAttributes(policyHasRoles: true, policyHasAccessPackages: false);
        _prpMock.Setup(p => p.GetPolicyAsync(It.IsAny<XacmlContextRequest>())).ReturnsAsync(policy);

        var (request, resourceAttrs) = CreateEnrichSubjectRequest(subjectUserId, resourcePartyId);

        // Act
        await _sut.TestEnrichSubjectAttributes(request, resourceAttrs, isExternalRequest: false, TestContext.Current.CancellationToken);

        // Assert
        var subjectAttrs = request.GetSubjectAttributes();
        AssertContainsAttributeValue(subjectAttrs, XacmlRequestAttribute.RoleAttribute, "DAGL");
        AssertContainsAttributeValue(subjectAttrs, XacmlRequestAttribute.RoleAttribute, "HADM");

        _accessMgmtMock.Verify(a => a.GetRolesAndAccessPackages(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _rolesMock.Verify(r => r.GetDecisionPointRolesForUser(subjectUserId, resourcePartyId), Times.Once);
    }

    [Fact]
    public async Task EnrichSubjectAttributes_WithFeatureFlag_EmptyPipResponse_NoAttributesAdded()
    {
        // Arrange
        int subjectUserId = 1001;
        int resourcePartyId = 50001337;
        Guid subjectPartyUuid = Guid.Parse("00000000-0000-0000-0000-000000001001");
        Guid resourcePartyUuid = Guid.Parse("00000000-0000-0000-0000-000000050001");

        var pipResponse = new PipResponseDto { Roles = [], AccessPackages = [] };

        SetupEnrichSubjectWithCachingWrapper(subjectUserId, resourcePartyId, subjectPartyUuid, resourcePartyUuid, pipResponse, policyHasRoles: true, policyHasAccessPackages: true);

        var (request, resourceAttrs) = CreateEnrichSubjectRequest(subjectUserId, resourcePartyId, resourcePartyUuid);

        // Act
        await _cachingSut.TestEnrichSubjectAttributes(request, resourceAttrs, isExternalRequest: false, TestContext.Current.CancellationToken);

        // Assert: only the original user attribute should remain (no role/accesspackage attributes added)
        var subjectAttrs = request.GetSubjectAttributes();
        Assert.Single(subjectAttrs.Attributes);
        Assert.Equal(XacmlRequestAttribute.UserAttribute, subjectAttrs.Attributes.First().AttributeId.OriginalString);
    }

    /// <summary>
    /// Sets up mocks and creates a <see cref="TestableContextHandler"/> backed by a
    /// <see cref="CachingAccessManagementWrapperStub"/> so that cache behaviour can be verified.
    /// </summary>
    private CachingAccessManagementWrapperStub SetupEnrichSubjectWithCachingWrapper(
        int subjectUserId, int resourcePartyId, Guid subjectPartyUuid, Guid resourcePartyUuid,
        PipResponseDto pipResponse, bool policyHasRoles, bool policyHasAccessPackages)
    {
        _featureManagerMock.Setup(f => f.IsEnabledAsync(FeatureFlags.AccessManagementAsPipForRoles)).ReturnsAsync(true);
        _featureManagerMock.Setup(f => f.IsEnabledAsync(FeatureFlags.UserAccessPackageAuthorization)).ReturnsAsync(true);
        _featureManagerMock.Setup(f => f.IsEnabledAsync(FeatureFlags.SystemUserAccessPackageAuthorization)).ReturnsAsync(false);

        _profileMock.Setup(p => p.GetUserProfile(subjectUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile { UserId = subjectUserId, Party = new Party { SSN = "01017012345", PartyTypeName = PartyType.Person, PartyUuid = subjectPartyUuid } });

        _registerServiceMock.Setup(r => r.GetPartiesAsync(It.Is<List<int>>(l => l.Contains(resourcePartyId)), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Party> { new() { PartyId = resourcePartyId, PartyUuid = resourcePartyUuid } });

        var policy = CreatePolicyWithSubjectAttributes(policyHasRoles, policyHasAccessPackages);
        _prpMock.Setup(p => p.GetPolicyAsync(It.IsAny<XacmlContextRequest>())).ReturnsAsync(policy);

        var cachingWrapper = new CachingAccessManagementWrapperStub(_memoryCache, pipResponse);

        _cachingSut = new TestableContextHandler(
            _policyInfoRepoMock.Object,
            _rolesMock.Object,
            _oedRolesMock.Object,
            _partiesMock.Object,
            _profileMock.Object,
            _memoryCache,
            Options.Create(new GeneralSettings { RoleCacheTimeout = 5, MainUnitCacheTimeout = 5 }),
            _registerServiceMock.Object,
            _prpMock.Object,
            cachingWrapper,
            _featureManagerMock.Object,
            _resourceRegistryMock.Object);

        return cachingWrapper;
    }

    private static (XacmlContextRequest request, XacmlResourceAttributes resourceAttrs) CreateEnrichSubjectRequest(int subjectUserId, int resourcePartyId, Guid? resourcePartyUuid = null)
    {
        var subjectCategory = new XacmlContextAttributes(new Uri(XacmlConstants.MatchAttributeCategory.Subject));
        var userAttr = new XacmlAttribute(new Uri(XacmlRequestAttribute.UserAttribute), false);
        userAttr.AttributeValues.Add(new XacmlAttributeValue(new Uri(XacmlConstants.DataTypes.XMLString), subjectUserId.ToString()));
        subjectCategory.Attributes.Add(userAttr);

        var resourceCategory = new XacmlContextAttributes(new Uri(XacmlConstants.MatchAttributeCategory.Resource));
        var request = new XacmlContextRequest(false, false, new[] { subjectCategory, resourceCategory });

        var resourceAttrs = new XacmlResourceAttributes
        {
            ResourcePartyValue = resourcePartyId.ToString(),
            ResourceRegistryId = "test-resource",
            PartyUuid = resourcePartyUuid ?? Guid.Empty,
        };

        return (request, resourceAttrs);
    }

    private static XacmlPolicy CreatePolicyWithSubjectAttributes(bool policyHasRoles, bool policyHasAccessPackages)
    {
        var matches = new List<XacmlMatch>();

        if (policyHasRoles)
        {
            matches.Add(CreateSubjectMatch(AltinnXacmlConstants.MatchAttributeIdentifiers.RoleAttribute, "DAGL"));
        }

        if (policyHasAccessPackages)
        {
            matches.Add(CreateSubjectMatch(AltinnXacmlConstants.MatchAttributeIdentifiers.AccessPackageAttribute, "klientadministrator"));
        }

        if (matches.Count == 0)
        {
            // Policy with no rules
            return new XacmlPolicy(new Uri("urn:test:policy"), new Uri("urn:oasis:names:tc:xacml:3.0:rule-combining-algorithm:deny-overrides"), new XacmlTarget(Enumerable.Empty<XacmlAnyOf>()));
        }

        var allOf = new XacmlAllOf(matches);
        var anyOf = new XacmlAnyOf(new[] { allOf });
        var target = new XacmlTarget(new[] { anyOf });
        var rule = new XacmlRule("rule1", XacmlEffectType.Permit) { Target = target };

        var policy = new XacmlPolicy(new Uri("urn:test:policy"), new Uri("urn:oasis:names:tc:xacml:3.0:rule-combining-algorithm:deny-overrides"), new XacmlTarget(Enumerable.Empty<XacmlAnyOf>()));
        policy.Rules.Add(rule);

        return policy;
    }

    private static XacmlMatch CreateSubjectMatch(string attributeId, string value)
    {
        var attrValue = new XacmlAttributeValue(new Uri(XacmlConstants.DataTypes.XMLString), value);
        var designator = new XacmlAttributeDesignator(
            new Uri(XacmlConstants.MatchAttributeCategory.Subject),
            new Uri(attributeId),
            new Uri(XacmlConstants.DataTypes.XMLString),
            false);

        return new XacmlMatch(new Uri("urn:oasis:names:tc:xacml:1.0:function:string-equal"), attrValue, designator);
    }

    private static void AssertContainsAttributeValue(XacmlContextAttributes contextAttributes, string attributeId, string expectedValue)
    {
        var matchingAttrs = contextAttributes.Attributes
            .Where(a => a.AttributeId.OriginalString == attributeId)
            .SelectMany(a => a.AttributeValues)
            .Where(v => v.Value == expectedValue)
            .ToList();

        Assert.True(matchingAttrs.Count > 0, $"Expected attribute '{attributeId}' with value '{expectedValue}' not found in subject attributes.");
    }

    #endregion

    #region Helpers

    private static XacmlContextAttributes CreateResourceAttributes(params (string id, string value)[] attributes)
    {
        var category = new Uri(XacmlConstants.MatchAttributeCategory.Resource);
        var ctx = new XacmlContextAttributes(category);
        foreach (var (id, value) in attributes)
        {
            var attr = new XacmlAttribute(new Uri(id), false);
            attr.AttributeValues.Add(new XacmlAttributeValue(new Uri(XacmlConstants.DataTypes.XMLString), value));
            ctx.Attributes.Add(attr);
        }

        return ctx;
    }

    #endregion

    /// <summary>
    /// Testable subclass exposing protected methods of <see cref="ContextHandler"/>.
    /// </summary>
    private class TestableContextHandler : ContextHandler
    {
        public TestableContextHandler(
            IInstanceMetadataRepository policyInformationRepository,
            IRoles rolesWrapper,
            IOedRoleAssignmentWrapper oedRolesWrapper,
            IParties partiesWrapper,
            IProfile profileWrapper,
            IMemoryCache memoryCache,
            IOptions<GeneralSettings> settings,
            IRegisterService registerService,
            IPolicyRetrievalPoint prp,
            IAccessManagementWrapper accessManagementWrapper,
            IFeatureManager featureManager,
            IResourceRegistry resourceRegistry)
            : base(policyInformationRepository, rolesWrapper, oedRolesWrapper, partiesWrapper, profileWrapper, memoryCache, settings, registerService, prp, accessManagementWrapper, featureManager, resourceRegistry)
        {
        }

        public XacmlResourceAttributes TestGetResourceAttributeValues(XacmlContextAttributes attrs)
            => GetResourceAttributeValues(attrs);

        public void TestAddIfValueDoesNotExist(XacmlContextAttributes attrs, string attributeId, string attributeValue, string newValue)
            => AddIfValueDoesNotExist(attrs, attributeId, attributeValue, newValue);

        public XacmlAttribute TestGetAttribute(string attributeId, string value)
            => GetAttribute(attributeId, value);

        public Task TestEnrichResourceParty(XacmlContextAttributes attrs, XacmlResourceAttributes resourceAttrs, bool isExternal, CancellationToken ct = default)
            => EnrichResourceParty(attrs, resourceAttrs, isExternal, ct);

        public Task<List<Role>> TestGetRoles(int userId, int partyId)
            => GetRoles(userId, partyId);

        public Task<UserProfile> TestGetUserProfileByUserId(int userId, CancellationToken ct = default)
            => GetUserProfileByUserId(userId, ct);

        public Task<UserProfile> TestGetUserProfileByPersonId(string personId, CancellationToken ct = default)
            => GetUserProfileByPersonId(personId, ct);

        public Task<List<int>> TestGetKeyRolePartyIds(int userId, CancellationToken ct = default)
            => GetKeyRolePartyIds(userId, ct);

        public Task<List<MainUnit>> TestGetMainUnits(int partyId, CancellationToken ct = default)
            => GetMainUnits(partyId, ct);

        public Task<List<OedRoleAssignment>> TestGetOedRoleAssignments(string from, string to)
            => GetOedRoleAssignments(from, to);

        public Task TestEnrichSubjectAttributes(XacmlContextRequest request, XacmlResourceAttributes resourceAttrs, bool isExternalRequest, CancellationToken ct = default)
            => EnrichSubjectAttributes(request, resourceAttrs, isExternalRequest, ct);

        public XacmlAttribute TestGetRoleAttribute(List<Role> roles)
            => GetRoleAttribute(roles);

        public XacmlAttribute TestGetPartyTypeAttribute(PartyType partyType)
            => GetPartyTypeAttribute(partyType);

        public XacmlAttribute TestGetPartyIdsAttribute(List<int> partyIds)
            => GetPartyIdsAttribute(partyIds);
    }

    /// <summary>
    /// A test double for <see cref="IAccessManagementWrapper"/> that wraps <see cref="GetRolesAndAccessPackages"/>
    /// with the same <see cref="IMemoryCache"/> logic as the real <see cref="AccessManagementWrapper"/>.
    /// Tracks how many times the underlying data source was actually consulted (cache misses).
    /// </summary>
    private sealed class CachingAccessManagementWrapperStub : IAccessManagementWrapper
    {
        private readonly IMemoryCache _memoryCache;
        private readonly PipResponseDto _pipResponse;
        private int _getRolesAndAccessPackagesCallCount;

        public int GetRolesAndAccessPackagesCallCount => _getRolesAndAccessPackagesCallCount;

        public CachingAccessManagementWrapperStub(IMemoryCache memoryCache, PipResponseDto pipResponse)
        {
            _memoryCache = memoryCache;
            _pipResponse = pipResponse;
        }

        public Task<PipResponseDto> GetRolesAndAccessPackages(Guid to, Guid from, CancellationToken cancellationToken = default)
        {
            var cacheKey = $"RolesAndAccPkgs|f:{from}|t:{to}";

            if (!_memoryCache.TryGetValue(cacheKey, out PipResponseDto result))
            {
                Interlocked.Increment(ref _getRolesAndAccessPackagesCallCount);
                result = _pipResponse;

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetPriority(CacheItemPriority.High)
                    .SetAbsoluteExpiration(new TimeSpan(0, 5, 0));

                _memoryCache.Set(cacheKey, result, cacheEntryOptions);
            }

            return Task.FromResult(result);
        }

        public Task<IEnumerable<AccessPackageUrn>> GetAccessPackages(Guid to, Guid from, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<IEnumerable<DelegationChangeExternal>> GetAllDelegationChanges(DelegationChangeInput input, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<IEnumerable<DelegationChangeExternal>> GetAllDelegationChanges(CancellationToken cancellationToken = default, params Action<DelegationChangeInput>[] actions)
            => throw new NotImplementedException();

        public Task<IEnumerable<AuthorizedPartyDto>> GetAuthorizedParties(CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<AuthorizedPartyDto> GetAuthorizedParty(int partyId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
    }
}
