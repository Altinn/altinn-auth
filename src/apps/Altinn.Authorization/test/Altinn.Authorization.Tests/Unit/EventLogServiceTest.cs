using System.Diagnostics.Metrics;
using Altinn.Authorization.ABAC.Constants;
using Altinn.Authorization.ABAC.Xacml;
using Altinn.Authorization.Tests.Util;
using Altinn.Platform.Authorization.Clients.Interfaces;
using Altinn.Platform.Authorization.Configuration;
using Altinn.Platform.Authorization.Models;
using Altinn.Platform.Authorization.Services.Implementation;
using Altinn.Platform.Authorization.Telemetry;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using Moq;

namespace Altinn.Authorization.Tests.Unit;

[UnitTest]
public class EventLogServiceTest : IDisposable
{
    private const string AuditLogEventsInstrument = "altinn.pdp.auditlog.events";

    private readonly Mock<IEventsQueueClient> _queueClientMock = new();
    private readonly Mock<IFeatureManager> _featureManagerMock = new();
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private readonly ServiceProvider _metrics = new ServiceCollection().AddMetrics().BuildServiceProvider();

    public void Dispose() => _metrics.Dispose();

    private IMeterFactory MeterFactory => _metrics.GetRequiredService<IMeterFactory>();

    private EventLogService CreateService() => new(
        _queueClientMock.Object,
        _timeProvider,
        new AuthorizationEventDuplicateTracker(Options.Create(new AuditLogDeduplicationSettings()), _timeProvider),
        new DecisionTelemetry(MeterFactory));

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
        await service.CreateAuthorizationEvent(_featureManagerMock.Object, request, httpContext, response, TestContext.Current.CancellationToken);

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
        await service.CreateAuthorizationEvent(_featureManagerMock.Object, request, httpContext, response, TestContext.Current.CancellationToken);

        _queueClientMock.Verify(
            q => q.EnqueueAuthorizationEvent(It.IsAny<Altinn.Platform.Authorization.Models.EventLog.AuthorizationEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAuthorizationEvent_DuplicateMeasurementEnabled_QueuesEveryEvent_AndCountsDuplicates()
    {
        _featureManagerMock.Setup(f => f.IsEnabledAsync(FeatureFlags.AuditLog)).ReturnsAsync(true);
        _featureManagerMock.Setup(f => f.IsEnabledAsync(FeatureFlags.AuditLogDuplicateMeasurement)).ReturnsAsync(true);
        _queueClientMock
            .Setup(q => q.EnqueueAuthorizationEvent(It.IsAny<Altinn.Platform.Authorization.Models.EventLog.AuthorizationEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueuePostReceipt { Success = true });

        var request = CreateMinimalRequest();
        var response = new XacmlContextResponse(new XacmlContextResult(XacmlContextDecision.Permit, new XacmlContextStatus(new XacmlContextStatusCode("urn:oasis:names:tc:xacml:1.0:status:ok"))));
        var firstRequest = new DefaultHttpContext { TraceIdentifier = "trace-1" };
        var secondRequest = new DefaultHttpContext { TraceIdentifier = "trace-2" };

        using var collector = new PdpMetricCollector(MeterFactory, AuditLogEventsInstrument);
        var service = CreateService();

        // The same decision twice while handling one request, then once more in the next.
        await service.CreateAuthorizationEvent(_featureManagerMock.Object, request, firstRequest, response, TestContext.Current.CancellationToken);
        await service.CreateAuthorizationEvent(_featureManagerMock.Object, request, firstRequest, response, TestContext.Current.CancellationToken);
        await service.CreateAuthorizationEvent(_featureManagerMock.Object, request, secondRequest, response, TestContext.Current.CancellationToken);

        _queueClientMock.Verify(
            q => q.EnqueueAuthorizationEvent(It.IsAny<Altinn.Platform.Authorization.Models.EventLog.AuthorizationEvent>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));

        Assert.Equal(new object?[] { "none", "trace", "window" }, collector.Measurements.Select(tags => tags["auditlog.duplicate"]).ToArray());
        Assert.All(collector.Measurements, tags => Assert.Equal("test-resource", tags["resource.id"]));
    }

    [Fact]
    public async Task CreateAuthorizationEvent_DuplicateMeasurementDisabled_RecordsNoMeasurement()
    {
        _featureManagerMock.Setup(f => f.IsEnabledAsync(FeatureFlags.AuditLog)).ReturnsAsync(true);
        _featureManagerMock.Setup(f => f.IsEnabledAsync(FeatureFlags.AuditLogDuplicateMeasurement)).ReturnsAsync(false);
        _queueClientMock
            .Setup(q => q.EnqueueAuthorizationEvent(It.IsAny<Altinn.Platform.Authorization.Models.EventLog.AuthorizationEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueuePostReceipt { Success = true });

        var request = CreateMinimalRequest();
        var response = new XacmlContextResponse(new XacmlContextResult(XacmlContextDecision.Permit, new XacmlContextStatus(new XacmlContextStatusCode("urn:oasis:names:tc:xacml:1.0:status:ok"))));

        using var collector = new PdpMetricCollector(MeterFactory, AuditLogEventsInstrument);
        var service = CreateService();

        await service.CreateAuthorizationEvent(_featureManagerMock.Object, request, new DefaultHttpContext(), response, TestContext.Current.CancellationToken);

        _queueClientMock.Verify(
            q => q.EnqueueAuthorizationEvent(It.IsAny<Altinn.Platform.Authorization.Models.EventLog.AuthorizationEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
        Assert.Empty(collector.Measurements);
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
