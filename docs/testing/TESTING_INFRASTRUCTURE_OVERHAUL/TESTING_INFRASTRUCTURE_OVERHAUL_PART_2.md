# Testing Infrastructure Overhaul — Part 2 (Audit & Plan)

> **Status:** 📝 **Awaiting kickoff** — this document will be filled in when the
> fresh infrastructure audit begins. See the parent
> [`INDEX.md`](STEPS_PART_2/INDEX.md) for step-by-step execution.
>
> **Predecessor:** [`TESTING_INFRASTRUCTURE_OVERHAUL_PART_1.md`](TESTING_INFRASTRUCTURE_OVERHAUL_PART_1.md)
> (Steps 1–61, closed).
>
> **Retrospective:** [`TRACKING_RETROSPECTIVE.md`](TRACKING_RETROSPECTIVE.md)
> captures what made Part 1's tracking work well for Copilot — read this first
> before reshaping the Part 2 workflow.

---

## Why a Part 2?

By the end of Part 1 all of the originally identified issues (C1–C5, M1–M8,
L1–L3) were resolved and the phase plan was exhausted. The next highest-value
work was always going to be a **fresh audit** — many of the baseline numbers
have shifted materially since Step 12 and several new areas (`Host.Pipeline`,
`Host.Database`, `Host.MassTransit`, live-DB Npgsql repository code,
`Host.Lease` pending Azurite) are not yet covered by any plan.

Rather than continue appending "steps 62+" to Part 1, we start a fresh
overhaul document so that:

- The scope of Part 2 stays crisp and auditable (a new list of IDs, e.g.
  C1'–C*', M1'–M*').
- Part 1 remains a frozen historical record.
- Step numbering in `STEPS_PART_2/` starts at **Step 1** again, making
  cross-refs within Part 2 unambiguous.

---

## Table of Contents

1. [Current State Audit](#1-current-state-audit) — *TBD (kickoff step)*
2. [Findings & Issues](#2-findings--issues) — *TBD*
3. [Best Practices Already Followed](#3-best-practices-already-followed) — *TBD*
4. [Improvement Plan — Phases](#4-improvement-plan--phases) — *TBD*

---

## 1. Current State Audit

*To be populated by the Part 2 kickoff step. The audit should:*

- *Re-measure assembly-level coverage (line + branch) for every production
  assembly — many numbers have shifted materially since Step 12.*
- *Enumerate the current fixture inventory (`ApiFixture`,
  `AuthorizationApiFixture`, `LegacyApiFixture`, `EFPostgresFactory`,
  `PostgresFixture`) and verify each is still the right abstraction.*
- *Inventory test projects and their xUnit/TFM/MTP status (confirm the Part 1
  unifications have not regressed).*
- *List every assembly currently below 60% line coverage and classify each
  (pure-logic reachable / needs live DB / key-vault / Azurite / Program.cs).*
- *Call out any NEW sources of duplication introduced since Part 1 closed.*

---

## 2. Findings & Issues

*To be populated by the Part 2 kickoff step. Use a fresh ID namespace
(e.g. `C1'`, `M1'`, `L1'`) to distinguish from Part 1 issue IDs.*

---

## 3. Best Practices Already Followed

*To be populated by the Part 2 kickoff step. Carry forward anything from Part 1
that is still true, and add anything new established during Steps 1–61.*

---

## 4. Improvement Plan — Phases

*To be populated by the Part 2 kickoff step. Candidate phases (subject to
audit outcome):*

- **Phase A: Live-DB Npgsql repository coverage** — `AccessMgmt.Persistence`
  (32.51%) and `AccessManagement.Persistence` (44.94%). Probably via
  `EFPostgresFactory` template clones executed in a dedicated xUnit collection.
- **Phase B: Host.Lease coverage** — unblock by adding an Azurite
  Testcontainers fixture to `TestUtils`.
- **Phase C: Host.Pipeline / Host.Database / Host.MassTransit** — assess
  whether each warrants a dedicated test project; design decision + thin
  smoke suite.
- **Phase D: Coverage threshold ratchet** — promote assemblies that cleared
  their priority-1 gap into the enforced list; raise the `AccessManagement`
  main-app ratchet from warn-only to enforced once it crosses 60%.
- **Phase E: Housekeeping / drift** — whatever the fresh audit surfaces
  (duplicate-mock creep, fixture sprawl, TFM/MTP regressions, etc.).

---

## Execution Order & Dependencies

*Filled in at kickoff.*

---

## Decision Log

| Date | Decision | Rationale |
|---|---|---|
| Step 61 (Part 1) | Start a Part 2 document for the fresh audit instead of extending Part 1 | Keeps Part 1 as a frozen historical record; new issue IDs get a clean namespace; step numbering restarts at 1 |

---

*Last updated at the end of Part 1, Step 61 — awaiting the Part 2 kickoff
step.*
