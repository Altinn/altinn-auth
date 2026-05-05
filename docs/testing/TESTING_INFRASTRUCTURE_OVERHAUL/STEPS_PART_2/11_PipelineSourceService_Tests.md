---
step: 11
title: PipelineSourceService unit tests
phase: B
status: complete
linkedIssues:
  task: 2990
bugClassesCovered:
  - "Pre-cancelled CancellationToken — loop must exit before emitting any messages, queue must be completed in finally"
  - "Natural enumerator termination (MoveNextAsync returns false) — break with completion log; queue completed; finally cancels CTS"
  - "Each emitted PipelineMessage gets a strictly-increasing Sequence via Interlocked.Increment"
  - "Each emitted PipelineMessage carries the constructor-provided CancellationTokenSource (not a derived one)"
  - "Outbound BlockingCollection already completed → first Add throws InvalidOperationException → inner catch breaks the loop without crashing Run"
  - "Source delegate throws OperationCanceledException → outer `catch (OperationCanceledException)` branch logs PipelineCancelled, finally still completes the queue"
  - "Source delegate throws arbitrary Exception → outer `catch (Exception)` branch logs StreamingFailed + records telemetry failure, finally still completes the queue"
  - "Descriptor.ServiceScope override — when set, that factory is invoked instead of serviceProvider.CreateScope()"
verifiedTests: 8
touchedFiles: 1
---

# Step 11 — `PipelineSourceService` unit tests

## Goal

Pin down `PipelineSourceService.Run` / `EnumerateSource` behavior
with pure unit tests. The class is real-logic per the
*"real logic vs pass-through wiring"* filter — async-iterator
state machine over a delegate-supplied `IAsyncEnumerable`, with
multiple exception-handling branches, cancellation guards, and a
critical finally-block contract.

`PipelineSourceService` was previously only exercised end-to-end
when a full pipeline ran in a host. The 8 named bug classes
above were unprotected.

## What changed

### Tests added

[`src/libs/Altinn.Authorization.Host/test/Altinn.Authorization.Host.Pipeline.Tests/PipelineSourceServiceTest.cs`](../../../src/libs/Altinn.Authorization.Host/test/Altinn.Authorization.Host.Pipeline.Tests/PipelineSourceServiceTest.cs)
— 8 tests:

- `PreCancelledToken_NoMessagesEmitted_QueueCompleted`
- `NaturalEnumeratorTermination_AllMessagesEmitted_QueueCompleted`
- `EmittedMessages_HaveStrictlyIncreasingSequence`
- `EmittedMessages_CarryProvidedCancellationTokenSource`
- `OutboundQueueAlreadyCompleted_BreaksOnFirstAdd_NoCrash`
- `SourceThrowsOperationCanceled_CaughtInOuterHandler_NoCrash`
- `SourceThrowsArbitraryException_CaughtInOuterHandler_NoCrash`
- `DescriptorWithCustomServiceScope_IsInvokedInsteadOfDefaultCreateScope`

Test harness: `StubDescriptor` hand-rolled implementation of
`IPipelineDescriptor` (Moq isn't auto-pulled into test projects in
this repo); `Yield` / `YieldThenThrow` async-iterator helpers
build deterministic `PipelineSource<T>` delegates.
`NullLogger<PipelineSourceService>.Instance` for the logger;
`new ServiceCollection().BuildServiceProvider()` for the
`IServiceProvider` (registers `IServiceScopeFactory` automatically,
so `serviceProvider.CreateScope()` works without further setup).

xUnit1051 suppressed at the file level — the analyzer's
recommendation to thread `TestContext.Current.CancellationToken`
through every `BlockingCollection.GetConsumingEnumerable()` call
isn't useful here (tests complete in <1s, the test's own
`CancellationTokenSource` is already wired through `Run`).

## Verification

```text
$ dotnet test src/libs/Altinn.Authorization.Host/test/Altinn.Authorization.Host.Pipeline.Tests/...
Passed! - Failed: 0, Passed: 9, Skipped: 0, Total: 9, Duration: 257ms
```

9 = 8 new + 1 pre-existing `PipelineMessageSmokeTest`. Total
duration well under 1 s — no hardcoded delays in
`PipelineSourceService` to slow tests down.

## Deferred / follow-up

- **`PipelineSegmentService` / `PipelineSinkService` retry
  semantics.** Both have hardcoded `await Task.Delay(seconds(attempt))`
  backoffs in their `DispatchSegment` retry loops. Retry-exhaust
  tests would take ~3 s each at current settings; the right next
  step is a small refactor that injects the backoff function so
  unit tests can use a no-op delay, then write the retry tests
  fast.
- **`PipelineSegmentService.EnumerateSegment`** has a hardcoded
  `await Task.Delay(TimeSpan.FromSeconds(2))` *per consumed
  message* on line 48 of the production file — likely a debugging
  artifact. A separate Task should investigate whether this is
  intentional throttling or dead code; either way it's blocking
  fast-feedback tests on the segment service.
- **`PipelineHostedService`** lifecycle tests.
- **Builder fluent surface** (`PipelineSourceBuilder`,
  `PipelineSegmentBuilder`, `PipelineSinkBuilder`,
  `PipelineGroup.AddPipeline`, `PipelineDescriptor.With...`) — the
  builders are largely pass-through state-mutation; testing them
  in isolation borders on response-shape mapping rather than real
  logic. Skip unless a real bug class emerges.
