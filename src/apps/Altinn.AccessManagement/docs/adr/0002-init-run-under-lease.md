# ADR-0002: Migrations and seed data run in a separate init run, under a lease

- **Status:** Accepted
- **Date:** 2025-10-13, when the init container started running the init ([#1490](https://github.com/Altinn/altinn-auth/pull/1490)). The lease was added on 2026-05-07 ([#3088](https://github.com/Altinn/altinn-auth/pull/3088)). Recorded on 2026-10-06.
- **Deciders:** Implemented by @andreasisnes. The lease was asked for by @jonkjetiloye ([#3086](https://github.com/Altinn/altinn-auth/issues/3086)) and @Thuen ([#3116](https://github.com/Altinn/altinn-auth/issues/3116)).

**TL;DR**

- EF migrations, static data and the wait for the first import from Register run in a separate process started with `RunInitOnly=true`, before the app serves traffic.
- The serving app never migrates. A normal start skips all of it.
- A blocking lease, `access_management_init`, makes parallel init runs wait for each other, because parallel deploys to two clusters crash-looped without it.
- **Gap:** the pull requests that moved init out of normal startup give no reason in writing. The team should add it.

> Recorded after the fact for [#4079](https://github.com/Altinn/altinn-auth/issues/4079), from the sources under References and nothing else. Drafted with Claude (Opus 5.5). Human reader: not confirmed.

## Context

Access Management needs more than a schema before it can answer correctly: the EF migrations ([ADR-0001](0001-ef-core-persistence.md)), static data such as roles and packages, and parties and roles imported from Register. [#1545](https://github.com/Altinn/altinn-auth/issues/1545) puts the requirement as making sure the database contains all the required data before the app starts.

The init was moved into its own run in two steps, [#1062](https://github.com/Altinn/altinn-auth/pull/1062) (2025-08-21) and [#1490](https://github.com/Altinn/altinn-auth/pull/1490) (2025-10-13). Neither pull request states why it is separate from normal startup, and no other source found says so either.

In May 2026 the database migration failed in TT02 whenever deploys ran in parallel, which happens because there are two platform clusters. That caused crash loops and very long deploys ([#3086](https://github.com/Altinn/altinn-auth/issues/3086), [#3116](https://github.com/Altinn/altinn-auth/issues/3116)). EF's `MigrateAsync` does not lock on its own the way Yuniql did, as [#3603](https://github.com/Altinn/altinn-auth/issues/3603) notes for Authorization.

## Decision

1. **Init is a separate run.** With `RunInitOnly=true`, `Program.cs` runs `Init()` and exits. `Init()` migrates the database, ingests static data (`StaticDataIngest.IngestAll`), and, when the Register import feature flag is on, waits until the first import from Register has completed ([#1548](https://github.com/Altinn/altinn-auth/pull/1548), 2025-10-21). A normal start does none of this. `RunIntegrationTests` also runs `Init()`, and then continues to serve.
2. **Init runs under a blocking lease.** `Init()` takes the lease `access_management_init` before it touches the database, so a second init run waits instead of migrating at the same time ([#3088](https://github.com/Altinn/altinn-auth/pull/3088), [#3152](https://github.com/Altinn/altinn-auth/pull/3152)). This reuses the lease mechanism the hosted services already used ([#3116](https://github.com/Altinn/altinn-auth/issues/3116)).

### Alternatives considered

- **A lease taken by the init run**, as the hosted services already used. Chosen in #3116.
- **A PostgreSQL advisory lock around `MigrateAsync` at startup.** This is what Authorization did when it moved to EF ([#3602](https://github.com/Altinn/altinn-auth/issues/3602)); it was not discussed for Access Management.

## Consequences

- Every deployment must run the init before the app, or the app starts against a database that is missing schema or data. In this repository Flux runs a pre-deploy Job with `RunInitOnly=true` before the deployment (`manifests/pre-deploy/base/02-db-migration.yaml`; the deployment `dependsOn` it). Its manifests exist for at22 only; how the other environments start the init is not visible here.
- Locally, a fresh database needs one run with `--RunInitOnly=true` before the normal `dotnet run`.
- A stuck lease holds up a deploy. The alert #3086 asked for, when an init stays locked for 10 to 15 minutes, is still an unticked task in that closed issue.

## References

- Init run: [#1062](https://github.com/Altinn/altinn-auth/pull/1062), [#1490](https://github.com/Altinn/altinn-auth/pull/1490), [#1545](https://github.com/Altinn/altinn-auth/issues/1545), [#1548](https://github.com/Altinn/altinn-auth/pull/1548).
- Lease: [#3086](https://github.com/Altinn/altinn-auth/issues/3086), [#3116](https://github.com/Altinn/altinn-auth/issues/3116), [#3088](https://github.com/Altinn/altinn-auth/pull/3088), [#3152](https://github.com/Altinn/altinn-auth/pull/3152).
- Code: `Init()` and the `RunInitOnly` branch in `src/Altinn.AccessManagement/Program.cs`; `RunInitOnly` in `AccessManagementAppsettings.cs`; `manifests/pre-deploy/base/02-db-migration.yaml`; `syncroot/base/03-deployment.yaml`.
