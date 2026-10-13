# Architecture decision records: Altinn.AccessManagement

Decisions that concern only this vertical: rights and delegations, its data model and storage, and its API contracts. Cross-cutting decisions live in the [root `docs/adr/`](../../../../../docs/adr/README.md), which also holds the rules and the [template](../../../../../docs/adr/template.md). Write one when the [threshold in CONTRIBUTING.md](../../../../../CONTRIBUTING.md#when-an-adr-is-required) is met.

Numbering starts at `0001` in this folder. ADR-0001 and ADR-0002 were recorded after the fact ([#4079](https://github.com/Altinn/altinn-auth/issues/4079)): their date is when the decision was taken, and they say only what their sources say. Where a source gives no reason, the ADR says so.

## Index

| ADR | Title | Status |
| --- | --- | --- |
| [0001](0001-ef-core-persistence.md) | EF Core for persistence; the older persistence is retired, not converted | Accepted |
| [0002](0002-init-run-under-lease.md) | Migrations and seed data run in a separate init run, under a lease | Accepted |
