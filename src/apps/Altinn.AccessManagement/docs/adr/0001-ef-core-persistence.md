# ADR-0001: EF Core for persistence; the older persistence is retired, not converted

- **Status:** Accepted
- **Date:** 2025-06-17, when the direction was set in [#861](https://github.com/Altinn/altinn-auth/issues/861). Recorded on 2026-10-06.
- **Deciders:** The Access Management project, as stated in #861 by @jonkjetiloye. Implemented by @Thuen; the scope for the older schemas set by @howieandersen, who also decided the Core-layer rule in decision 6 on 2026-10-13.

**TL;DR**

- New persistence code and schema changes go into `Altinn.AccessMgmt.PersistenceEF`: `AppDbContext` and EF Core migrations.
- The team's own repository and migration framework was replaced and then deleted.
- The older `Altinn.AccessManagement.Persistence` (raw Npgsql, Yuniql) is retired together with the old APIs, not converted. Only `consent` moved to EF.
- Audit stays in database triggers. Schema changes are EF migrations, with hand-written SQL only where EF falls short.
- Where a project exists in both families, `Altinn.AccessMgmt.*` is the new one: new Core-layer code goes in `Altinn.AccessMgmt.Core`.

> Recorded after the fact for [#4079](https://github.com/Altinn/altinn-auth/issues/4079), from the sources under References and nothing else. Drafted with Claude (Opus 5.5). Human reader: not confirmed.

## Context

By mid-2025 Access Management ran on its own framework, `Altinn.AccessMgmt.Persistence`, which generated tables, constraints, indexes and views from models by reflection and built queries. Beside it sat the older `Altinn.AccessManagement.Persistence`, raw Npgsql with Yuniql scripts for the `delegation`, `accessmanagement` and `consent` schemas.

In June 2025 the project wanted to stop developing the in-house solution for migrations and query building, and to use EF Core directly ([#861](https://github.com/Altinn/altinn-auth/issues/861)). The issue worked through what that meant per area:

- **Audit** logs every change through triggers that rely on `changed_by` values being set, and `CASCADE` delete had already been a problem for it.
- **Migrations**: EF has weak support for the advanced model without writing the scripts yourself.
- **Ingest** of static data is an efficient MERGE-based service already used in several places.

## Decision

1. **EF Core owns Access Management's model and migrations** in `Altinn.AccessMgmt.PersistenceEF`. The first part landed in [#968](https://github.com/Altinn/altinn-auth/pull/968) (2025-08-14), whose follow-up list ended with removing the in-house framework. That was done in [#3099](https://github.com/Altinn/altinn-auth/pull/3099) (2026-05-22).
2. **Audit stays in database triggers**, and `UseEfAudit()` supplies who made the change. In 2025, #861 expected that EF could not manage `CASCADE` alongside the triggers. That no longer holds everywhere: `AssignmentPackage` has audit enabled and a cascade delete configured in EF, added by the migration `20260205102541_AddCascadeDeleteForAssignmentPackageToDelegationPackage`. The sources do not record when or why that became acceptable.
3. **Schema changes are EF migrations.** #861 expected the SQL to be written by hand. In practice most migrations use EF's own operations, and some also call `migrationBuilder.Sql` for what EF does not express. `CustomMigrationsSqlGenerator` adds the audit-trigger DDL to each table operation, because EF Core has no public API for it (its own comment).
4. **The older schemas are retired, not converted.** When migrations moved off Yuniql ([#3602](https://github.com/Altinn/altinn-auth/issues/3602)), only `consent` moved to EF, as `ConsentSchema_Baseline` ([#3606](https://github.com/Altinn/altinn-auth/issues/3606)). `delegation` and `accessmanagement` are no longer in use and are removed together with the old APIs, so they stay on Yuniql until then. The end state is deleting `Altinn.AccessManagement.Persistence` ([#3654](https://github.com/Altinn/altinn-auth/issues/3654)).
5. **Old schemas go before consent is rewritten.** Rewriting the consent repository onto EF ([#3433](https://github.com/Altinn/altinn-auth/issues/3433)) is not scheduled. The team decided to remove the old schemas first ([#3655](https://github.com/Altinn/altinn-auth/issues/3655)), so the rewrite is not written against a schema about to change (comment on [#3652](https://github.com/Altinn/altinn-auth/issues/3652), 2026-08-15). The attempt in #3652 and its pull request #3674 was closed on 2026-10-09.
6. **New Core-layer code goes in `Altinn.AccessMgmt.Core`.** Where a project exists in both families, `Altinn.AccessMgmt.*` is the new one. Code still in `Altinn.AccessManagement.Core` is maintained there until it is migrated in large chunks. Decided by @howieandersen in [review of this ADR](https://github.com/Altinn/altinn-auth/pull/4332#discussion_r4257966247) as the service-layer half of the same move.

### Alternatives considered

- **Keep developing the in-house framework.** Rejected in #861.
- **Convert every Yuniql schema to EF.** Rejected in #3602 for `delegation` and `accessmanagement`, since they are being removed.
- **Model `consent` as EF entities straight away.** Deferred: #3606 moved only the schema, and the repository rewrite is #3433.

## Consequences

- A schema change ships as an EF migration in `Altinn.AccessMgmt.PersistenceEF`. The Yuniql workspace ends in no-op tombstone folders for `consent`, kept so the version sequence holds (#3602).
- Removing Yuniql from the repository is blocked until Access Management and the shared libraries stop using it (#3602).
- Two similar names coexist for a while. `Altinn.AccessMgmt.Persistence` was the in-house framework and is gone; `Altinn.AccessManagement.Persistence` is the older family that is being retired.
- #968 planned to remove `Altinn.AccessMgmt.Core`; instead it became the service layer of the EF model, which decision 6 makes the home for new code. Until the older Core is migrated, a fix to existing code lands in whichever family that code lives in.

## References

- Direction and per-area analysis: [#861](https://github.com/Altinn/altinn-auth/issues/861).
- Implementation: [#968](https://github.com/Altinn/altinn-auth/pull/968), [#3099](https://github.com/Altinn/altinn-auth/pull/3099).
- Off Yuniql: [#3602](https://github.com/Altinn/altinn-auth/issues/3602), [#3606](https://github.com/Altinn/altinn-auth/issues/3606). Still open: [#3342](https://github.com/Altinn/altinn-auth/issues/3342), [#3433](https://github.com/Altinn/altinn-auth/issues/3433), [#3654](https://github.com/Altinn/altinn-auth/issues/3654), [#3655](https://github.com/Altinn/altinn-auth/issues/3655). Closed as not planned: [#3652](https://github.com/Altinn/altinn-auth/issues/3652).
- Code: `src/Altinn.AccessMgmt.PersistenceEF/Contexts/AppDbContext.cs`, `.../Extensions/CustomMigrationsSqlGenerator.cs`, `.../Configurations/AssignmentPackageConfiguration.cs`, `.../Migrations/20260623145805_ConsentSchema_Baseline.cs`; `UseEfAudit()` in `src/Altinn.AccessManagement/Program.cs`; `AddYuniqlMigrations` in `src/Altinn.AccessManagement.Persistence/Extensions/PersistenceDependencyInjectionExtensions.cs`.
