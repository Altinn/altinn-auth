# Architecture Decision Records (ADRs)

An ADR captures a single architectural decision: its **context**, the **decision**, and its **consequences**. ADRs explain the *why*, the thing source code and git history don't preserve and that humans and AI agents otherwise re-derive at great cost.

## Where ADRs live

| Scope | Folder |
| --- | --- |
| Cross-cutting: repository conventions, build and CI, dependencies between verticals | `docs/adr/` (this folder) |
| One vertical: flows, policy evaluation, API contracts, data model | `src/apps/<Vertical>/docs/adr/` (and the equivalent under `src/pkgs`, `src/libs`, `src/tools`) |

Each folder numbers its own ADRs from `0001`. A repository that is moved into this monorepo keeps its ADR numbers; only the folder changes. See [ADR-0001](0001-record-architecture-decisions.md).

## Rules

- ADRs are **immutable**. Once accepted, a decision is never edited. If it changes, write a **new** ADR that supersedes the old one and mark the old one `Superseded by ADR-XXXX`.
- Numbered sequentially per folder, zero-padded: `0001`, `0002`, …
- Keep them short (about one page). Link to code, PRs and issues for detail.
- Use [template.md](template.md) for new ADRs.
- Add the ADR to the index below (or to the vertical's own index) in the same PR.

## Index

| ADR | Title | Status |
| --- | --- | --- |
| [0001](0001-record-architecture-decisions.md) | Record architecture decisions | Accepted |
| [0002](0002-tool-neutral-agent-contract.md) | Tool-neutral agent contract: `AGENTS.md` as the single source | Accepted |
