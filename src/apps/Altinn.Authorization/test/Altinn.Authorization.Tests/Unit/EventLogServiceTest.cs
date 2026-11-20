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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.FeatureManagement;
using Moq;

namespace Altinn.Authorization.Tests.Unit;

[UnitTest]
public class EventLogServiceTest : IDisposable
{
    private const string AuditLogEventsInstrument = "altinn.pdp.auditlog.events";
    private const string AuditLogSuppressedInstrument = "altinn.pdp.auditlog.suppressed";

    private readonly Mock<IEventsQueueClient> _queueClientMock = new();
    private readonly Mock<IFeatureManager> _featureManagerMock = new();
    private readonly Mock<ILogger<EventLogService>> _loggerMock = new();
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private readonly ServiceProvider _metrics = new ServiceCollection().AddMetrics().BuildServiceProvider();

    public void Dispose() => _metrics.Dispose();

    private IMeterFactory MeterFactory => _metrics.GetRequiredService<IMeterFactory>();

    private EventLogService CreateService(TimeProvider? trackerTimeProvider = null, AuditLogSuppressScope suppressScope = AuditLogSuppressScope.Trace)
    {
        var telemetry = new DecisionTelemetry(MeterFactory);
        var settings = Options.Create(new AuditLogDeduplicationSettings { SuppressScope = suppressScope });
        return new(
            _queueClientMock.Object,
            _timeProvider,
            new AuthorizationEventDuplicateTracker(settings, trackerTimeProvider ?? _timeProvider, telemetry),
            telemetry,
            _loggerMock.Object,
            settings);
    }

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
        Assert.All(collector.Measurements, tags => Assert.Equal("auditlog.duplicate", Assert.Single(tags).Key));
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

    [Fact]
    public async Task CreateAuthorizationEvent_DuplicateMeasurementEnabled_TellsResourceInstancesApart()
    {
        _featureManagerMock.Setup(f => f.IsEnabledAsync(FeatureFlags.AuditLog)).ReturnsAsync(true);
        _featureManagerMock.Setup(f => f.IsEnabledAsync(FeatureFlags.AuditLogDuplicateMeasurement)).ReturnsAsync(true);
        _queueClientMock
            .Setup(q => q.EnqueueAuthorizationEvent(It.IsAny<Altinn.Platform.Authorization.Models.EventLog.AuthorizationEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueuePostReceipt { Success = true });

        var response = new XacmlContextResponse(new XacmlContextResult(XacmlContextDecision.Permit, new XacmlContextStatus(new XacmlContextStatusCode("urn:oasis:names:tc:xacml:1.0:status:ok"))));
        var httpContext = new DefaultHttpContext { TraceIdentifier = "trace-1" };

        using var collector = new PdpMetricCollector(MeterFactory, AuditLogEventsInstrument);
        var service = CreateService();

        // Two messages of the same resource, read while handling one request, and the first one again.
        await service.CreateAuthorizationEvent(_featureManagerMock.Object, CreateMinimalRequest("message-a"), httpContext, response, TestContext.Current.CancellationToken);
        await service.CreateAuthorizationEvent(_featureManagerMock.Object, CreateMinimalRequest("message-b"), httpContext, response, TestContext.Current.CancellationToken);
        await service.CreateAuthorizationEvent(_featureManagerMock.Object, CreateMinimalRequest("message-a"), httpContext, response, TestContext.Current.CancellationToken);

        Assert.Equal(new object?[] { "none", "none", "trace" }, collector.Measurements.Select(tags => tags["auditlog.duplicate"]).ToArray());
    }

    [Fact]
    public async Task CreateAuthorizationEvent_DuplicateMeasurementFails_StillQueuesEvent_CountsError_AndLogsOnce()
    {
        _featureManagerMock.Setup(f => f.IsEnabledAsync(FeatureFlags.AuditLog)).ReturnsAsync(true);
        _featureManagerMock.Setup(f => f.IsEnabledAsync(FeatureFlags.AuditLogDuplicateMeasurement)).ReturnsAsync(true);
        _queueClientMock
            .Setup(q => q.EnqueueAuthorizationEvent(It.IsAny<Altinn.Platform.Authorization.Models.EventLog.AuthorizationEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueuePostReceipt { Success = true });
        _loggerMock.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        // The tracker reads the clock for every event, so a failing clock makes every measurement fail.
        var failingClock = new Mock<TimeProvider>();
        failingClock.Setup(t => t.GetUtcNow()).Throws(new InvalidOperationException("clock failure"));

        var response = new XacmlContextResponse(new XacmlContextResult(XacmlContextDecision.Permit, new XacmlContextStatus(new XacmlContextStatusCode("urn:oasis:names:tc:xacml:1.0:status:ok"))));

        using var collector = new PdpMetricCollector(MeterFactory, AuditLogEventsInstrument);
        var service = CreateService(trackerTimeProvider: failingClock.Object);

        await service.CreateAuthorizationEvent(_featureManagerMock.Object, CreateMinimalRequest(), new DefaultHttpContext(), response, TestContext.Current.CancellationToken);
        await service.CreateAuthorizationEvent(_featureManagerMock.Object, CreateMinimalRequest(), new DefaultHttpContext(), response, TestContext.Current.CancellationToken);

        // The decisions' events are queued regardless, and every failure is counted, but logged once.
        _queueClientMock.Verify(
            q => q.EnqueueAuthorizationEvent(It.IsAny<Altinn.Platform.Authorization.Models.EventLog.AuthorizationEvent>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        Assert.Equal(new object?[] { "error", "error" }, collector.Measurements.Select(tags => tags["auditlog.duplicate"]).ToArray());
        _loggerMock.Verify(
            l => l.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<InvalidOperationException>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAuthorizationEvent_MetricRecordingFails_StillQueuesEvent_AndLogsOnce()
    {
        _featureManagerMock.Setup(f => f.IsEnabledAsync(FeatureFlags.AuditLog)).ReturnsAsync(true);
        _featureManagerMock.Setup(f => f.IsEnabledAsync(FeatureFlags.AuditLogDuplicateMeasurement)).ReturnsAsync(true);
        _queueClientMock
            .Setup(q => q.EnqueueAuthorizationEvent(It.IsAny<Altinn.Platform.Authorization.Models.EventLog.AuthorizationEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueuePostReceipt { Success = true });
        _loggerMock.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        // A listener that throws on every measurement, so that both the classification and the error
        // recorded after it fail. Scoped to this test's meter, so other tests are not affected.
        Meter meter = MeterFactory.Create(DecisionTelemetry.MeterName);
        using var throwingListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (ReferenceEquals(instrument.Meter, meter) && instrument.Name == AuditLogEventsInstrument)
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };
        throwingListener.SetMeasurementEventCallback<long>((_, _, _, _) => throw new InvalidOperationException("listener failure"));
        throwingListener.Start();

        var response = new XacmlContextResponse(new XacmlContextResult(XacmlContextDecision.Permit, new XacmlContextStatus(new XacmlContextStatusCode("urn:oasis:names:tc:xacml:1.0:status:ok"))));
        var service = CreateService();

        await service.CreateAuthorizationEvent(_featureManagerMock.Object, CreateMinimalRequest(), new DefaultHttpContext(), response, TestContext.Current.CancellationToken);
        await service.CreateAuthorizationEvent(_featureManagerMock.Object, CreateMinimalRequest(), new DefaultHttpContext(), response, TestContext.Current.CancellationToken);

        _queueClientMock.Verify(
            q => q.EnqueueAuthorizationEvent(It.IsAny<Altinn.Platform.Authorization.Models.EventLog.AuthorizationEvent>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        _loggerMock.Verify(
            l => l.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<InvalidOperationException>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAuthorizationEvent_LoggingTheMeasurementFailureFails_StillQueuesEvent()
    {
        _featureManagerMock.Setup(f => f.IsEnabledAsync(FeatureFlags.AuditLog)).ReturnsAsync(true);
        _featureManagerMock.Setup(f => f.IsEnabledAsync(FeatureFlags.AuditLogDuplicateMeasurement)).ReturnsAsync(true);
        _queueClientMock
            .Setup(q => q.EnqueueAuthorizationEvent(It.IsAny<Altinn.Platform.Authorization.Models.EventLog.AuthorizationEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueuePostReceipt { Success = true });
        _loggerMock.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        _loggerMock
            .Setup(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Throws(new AggregateException("logger failure"));

        var failingClock = new Mock<TimeProvider>();
        failingClock.Setup(t => t.GetUtcNow()).Throws(new InvalidOperationException("clock failure"));

        var response = new XacmlContextResponse(new XacmlContextResult(XacmlContextDecision.Permit, new XacmlContextStatus(new XacmlContextStatusCode("urn:oasis:names:tc:xacml:1.0:status:ok"))));
        var service = CreateService(trackerTimeProvider: failingClock.Object);

        await service.CreateAuthorizationEvent(_featureManagerMock.Object, CreateMinimalRequest(), new DefaultHttpContext(), response, TestContext.Current.CancellationToken);

        _queueClientMock.Verify(
            q => q.EnqueueAuthorizationEvent(It.IsAny<Altinn.Platform.Authorization.Models.EventLog.AuthorizationEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAuthorizationEvent_SuppressionTraceScope_DropsRepeatsInTheSameRequestOnly()
    {
        EnableSuppression();
        QueueReturns(true, true);
        using var suppressed = new PdpMetricCollector(MeterFactory, AuditLogSuppressedInstrument);
        var service = CreateService();

        await LogDecision(service, "trace-1");
        await LogDecision(service, "trace-1");
        await LogDecision(service, "trace-2");

        VerifyQueued(Times.Exactly(2));
        Assert.Equal(new object?[] { "trace" }, suppressed.Measurements.Select(tags => tags["auditlog.duplicate"]).ToArray());
    }

    [Fact]
    public async Task CreateAuthorizationEvent_SuppressionWindowScope_DropsRepeatsFromOtherRequests_AndStillMeasures()
    {
        EnableSuppression();
        QueueReturns(true);
        using var events = new PdpMetricCollector(MeterFactory, AuditLogEventsInstrument);
        using var suppressed = new PdpMetricCollector(MeterFactory, AuditLogSuppressedInstrument);
        var service = CreateService(suppressScope: AuditLogSuppressScope.Window);

        await LogDecision(service, "trace-1");
        await LogDecision(service, "trace-2");
        await LogDecision(service, "trace-2");

        VerifyQueued(Times.Once());
        Assert.Equal(new object?[] { "none", "window", "trace" }, events.Measurements.Select(tags => tags["auditlog.duplicate"]).ToArray());
        Assert.Equal(new object?[] { "window", "trace" }, suppressed.Measurements.Select(tags => tags["auditlog.duplicate"]).ToArray());
    }

    [Fact]
    public async Task CreateAuthorizationEvent_Suppression_FailedSendIsNotRemembered()
    {
        EnableSuppression();
        QueueReturns(false, true);
        var service = CreateService();

        await LogDecision(service, "trace-1");
        await LogDecision(service, "trace-1");

        VerifyQueued(Times.Exactly(2));
    }

    [Fact]
    public async Task CreateAuthorizationEvent_Suppression_SendThatThrowsIsNotRemembered()
    {
        EnableSuppression();
        _queueClientMock
            .SetupSequence(q => q.EnqueueAuthorizationEvent(It.IsAny<Altinn.Platform.Authorization.Models.EventLog.AuthorizationEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException())
            .ReturnsAsync(new QueuePostReceipt { Success = true });
        var service = CreateService();

        await LogDecision(service, "trace-1");
        await LogDecision(service, "trace-1");

        VerifyQueued(Times.Exactly(2));
    }

    [Fact]
    public async Task CreateAuthorizationEvent_Suppression_ClassificationFails_QueuesEveryEvent()
    {
        EnableSuppression();
        QueueReturns(true, true);
        var failingClock = new Mock<TimeProvider>();
        failingClock.Setup(t => t.GetUtcNow()).Throws(new InvalidOperationException("clock failure"));
        using var events = new PdpMetricCollector(MeterFactory, AuditLogEventsInstrument);
        var service = CreateService(trackerTimeProvider: failingClock.Object);

        await LogDecision(service, "trace-1");
        await LogDecision(service, "trace-1");

        // Nothing is known to be a duplicate, so nothing is dropped.
        VerifyQueued(Times.Exactly(2));
        Assert.Equal(new object?[] { "error", "error" }, events.Measurements.Select(tags => tags["auditlog.duplicate"]).ToArray());
    }

    [Fact]
    public async Task CreateAuthorizationEvent_SuppressionWindowScope_StopsDroppingWhenTheQueuedEventLeavesTheWindow()
    {
        // From review: a dropped repeat's trace must not get a window of its own.
        EnableSuppression();
        QueueReturns(true, true);
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 11, 13, 12, 0, 0, TimeSpan.Zero));
        var service = CreateService(trackerTimeProvider: clock, suppressScope: AuditLogSuppressScope.Window);

        await LogDecision(service, "trace-1");
        clock.Advance(TimeSpan.FromSeconds(50));
        await LogDecision(service, "trace-2");

        // 65 seconds after the only queued copy, so this one must be queued too.
        clock.Advance(TimeSpan.FromSeconds(15));
        await LogDecision(service, "trace-2");

        VerifyQueued(Times.Exactly(2));
    }

    [Fact]
    public async Task CreateAuthorizationEvent_SuppressionSwitchedOnWhileRunning_IgnoresEventsTheMeasurementSaw()
    {
        // From review: the measurement remembers an event whose send failed. Dropping, switched on
        // afterwards, must not treat that event as logged.
        _featureManagerMock.Setup(f => f.IsEnabledAsync(FeatureFlags.AuditLog)).ReturnsAsync(true);
        _featureManagerMock.Setup(f => f.IsEnabledAsync(FeatureFlags.AuditLogDuplicateMeasurement)).ReturnsAsync(true);
        bool suppression = false;
        _featureManagerMock.Setup(f => f.IsEnabledAsync(FeatureFlags.AuditLogDuplicateSuppression)).ReturnsAsync(() => suppression);
        QueueReturns(false, true);
        var service = CreateService(suppressScope: AuditLogSuppressScope.Window);

        await LogDecision(service, "trace-1");
        suppression = true;
        await LogDecision(service, "trace-2");

        VerifyQueued(Times.Exactly(2));
    }

    [Fact]
    public async Task CreateAuthorizationEvent_SuppressionOn_MeasurementStillSeesRepeatsOfAFailedSend()
    {
        // From review: dropping must not change what the measurement reports.
        EnableSuppression();
        QueueReturns(false, true);
        using var events = new PdpMetricCollector(MeterFactory, AuditLogEventsInstrument);
        var service = CreateService(suppressScope: AuditLogSuppressScope.Window);

        await LogDecision(service, "trace-1");
        await LogDecision(service, "trace-2");

        // Not dropped, since the first send failed, but still measured as a repeat.
        VerifyQueued(Times.Exactly(2));
        Assert.Equal(new object?[] { "none", "window" }, events.Measurements.Select(tags => tags["auditlog.duplicate"]).ToArray());
    }

    [Fact]
    public async Task CreateAuthorizationEvent_SuppressionOnMeasurementOff_FailureRecordsNoMeasurement_AndLogsOnce()
    {
        // From review: with the measurement off, a failure in dropping must not appear in its metric.
        EnableSuppression();
        _featureManagerMock.Setup(f => f.IsEnabledAsync(FeatureFlags.AuditLogDuplicateMeasurement)).ReturnsAsync(false);
        QueueReturns(true, true);
        _loggerMock.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        var failingClock = new Mock<TimeProvider>();
        failingClock.Setup(t => t.GetUtcNow()).Throws(new InvalidOperationException("clock failure"));
        using var events = new PdpMetricCollector(MeterFactory, AuditLogEventsInstrument);
        var service = CreateService(trackerTimeProvider: failingClock.Object);

        await LogDecision(service, "trace-1");
        await LogDecision(service, "trace-1");

        VerifyQueued(Times.Exactly(2));
        Assert.Empty(events.Measurements);
        _loggerMock.Verify(
            l => l.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<InvalidOperationException>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAuthorizationEvent_Suppression_RememberFails_CountsTheEventOnce()
    {
        // From review: a failure after the event was measured must not count it a second time.
        EnableSuppression();
        bool sent = false;
        _queueClientMock
            .Setup(q => q.EnqueueAuthorizationEvent(It.IsAny<Altinn.Platform.Authorization.Models.EventLog.AuthorizationEvent>(), It.IsAny<CancellationToken>()))
            .Callback(() => sent = true)
            .ReturnsAsync(new QueuePostReceipt { Success = true });

        // The clock fails once the event is sent, so remembering it fails, whatever came before.
        var start = new DateTimeOffset(2026, 11, 13, 12, 0, 0, TimeSpan.Zero);
        var clock = new Mock<TimeProvider>();
        clock.Setup(t => t.GetUtcNow()).Returns(() => sent ? throw new InvalidOperationException("clock failure") : start);
        using var events = new PdpMetricCollector(MeterFactory, AuditLogEventsInstrument);
        var service = CreateService(trackerTimeProvider: clock.Object);

        await LogDecision(service, "trace-1");

        VerifyQueued(Times.Once());
        Assert.Equal(new object?[] { "none" }, events.Measurements.Select(tags => tags["auditlog.duplicate"]).ToArray());
    }

    private void EnableSuppression()
    {
        _featureManagerMock.Setup(f => f.IsEnabledAsync(FeatureFlags.AuditLog)).ReturnsAsync(true);
        _featureManagerMock.Setup(f => f.IsEnabledAsync(FeatureFlags.AuditLogDuplicateMeasurement)).ReturnsAsync(true);
        _featureManagerMock.Setup(f => f.IsEnabledAsync(FeatureFlags.AuditLogDuplicateSuppression)).ReturnsAsync(true);
    }

    private void QueueReturns(params bool[] successes)
    {
        var sequence = _queueClientMock.SetupSequence(q => q.EnqueueAuthorizationEvent(It.IsAny<Altinn.Platform.Authorization.Models.EventLog.AuthorizationEvent>(), It.IsAny<CancellationToken>()));
        foreach (bool success in successes)
        {
            sequence = sequence.ReturnsAsync(new QueuePostReceipt { Success = success });
        }
    }

    private void VerifyQueued(Times times) => _queueClientMock.Verify(
        q => q.EnqueueAuthorizationEvent(It.IsAny<Altinn.Platform.Authorization.Models.EventLog.AuthorizationEvent>(), It.IsAny<CancellationToken>()),
        times);

    // The mocked queue completes synchronously, so the event is remembered before this returns.
    private Task LogDecision(EventLogService service, string traceId) => service.CreateAuthorizationEvent(
        _featureManagerMock.Object,
        CreateMinimalRequest(),
        new DefaultHttpContext { TraceIdentifier = traceId },
        new XacmlContextResponse(new XacmlContextResult(XacmlContextDecision.Permit, new XacmlContextStatus(new XacmlContextStatusCode("urn:oasis:names:tc:xacml:1.0:status:ok")))),
        TestContext.Current.CancellationToken);

    private static XacmlContextRequest CreateMinimalRequest(string? resourceInstanceId = null)
    {
        var resourceAttrs = new XacmlContextAttributes(new Uri(XacmlConstants.MatchAttributeCategory.Resource));
        var resourceAttr = new XacmlAttribute(new Uri("urn:altinn:resource"), false);
        resourceAttr.AttributeValues.Add(new XacmlAttributeValue(new Uri(XacmlConstants.DataTypes.XMLString), "test-resource"));
        resourceAttrs.Attributes.Add(resourceAttr);

        if (resourceInstanceId != null)
        {
            var instanceAttr = new XacmlAttribute(new Uri("urn:altinn:resource:instance-id"), false);
            instanceAttr.AttributeValues.Add(new XacmlAttributeValue(new Uri(XacmlConstants.DataTypes.XMLString), resourceInstanceId));
            resourceAttrs.Attributes.Add(instanceAttr);
        }

        return new XacmlContextRequest(false, false, [resourceAttrs]);
    }
}
