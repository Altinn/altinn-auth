using Altinn.Authorization.ABAC.Constants;
using Altinn.Authorization.ABAC.Xacml;
using Altinn.Platform.Authorization.Clients.Interfaces;
using Altinn.Platform.Authorization.Configuration;
using Altinn.Platform.Authorization.Models;
using Altinn.Platform.Authorization.Services.Implementation;
using Microsoft.AspNetCore.Http;
using Microsoft.FeatureManagement;
using Moq;

namespace Altinn.Platform.Authorization.Tests;

public class EventLogServiceTest
{
    private readonly Mock<IEventsQueueClient> _queueClientMock = new();
    private readonly Mock<IFeatureManager> _featureManagerMock = new();
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    private EventLogService CreateService() => new(_queueClientMock.Object, _timeProvider);

    [Fact]
    public async Task CreateAuthorizationEvent_AuditLogEnabled_EnqueuesEvent()
    {
        _featureManagerMock.Setup(f => f.IsEnabledAsync(FeatureFlags.AuditLog)).ReturnsAsync(true);
        _queueClientMock
            .Setup(q => q.EnqueueAuthorizationEvent(It.IsAny<Altinn.Platform.Authorization.Models.EventLog.AuthorizationEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueuePostReceipt { Success = true });

        var request = CreateMinimalRequest();
        var response = new XacmlContextResponse(new XacmlContextResult(XacmlContextDecision.Permit, new XacmlContextStatus(new XacmlContextStatusCode("urn:oasis:names:tc:xacml:1.0:status:ok"))));
        var httpContext = new DefaultHttpContext();

        var service = CreateService();
        await service.CreateAuthorizationEvent(_featureManagerMock.Object, request, httpContext, response);

        _queueClientMock.Verify(
            q => q.EnqueueAuthorizationEvent(It.IsAny<Altinn.Platform.Authorization.Models.EventLog.AuthorizationEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAuthorizationEvent_AuditLogDisabled_DoesNotEnqueue()
    {
        _featureManagerMock.Setup(f => f.IsEnabledAsync(FeatureFlags.AuditLog)).ReturnsAsync(false);

        var request = CreateMinimalRequest();
        var response = new XacmlContextResponse(new XacmlContextResult(XacmlContextDecision.Permit, new XacmlContextStatus(new XacmlContextStatusCode("urn:oasis:names:tc:xacml:1.0:status:ok"))));
        var httpContext = new DefaultHttpContext();

        var service = CreateService();
        await service.CreateAuthorizationEvent(_featureManagerMock.Object, request, httpContext, response);

        _queueClientMock.Verify(
            q => q.EnqueueAuthorizationEvent(It.IsAny<Altinn.Platform.Authorization.Models.EventLog.AuthorizationEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static XacmlContextRequest CreateMinimalRequest()
    {
        var resourceAttrs = new XacmlContextAttributes(new Uri(XacmlConstants.MatchAttributeCategory.Resource));
        var resourceAttr = new XacmlAttribute(new Uri("urn:altinn:resource"), false);
        resourceAttr.AttributeValues.Add(new XacmlAttributeValue(new Uri(XacmlConstants.DataTypes.XMLString), "test-resource"));
        resourceAttrs.Attributes.Add(resourceAttr);

        return new XacmlContextRequest(false, false, [resourceAttrs]);
    }
}
