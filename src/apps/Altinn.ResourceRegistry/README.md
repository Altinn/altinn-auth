# Resource Registry

## TL;DR

- Resource responses expose server-controlled `createdAt` and `updatedAt` timestamps in UTC.
- `createdAt` is the first registration in this registry; `updatedAt` describes the returned metadata version.
- Unknown timestamps are returned as JSON `null`, including both timestamps for virtual Storage apps.
- The `resource/changes` feed links every page with items to the next position, so a consumer can store the link and resume.

Timestamp documentation drafted with Codex (GPT-6). Human reader: [@howieandersen, review of PR #4300](https://github.com/Altinn/altinn-auth/pull/4300#pullrequestreview-5376363483).

See [architecture documentation](https://docs.altinn.studio/technology/architecture/components/application/solution/altinn-platform/authorization/resourceregistry/).

## Resource timestamps

`GET /resourceregistry/api/v1/resource/{id}` (including `?versionId=...`),
`GET /resourceregistry/api/v1/resource/resourcelist`, and
`GET /resourceregistry/api/v1/resource/Search` include:

| Field | Meaning |
| --- | --- |
| `createdAt` | First registration in Resource Registry, unchanged across versions. |
| `updatedAt` | When the returned metadata version was saved. Equal to `createdAt` on creation; null for legacy versions without a recorded modification time. |

Both are nullable ISO 8601 timestamps with UTC offset, assigned by the database clock. POST and PUT ignore client-supplied
values; database columns are authoritative. Every successful metadata PUT saves a new
version and updates `updatedAt`, even when the content is unchanged. Policy uploads do
not change either field; use the existing `resource/changes` feed for metadata and policy
changes together. Neither timestamp means first creation in an external source system.

Virtual app resources fetched from Storage have both fields set to null because they
have no registration or metadata version in this registry. Apps published into Resource
Registry use their registry timestamps.

## Resource change feed

`GET /resourceregistry/api/v1/resource/changes?token=...&limit=...` lists resources whose
metadata or policy changed, ordered by change, each resource once at its latest change.
Resources without an uploaded policy and deleted resources are not listed.

The feed is meant to be followed over time. Every page with items has a `links.next`,
also the last one; only an empty page has none, which means the consumer has caught up.
Store the latest `links.next` you received and request it again on the next poll; do not
stop following the feed when a page has fewer items than `limit`. The token is opaque.

## Implementation guidance

Always obtain resource creation time from `resource_identifier.created`. The `created`
column in `resources` and `current_resources` belongs to the individual version and
changes on each update. Timestamp fields must remain excluded from persisted metadata
JSON; map them from the database columns after deserialization.

Run this vertical through its own solution with a working Docker/Podman runtime:
`dotnet test src/apps/Altinn.ResourceRegistry/Altinn.ResourceRegistry.slnx`.
Its tests use xUnit v2 and require real PostgreSQL; do not apply the repository's xUnit v3 category filter.

## Architecture decisions

| ADR | Title | Status |
| --- | --- | --- |
| [0001](docs/adr/0001-resource-timestamps.md) | Server-controlled resource timestamps | Proposed |
