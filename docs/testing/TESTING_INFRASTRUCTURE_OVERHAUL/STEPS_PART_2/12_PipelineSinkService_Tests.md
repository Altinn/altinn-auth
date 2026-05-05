---
step: 12
title: PipelineSinkService unit tests
phase: B
status: complete
linkedIssues:
  task: 2990
bugClassesCovered:
  - "Empty inbound queue → sink delegate never invoked, no crash"
  - "Success on first attempt → no retry, no backoff, single delegate call"
  - "Multiple messages processed in arrival order, all consumed"
  - "Success on retry attempt 2 → single retry, then success, message's CTS not cancelled"
  - "Success on retry attempt 3 → two retries, then success, message's CTS not cancelled"
  - "All 3 attempts fail (retry exhausted) → DispatchSegment maps to InvalidOperationException, outer catch logs + cancels message's CTS, Run does NOT propagate"
  - "Retry exhaustion stops EnumerateSink (returns), so subsequent queued messages are NOT processed"
  - "Descriptor.ServiceScope override — when set, that factory is invoked instead of serviceProvider.CreateScope()"
verifiedTests: 8
touchedFiles: 1
---

# Step 12 — `PipelineSinkService` unit tests

## Goal

Pin down `PipelineSinkService.Run` / `EnumerateSink` /
`DispatchSegment` retry semantics with pure unit tests. The class
is the terminal stage of the pipeline; its retry loop has the same
shape as `PipelineSegmentService.DispatchSegment` (3 attempts,
exponential backoff, `InvalidOperationException` after exhaustion)
but without the per-message `Task.Delay(2)` quirk that makes
`PipelineSegmentService` end-to-end testing painful.

## What changed

### Tests added

[`src/libs/Altinn.Authorization.Host/test/Altinn.Authorization.Host.Pipeline.Tests/PipelineSinkServiceTest.cs`](../../../src/libs/Altinn.Authorization.Host/test/Altinn.Authorization.Host.Pipeline.Tests/PipelineSinkServiceTest.cs)
— 8 tests:

- `EmptyInbound_SinkDelegateNeverInvoked`
- `SuccessOnFirstAttempt_NoRetry`
- `MultipleMessages_AllProcessedInOrder`
- `SuccessOnSecondAttempt_RetriedOnce` *(slow: ~1 s, the 1-second backoff before attempt 2)*
- `SuccessOnThirdAttempt_RetriedTwice` *(slow: ~3 s, 1 s + 2 s backoff before attempts 2 and 3)*
- `AllAttemptsFail_CtsCancelled_RunDoesNotPropagate` *(slow: ~3 s, full retry exhaustion)*
- `RetryExhaustion_StopsConsumingAfterFirstAbortedMessage` *(slow: ~3 s, full retry exhaustion + verifies queue tail untouched)*
- `DescriptorWithCustomServiceScope_IsInvokedInsteadOfDefaultCreateScope`

Test harness reuses the `StubDescriptor` pattern from
[step 11](11_PipelineSourceService_Tests.md). Each test owns its
own `BlockingCollection<PipelineMessage<int>>` populated via the
`Inbound(...)` helper which `Add`s the messages and immediately
calls `CompleteAdding()` so `GetConsumingEnumerable` terminates
naturally.

xUnit1051 suppressed at the file level for the same reason as
step 11.

## Verification

```text
$ dotnet test src/libs/Altinn.Authorization.Host/test/Altinn.Authorization.Host.Pipeline.Tests/...
Passed! - Failed: 0, Passed: 17, Skipped: 0, Total: 17, Duration: 11s 486ms
```

17 = 1 (`PipelineMessageSmokeTest`) + 8 (`PipelineSourceServiceTest`)
\+ 8 (new `PipelineSinkServiceTest`). Total ~11.5 s, dominated by
the ~10 s of retry-backoff `Task.Delay`s in the four slow tests.

## Deferred / follow-up

- **Investigate `PipelineSegmentService.EnumerateSegment` line 48 —
  `await Task.Delay(TimeSpan.FromSeconds(2))` runs *per consumed
  message* before the segment function is invoked.** That caps
  segment throughput at ~30 msg/min and forces every Run-level
  segment test to be 2 s+ even on the success path. Looks like a
  debugging artifact left in production. Should be filed as a Bug
  Issue for the team to confirm intent or remove. Once resolved,
  port the same retry-test suite from this step to the segment
  service.
- **`PipelineHostedService` lifecycle, lease handling, recurring
  scheduling** — separate Task; needs `FeatureManager` /
  `ILeaseService` mock setup which is heavier than the pure-logic
  pattern used here.
- **Builder fluent surface** — pass-through state-mutation, skip
  unless a real bug class emerges.
