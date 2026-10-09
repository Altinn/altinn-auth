# ADR-0002: Decisions are audit-logged through a queue to a separate, write-only store

- **Status:** Accepted
- **Date:** 2023-10-17, when the PDP started sending authorization events ([altinn-authorization#490](https://github.com/Altinn/altinn-authorization-archive/pull/490)). Recorded on 2026-10-06.
- **Deciders:** Requirement by @ekorra ([altinn-auth-audit-log#3](https://github.com/Altinn/altinn-auth-audit-log/issues/3)), architecture by @TheTechArch ([altinn-auth-audit-log#2](https://github.com/Altinn/altinn-auth-audit-log/issues/2)). Implemented in the PDP by @acn-dgopa.

**TL;DR**

- The PDP puts one authorization event per single decision request on an Azure Storage Queue. A separate audit-log service stores it; the PDP never reads it back.
- The reason given is that logging must not slow the rest of the solution down, and the log must be write-only.
- Multi-requests are not logged, because of the volume.
- The send is not awaited, so a lost event does not fail a decision. No source discusses that trade-off.
- **No legal requirements are on record** for what is logged or for how long.

> Recorded after the fact for [#4079](https://github.com/Altinn/altinn-auth/issues/4079), from the sources under References and nothing else. Drafted with Claude (Opus 5.5). Human reader: not confirmed.

## Context

Altinn 3 needed a log of who was given access to, or acted on, what and when, for both authentication and authorization ([altinn-auth-audit-log#3](https://github.com/Altinn/altinn-auth-audit-log/issues/3), 2023-03-06). The requirement says the log must be write-only, with high accuracy and integrity, so that it can be used in investigations of misuse. It also says the logging must not harm the performance of the rest of the solution, and that a queue is set up for each of the two logs.

The architecture in [altinn-auth-audit-log#2](https://github.com/Altinn/altinn-auth-audit-log/issues/2) put Queue Storage first "for high-performance logging", so producers do not wait for a database write. A separate component stores the events in PostgreSQL, in separate tables for authentication and authorization, with a write-only application user.

## Decision

1. **The PDP produces, the audit-log service stores.** `EventLogService` builds an authorization event from the request and the decision, and `EventsQueueClient` puts it on the queue ([altinn-authorization#490](https://github.com/Altinn/altinn-authorization-archive/pull/490)). Storing, retention and reading belong to [altinn-auth-audit-log](https://github.com/Altinn/altinn-auth-audit-log).
2. **One event per single decision request; multi-requests are not logged.** Apps made so many calls that logging them produced enormous volumes ([altinn-authorization#554](https://github.com/Altinn/altinn-authorization-archive/issues/554)). Multi-requests were excluded in [altinn-authorization#652](https://github.com/Altinn/altinn-authorization-archive/pull/652) (2024-01-11).
3. **Logging is switched on per environment** with the feature flag `AuditLog`, which is off in `appsettings.json`.

### Alternatives considered

- **Cache in the PDP, cache in the apps, or log by time instead of per call.** All raised in #554 as answers to the volume. Excluding multi-requests was what was implemented.
- **Writing to the database directly from the PDP.** Not discussed as such. #2 chose a queue so that producers do not wait for a database write.

## Consequences

- A decision never waits for, or fails because of, audit logging. The flip side is that a failed send loses that event silently: `EnqueueAuthorizationEvent` is called without being awaited. No source states this as intended. Anyone who needs every decision logged should make it a new decision.
- What a decision puts in the log, and whether duplicates are dropped, is a change to an audit record that others may rely on. [#4127](https://github.com/Altinn/altinn-auth/issues/4127) measures duplicate events and asks for an ADR to be considered before any are dropped.
- The log was built without formal requirements. The legal assessment asked for in [altinn-auth-audit-log#4](https://github.com/Altinn/altinn-auth-audit-log/issues/4) is still open, and [altinn-auth-audit-log#314](https://github.com/Altinn/altinn-auth-audit-log/issues/314) (2026) restates the gap. Do not describe the log as meeting a legal requirement until those are settled.

## References

- Requirement and architecture: [altinn-auth-audit-log#3](https://github.com/Altinn/altinn-auth-audit-log/issues/3), [#2](https://github.com/Altinn/altinn-auth-audit-log/issues/2). Open: [#4](https://github.com/Altinn/altinn-auth-audit-log/issues/4), [#314](https://github.com/Altinn/altinn-auth-audit-log/issues/314).
- PDP side: [altinn-authorization#490](https://github.com/Altinn/altinn-authorization-archive/pull/490), [altinn-authorization#554](https://github.com/Altinn/altinn-authorization-archive/issues/554), [altinn-authorization#652](https://github.com/Altinn/altinn-authorization-archive/pull/652). Later: [#4127](https://github.com/Altinn/altinn-auth/issues/4127), [#4290](https://github.com/Altinn/altinn-auth/pull/4290).
- Code: `src/Altinn.Authorization/Services/Implementation/EventLogService.cs`, `src/Altinn.Authorization/Clients/EventsQueueClient.cs`, the `logEvent` handling in `Controllers/DecisionController.cs`.
- `altinn-authorization` is archived as `altinn-authorization-archive`; its code moved here without history.
