# ADR-0003: Duplicate authorization events may be dropped in the PDP within a window

- **Status:** Proposed
- **Date:** 2026-11-13
- **Deciders:** Not decided. Proposed in [#4127](https://github.com/Altinn/altinn-auth/issues/4127), to be decided together with [altinn-auth-audit-log#312](https://github.com/Altinn/altinn-auth-audit-log/issues/312) and [#4285](https://github.com/Altinn/altinn-auth/issues/4285).

**TL;DR**

- Proposed: the PDP drops an authorization event instead of queuing it when the same event was logged within the last minute. Behind the feature flag `AuditLogDuplicateSuppression`, off by default.
- The reason: about half of the events in TT02, and at least a quarter in production, repeat one already logged within a minute. Nearly all come from separate requests, not from repeats within one request.
- What is lost for the repeats: how many there were, their times and their trace ids. Who, what, which instance, which action, which decision, session and IP are the same as in the event that was logged.
- An event is remembered only once the queue has it, so a failed send never makes a later event look like a duplicate.
- When accepted, this amends decision 2 of [ADR-0002](0002-audit-events-through-a-queue.md), "one event per single decision request".

> Drafted with Claude (Opus 5.5) from the measurements in #4127, #4130 and #4290. Human reader: not confirmed.

## Context

The PDP queues one authorization event for each single decision request ([ADR-0002](0002-audit-events-through-a-queue.md)). Each event becomes a queue message, a run of the audit-log consumer and a row in `authz.eventlogv1`. The audit-log database is too slow to analyse directly, so the PDP measures repeats itself ([#4130](https://github.com/Altinn/altinn-auth/pull/4130), on by default since [#4290](https://github.com/Altinn/altinn-auth/pull/4290)).

Two events are the same when every field persisted as an audit-log column is equal (resource, instance, resource party, subject, session, IP address, action and decision), and every value of `urn:altinn:resource:instance-id` is too. The window is one minute from the first occurrence, so an event that repeats continuously is still logged once a minute.

Measured per pod:

- **TT02**, one hour on four pods: about 46 % of events were duplicates, about 2 % within the same trace and about 44 % from other traces.
- **Production**, one day: a clearly lower share than TT02, but still at least a quarter of all events, again nearly all from other traces. Exact figures are production telemetry and are not recorded here.

Each pod has its own tracker, so repeats that reach another pod are not seen. The shares are a lower bound for duplicates in the log as a whole, and an exact figure for what dropping in the PDP would save with today's routing.

The likely main source is Altinn apps saving form data each time a user leaves a field. Each save is a separate request that asks the PDP the same question. [#4289](https://github.com/Altinn/altinn-auth/issues/4289) reduces those calls at the source.

## Decision

We will make it possible for the PDP to drop duplicate events, behind the feature flag `AuditLogDuplicateSuppression`, and switch it on only when this ADR is accepted.

1. **Scope is configurable.** `AuditLogDeduplication:SuppressScope` is `Trace`, which drops only repeats within the same request, or `Window`, which also drops repeats from other requests within the window. `Trace` is the default. The saving is in `Window`.
2. **An event is remembered only after the queue has it.** The PDP classifies the event without remembering it, drops it if it is a duplicate within scope, and otherwise queues it and remembers it when the send succeeds. A failed send therefore never hides a later event. The cost is that identical events in flight at the same moment can all be logged. For the same reason, dropping keeps state of its own, separate from the measurement, which remembers every event whether or not it was queued.
3. **The window belongs to the logged event.** A repeat that is dropped does not start a window of its own. Its trace is remembered from the time of the event it repeats, so a repeat is dropped only while that event is within the window, never up to two windows after it.
4. **Failures log too much rather than too little.** If classifying fails, the event is queued. Nothing in the measurement or the dropping can fail the decision.
5. **What is dropped is counted.** `altinn.pdp.auditlog.suppressed`, tagged `trace` or `window`, sits beside `altinn.pdp.auditlog.events`, which keeps counting every event produced.

### Alternatives considered

- **Drop only within the same trace.** Uncontroversial, since it is one operation asking twice, but it saves only a small fraction, in production as in TT02 (about 2 %). Kept as the cautious first setting rather than the goal.
- **Deduplicate only in the audit-log service.** It sees traffic from every pod and would catch more, but the PDP would still send every message, and the queue is where events are lost ([altinn-auth-audit-log#307](https://github.com/Altinn/altinn-auth-audit-log/issues/307)). [#4130](https://github.com/Altinn/altinn-auth/pull/4130) concluded both ends should do it, with a shared cache on the audit-log side.
- **Fewer PDP calls instead** ([#4289](https://github.com/Altinn/altinn-auth/issues/4289)). Complementary: it removes calls as well as events, but needs changes in the PEP, app-lib and Storage, and does not cover other callers.
- **Remember an event when it is classified, as the measurement does.** Catches concurrent repeats, but a failed send would then suppress the repeats that could have replaced it.
- **Log a count of suppressed repeats with the next event.** Would keep how often access was used, but needs a change to the event and the audit-log schema. Not pursued.

## Consequences

- For a repeated access within a minute, the audit log keeps the first occurrence only. A question such as "how many times did this user read this message, and when" can then only be answered to the minute. Whether that is acceptable is what [altinn-auth-audit-log#312](https://github.com/Altinn/altinn-auth-audit-log/issues/312) and [#4285](https://github.com/Altinn/altinn-auth/issues/4285) must answer. They ask the same question and should answer it once.
- Events are compared by a 64-bit XxHash3 key with a random per-process seed. A collision would drop a real event. With 200 000 keys in a window that is about one in a billion by chance, and a crafted collision would need the seed, which never leaves the process. If that is not strong enough for an audit record, a keyed hash such as SipHash or HMAC is the alternative, at a higher cost per event.
- Each PDP pod deduplicates on its own, so more pods mean fewer duplicates caught. The audit-log side with a shared cache covers the rest.
- Turning the flag on per environment is a change in `manifests/deploy/<environment>`. TT02 starts with `Window` ([#5005](https://github.com/Altinn/altinn-auth/pull/5005)), since `Trace` and `Window` run the same code and only `Window` saves enough to learn from; compare queued events with `suppressed`. Production only when this ADR is accepted.

## References

- Issue and measurements: [#4127](https://github.com/Altinn/altinn-auth/issues/4127), [#4130](https://github.com/Altinn/altinn-auth/pull/4130), [#4290](https://github.com/Altinn/altinn-auth/pull/4290).
- Open questions: [altinn-auth-audit-log#312](https://github.com/Altinn/altinn-auth-audit-log/issues/312), [#4285](https://github.com/Altinn/altinn-auth/issues/4285). Related: [#4289](https://github.com/Altinn/altinn-auth/issues/4289), [altinn-auth-audit-log#332](https://github.com/Altinn/altinn-auth-audit-log/issues/332).
- Code: `src/Altinn.Authorization/Services/Implementation/EventLogService.cs`, `src/Altinn.Authorization/Services/Implementation/AuthorizationEventDuplicateTracker.cs`.
