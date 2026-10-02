# ADR-0001: Server-controlled resource timestamps

## TL;DR

- Expose first registration and metadata version save time as `createdAt` and `updatedAt`.
- Read authoritative database columns; ignore timestamps in client metadata JSON.
- Keep unknown timestamps null and policy changes in the existing change feed.

Drafted with Codex (GPT-6). Human reader: [@howieandersen, review of PR #4300](https://github.com/Altinn/altinn-auth/pull/4300#pullrequestreview-5376363483).

- **Status:** Proposed
- **Date:** 2026-10-01
- **Deciders:** Pending explicit human acceptance.

## Context

Consumers need timestamps on resource responses. The database already stores them,
but versioning makes `resources.created` different from original registration time.
The same model represents virtual apps from Storage, which have no registry timestamps.
Policy changes have a separate timestamp on the resource identity, without historical
policy timestamps for each metadata version.

## Decision

Expose nullable `DateTimeOffset` properties on `ServiceResource`, serialized in UTC:
`createdAt` comes from `resource_identifier.created`; `updatedAt` comes from the returned
version's `resources.modified`. All repository return paths use the same mapping.
Both creation inserts use PostgreSQL `now()` within the same transaction, and updates
use the same database clock. App-host clock skew cannot change the timestamp ordering.

Database columns are authoritative. Exclude these properties from persisted metadata JSON
and ignore any historical JSON values during reads. POST and PUT may include the fields
when round-tripping a GET response, but the supplied values do not affect stored timestamps.

Every metadata PUT creates a version, including unchanged content, so it advances
`updatedAt`. Policy uploads do not change metadata timestamps. The existing change feed
continues to report metadata and policy changes together.

Return explicit null values for virtual Storage apps and for missing historical
modification times. Do not infer a modification time from registration time or copy
Storage timestamps with different semantics. No migration or backfill is required.

## Consequences

Single-resource, list, search, and historical responses agree for the same version.
Consumers must handle null values. The additive fields do not change POST/PUT response
bodies or introduce a new API version. The contract reports registry save times, not
external creation time or the time of the last actual content difference.

## References

- [Issue #4296](https://github.com/Altinn/altinn-auth/issues/4296)
- [Change feed #4241](https://github.com/Altinn/altinn-auth/issues/4241)
- [Implemented change feed](https://github.com/Altinn/altinn-resource-registry/issues/812)
- [Resource API documentation and implementation guidance](../../README.md)
