using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Altinn.Authorization.Enums;
using Altinn.Authorization.Models;
using Altinn.Authorization.Models.Register;
using Altinn.Authorization.Models.ResourceRegistry;
using Altinn.Platform.Authorization.Models;
using Altinn.Platform.Authorization.Services.Implementation;
using Altinn.Platform.Authorization.Services.Interface;
using Moq;
using Xunit;

namespace Altinn.Platform.Authorization.UnitTests;

public class AccessListAuthorizationTest
{
    private static readonly Guid PartyUuid = Guid.NewGuid();
    private static readonly string ResourceId = "ttd-accesslist-resource";
    private static readonly string ActionId = "read";

    private readonly Mock<IResourceRegistry> _registryMock = new();
    private readonly AccessListAuthorization _sut;

    public AccessListAuthorizationTest()
    {
        _sut = new AccessListAuthorization(_registryMock.Object);
    }

    [Fact]
    public async Task Authorize_NullMemberships_ReturnsNotAuthorized()
    {
        // Arrange
        var request = CreateRequest();
        _registryMock
            .Setup(r => r.GetMembershipsForResourceForParty(It.IsAny<PartyUrn>(), It.IsAny<ResourceIdUrn>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<AccessListResourceMembershipWithActionFilterDto>)null);

        // Act
        var result = await _sut.Authorize(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(AccessListAuthorizationResult.NotAuthorized, result.Value.Result);
    }

    [Fact]
    public async Task Authorize_EmptyMemberships_ReturnsNotAuthorized()
    {
        // Arrange
        var request = CreateRequest();
        _registryMock
            .Setup(r => r.GetMembershipsForResourceForParty(It.IsAny<PartyUrn>(), It.IsAny<ResourceIdUrn>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AccessListResourceMembershipWithActionFilterDto>());

        // Act
        var result = await _sut.Authorize(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(AccessListAuthorizationResult.NotAuthorized, result.Value.Result);
    }

    [Fact]
    public async Task Authorize_MembershipWithNullActionFilters_ReturnsAuthorized()
    {
        // Arrange
        var request = CreateRequest();
        var membership = CreateMembership(actionFilters: null);
        _registryMock
            .Setup(r => r.GetMembershipsForResourceForParty(It.IsAny<PartyUrn>(), It.IsAny<ResourceIdUrn>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { membership });

        // Act
        var result = await _sut.Authorize(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(AccessListAuthorizationResult.Authorized, result.Value.Result);
    }

    [Fact]
    public async Task Authorize_MembershipWithMatchingActionFilter_ReturnsAuthorized()
    {
        // Arrange
        var request = CreateRequest();
        var membership = CreateMembership(actionFilters: new[] { ActionId });
        _registryMock
            .Setup(r => r.GetMembershipsForResourceForParty(It.IsAny<PartyUrn>(), It.IsAny<ResourceIdUrn>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { membership });

        // Act
        var result = await _sut.Authorize(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(AccessListAuthorizationResult.Authorized, result.Value.Result);
    }

    [Fact]
    public async Task Authorize_MembershipWithNonMatchingActionFilter_ReturnsNotAuthorized()
    {
        // Arrange
        var request = CreateRequest();
        var membership = CreateMembership(actionFilters: new[] { "write" });
        _registryMock
            .Setup(r => r.GetMembershipsForResourceForParty(It.IsAny<PartyUrn>(), It.IsAny<ResourceIdUrn>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { membership });

        // Act
        var result = await _sut.Authorize(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(AccessListAuthorizationResult.NotAuthorized, result.Value.Result);
    }

    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static AccessListAuthorizationRequest CreateRequest()
    {
        string json = $$"""
            {
              "subject": { "type": "urn:altinn:party:uuid", "value": "{{PartyUuid}}" },
              "resource": { "type": "urn:altinn:resource", "value": "{{ResourceId}}" },
              "action": { "type": "urn:oasis:names:tc:xacml:1.0:action:action-id", "value": "{{ActionId}}" }
            }
            """;
        return System.Text.Json.JsonSerializer.Deserialize<AccessListAuthorizationRequest>(json, JsonOptions);
    }

    private static AccessListResourceMembershipWithActionFilterDto CreateMembership(IReadOnlyCollection<string> actionFilters)
    {
        PartyUrn.TryParse($"urn:altinn:party:uuid:{PartyUuid}", out var partyUrn);
        var party = (PartyUrn.PartyUuid)partyUrn;
        ResourceUrn.TryParse($"urn:altinn:resource:{ResourceId}", out var resourceUrn);
        var resource = (ResourceUrn.ResourceId)resourceUrn;
        return new AccessListResourceMembershipWithActionFilterDto(party, resource, DateTimeOffset.UtcNow, actionFilters);
    }
}
