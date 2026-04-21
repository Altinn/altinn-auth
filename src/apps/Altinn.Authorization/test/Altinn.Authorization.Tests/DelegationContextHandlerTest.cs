using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Altinn.Authorization.ABAC.Constants;
using Altinn.Authorization.ABAC.Xacml;
using Altinn.Platform.Authorization.Configuration;
using Altinn.Platform.Authorization.Constants;
using Altinn.Platform.Authorization.Models;
using Altinn.Platform.Authorization.Repositories.Interface;
using Altinn.Platform.Authorization.Services.Implementation;
using Altinn.Platform.Authorization.Services.Interface;
using Altinn.Platform.Authorization.Services.Interfaces;
using Altinn.Platform.Profile.Models;
using Altinn.Platform.Register.Enums;
using Altinn.Platform.Register.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using Moq;
using Xunit;

namespace Altinn.Platform.Authorization.UnitTests;

public class DelegationContextHandlerTest : IDisposable
{
    private static readonly Uri StringDataType = new("http://www.w3.org/2001/XMLSchema#string");

    private readonly Mock<IInstanceMetadataRepository> _policyInfoRepoMock = new();
    private readonly Mock<IRoles> _rolesMock = new();
    private readonly Mock<IOedRoleAssignmentWrapper> _oedRolesMock = new();
    private readonly Mock<IParties> _partiesMock = new();
    private readonly Mock<IProfile> _profileMock = new();
    private readonly IMemoryCache _memoryCache = new MemoryCache(new MemoryCacheOptions());
    private readonly Mock<IRegisterService> _registerServiceMock = new();
    private readonly Mock<IPolicyRetrievalPoint> _prpMock = new();
    private readonly Mock<IAccessManagementWrapper> _accMgmtMock = new();
    private readonly Mock<IFeatureManager> _featureManagerMock = new();
    private readonly Mock<IResourceRegistry> _resourceRegistryMock = new();

    private readonly DelegationContextHandler _sut;

    public DelegationContextHandlerTest()
    {
        var settings = Options.Create(new GeneralSettings { RoleCacheTimeout = 5, MainUnitCacheTimeout = 5 });
        _sut = new DelegationContextHandler(
            _policyInfoRepoMock.Object, _rolesMock.Object, _oedRolesMock.Object, _partiesMock.Object,
            _profileMock.Object, _memoryCache, settings, _registerServiceMock.Object,
            _prpMock.Object, _accMgmtMock.Object, _featureManagerMock.Object, _resourceRegistryMock.Object);
    }

    public void Dispose() => _memoryCache.Dispose();

    #region GetSubjectUserId

    [Fact]
    public void GetSubjectUserId_WithUserAttribute_ReturnsUserId()
    {
        var attrs = CreateSubjectAttributes((XacmlRequestAttribute.UserAttribute, "12345"));
        Assert.Equal(12345, _sut.GetSubjectUserId(attrs));
    }

    [Fact]
    public void GetSubjectUserId_NoUserAttribute_ReturnsZero()
    {
        var attrs = CreateSubjectAttributes();
        Assert.Equal(0, _sut.GetSubjectUserId(attrs));
    }

    #endregion

    #region GetSubjectPartyId

    [Fact]
    public void GetSubjectPartyId_WithPartyAttribute_ReturnsPartyId()
    {
        var attrs = CreateSubjectAttributes((XacmlRequestAttribute.PartyAttribute, "50001"));
        Assert.Equal(50001, _sut.GetSubjectPartyId(attrs));
    }

    [Fact]
    public void GetSubjectPartyId_NoPartyAttribute_ReturnsZero()
    {
        var attrs = CreateSubjectAttributes();
        Assert.Equal(0, _sut.GetSubjectPartyId(attrs));
    }

    #endregion

    #region GetSubjectAttributeMatch

    [Fact]
    public void GetSubjectAttributeMatch_FirstPriorityFound_ReturnsThat()
    {
        var request = CreateRequestWithSubject(
            (XacmlRequestAttribute.PersonUuidAttribute, "abc-123"),
            (XacmlRequestAttribute.UserAttribute, "999"));

        var result = _sut.GetSubjectAttributeMatch(request, [XacmlRequestAttribute.PersonUuidAttribute, XacmlRequestAttribute.UserAttribute]);

        Assert.NotNull(result);
        Assert.Equal(XacmlRequestAttribute.PersonUuidAttribute, result.Id);
        Assert.Equal("abc-123", result.Value);
    }

    [Fact]
    public void GetSubjectAttributeMatch_FallsBackToSecondPriority()
    {
        var request = CreateRequestWithSubject((XacmlRequestAttribute.UserAttribute, "999"));

        var result = _sut.GetSubjectAttributeMatch(request, [XacmlRequestAttribute.PersonUuidAttribute, XacmlRequestAttribute.UserAttribute]);

        Assert.NotNull(result);
        Assert.Equal(XacmlRequestAttribute.UserAttribute, result.Id);
        Assert.Equal("999", result.Value);
    }

    [Fact]
    public void GetSubjectAttributeMatch_NoMatch_ReturnsNull()
    {
        var request = CreateRequestWithSubject();
        var result = _sut.GetSubjectAttributeMatch(request, [XacmlRequestAttribute.PersonUuidAttribute]);
        Assert.Null(result);
    }

    #endregion

    #region GetActionString

    [Fact]
    public void GetActionString_WithActionAttribute_ReturnsValue()
    {
        var request = CreateRequestWithAction("read");
        Assert.Equal("read", _sut.GetActionString(request));
    }

    [Fact]
    public void GetActionString_NoActionCategory_ReturnsNull()
    {
        var request = new XacmlContextRequest(false, false, []);
        Assert.Null(_sut.GetActionString(request));
    }

    #endregion

    #region GetResourceAttributes

    [Fact]
    public void GetResourceAttributes_ExtractsOrgAndApp()
    {
        var request = CreateRequestWithResource(
            (XacmlRequestAttribute.OrgAttribute, "ttd"),
            (XacmlRequestAttribute.AppAttribute, "myapp"));

        var result = _sut.GetResourceAttributes(request);

        Assert.Equal("ttd", result.OrgValue);
        Assert.Equal("myapp", result.AppValue);
    }

    [Fact]
    public void GetResourceAttributes_ExtractsInstanceValues()
    {
        var request = CreateRequestWithResource(
            (XacmlRequestAttribute.InstanceAttribute, "50001/abc-def"));

        var result = _sut.GetResourceAttributes(request);

        Assert.Equal("50001/abc-def", result.InstanceValue);
        Assert.Equal("abc-def", result.AppInstanceIdValue);
    }

    #endregion

    #region EnrichRequestSubjectAttributes

    [Fact]
    public async Task Enrich_UserIdWithInstanceAccess_AddsPersonUuid()
    {
        var attrs = CreateSubjectAttributes((XacmlRequestAttribute.UserAttribute, "100"));
        var userUuid = Guid.NewGuid();

        _profileMock.Setup(p => p.GetUserProfile(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile { Party = new Party { PartyTypeName = PartyType.Person, PartyUuid = userUuid } });
        _partiesMock.Setup(p => p.GetKeyRoleParties(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<int>());

        await _sut.EnrichRequestSubjectAttributes(attrs, true, CancellationToken.None);

        Assert.Contains(attrs.Attributes, a =>
            a.AttributeId.OriginalString == XacmlRequestAttribute.PersonUuidAttribute &&
            a.AttributeValues.First().Value == userUuid.ToString());
    }

    [Fact]
    public async Task Enrich_UserIdNotInstanceAccess_SkipsUuidEnrichment()
    {
        var attrs = CreateSubjectAttributes((XacmlRequestAttribute.UserAttribute, "100"));

        _profileMock.Setup(p => p.GetUserProfile(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile { Party = new Party { PartyTypeName = PartyType.Person, PartyUuid = Guid.NewGuid() } });
        _partiesMock.Setup(p => p.GetKeyRoleParties(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<int>());

        await _sut.EnrichRequestSubjectAttributes(attrs, false, CancellationToken.None);

        Assert.DoesNotContain(attrs.Attributes, a =>
            a.AttributeId.OriginalString == XacmlRequestAttribute.PersonUuidAttribute);
    }

    [Fact]
    public async Task Enrich_UserWithKeyRoleParties_AddsPartyAttribute()
    {
        var attrs = CreateSubjectAttributes((XacmlRequestAttribute.UserAttribute, "100"));

        _profileMock.Setup(p => p.GetUserProfile(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile { Party = new Party { PartyTypeName = PartyType.Person } });
        _partiesMock.Setup(p => p.GetKeyRoleParties(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<int> { 200, 300 });
        _registerServiceMock.Setup(r => r.GetPartiesAsync(It.IsAny<List<int>>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Party>
            {
                new() { PartyUuid = Guid.NewGuid() },
                new() { PartyUuid = Guid.NewGuid() },
            });

        await _sut.EnrichRequestSubjectAttributes(attrs, false, CancellationToken.None);

        Assert.Contains(attrs.Attributes, a =>
            a.AttributeId.OriginalString == XacmlRequestAttribute.PartyAttribute);
    }

    [Fact]
    public async Task Enrich_PartyIdPersonInstanceAccess_AddsPersonUuid()
    {
        var partyUuid = Guid.NewGuid();
        var attrs = CreateSubjectAttributes((XacmlRequestAttribute.PartyAttribute, "500"));

        _registerServiceMock.Setup(r => r.GetParty(500, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Party { PartyTypeName = PartyType.Person, PartyUuid = partyUuid });

        await _sut.EnrichRequestSubjectAttributes(attrs, true, CancellationToken.None);

        Assert.Contains(attrs.Attributes, a =>
            a.AttributeId.OriginalString == XacmlRequestAttribute.PersonUuidAttribute &&
            a.AttributeValues.First().Value == partyUuid.ToString());
    }

    [Fact]
    public async Task Enrich_PartyIdOrganisationInstanceAccess_AddsOrgUuid()
    {
        var orgUuid = Guid.NewGuid();
        var attrs = CreateSubjectAttributes((XacmlRequestAttribute.PartyAttribute, "600"));

        _registerServiceMock.Setup(r => r.GetParty(600, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Party { PartyTypeName = PartyType.Organisation, PartyUuid = orgUuid });

        await _sut.EnrichRequestSubjectAttributes(attrs, true, CancellationToken.None);

        Assert.Contains(attrs.Attributes, a =>
            a.AttributeId.OriginalString == XacmlRequestAttribute.OrganizationUuidAttribute &&
            a.AttributeValues.First().Value == orgUuid.ToString());
    }

    [Fact]
    public async Task Enrich_PartyIdNotInstanceAccess_SkipsUuidEnrichment()
    {
        var attrs = CreateSubjectAttributes((XacmlRequestAttribute.PartyAttribute, "600"));

        await _sut.EnrichRequestSubjectAttributes(attrs, false, CancellationToken.None);

        _registerServiceMock.Verify(r => r.GetParty(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Enrich_NoSubjectUserIdOrPartyId_DoesNothing()
    {
        var attrs = CreateSubjectAttributes();

        await _sut.EnrichRequestSubjectAttributes(attrs, true, CancellationToken.None);

        // Only the initial (empty) attributes collection
        Assert.Empty(attrs.Attributes);
    }

    #endregion

    #region Helpers

    private static XacmlContextAttributes CreateSubjectAttributes(params (string id, string value)[] attributes)
    {
        var subject = new XacmlContextAttributes(new Uri(XacmlConstants.MatchAttributeCategory.Subject));
        foreach (var (id, value) in attributes)
        {
            var attr = new XacmlAttribute(new Uri(id), false);
            attr.AttributeValues.Add(new XacmlAttributeValue(StringDataType, value));
            subject.Attributes.Add(attr);
        }

        return subject;
    }

    private static XacmlContextRequest CreateRequestWithSubject(params (string id, string value)[] attributes)
    {
        var subject = CreateSubjectAttributes(attributes);
        return new XacmlContextRequest(false, false, [subject]);
    }

    private static XacmlContextRequest CreateRequestWithAction(string actionValue)
    {
        var actionAttrs = new XacmlContextAttributes(new Uri(XacmlConstants.MatchAttributeCategory.Action));
        var attr = new XacmlAttribute(new Uri(XacmlConstants.MatchAttributeIdentifiers.ActionId), false);
        attr.AttributeValues.Add(new XacmlAttributeValue(StringDataType, actionValue));
        actionAttrs.Attributes.Add(attr);
        return new XacmlContextRequest(false, false, [actionAttrs]);
    }

    private static XacmlContextRequest CreateRequestWithResource(params (string id, string value)[] attributes)
    {
        var resource = new XacmlContextAttributes(new Uri(XacmlConstants.MatchAttributeCategory.Resource));
        foreach (var (id, value) in attributes)
        {
            var attr = new XacmlAttribute(new Uri(id), false);
            attr.AttributeValues.Add(new XacmlAttributeValue(StringDataType, value));
            resource.Attributes.Add(attr);
        }

        return new XacmlContextRequest(false, false, [resource]);
    }

    #endregion
}
