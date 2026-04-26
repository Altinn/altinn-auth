# Testing Infrastructure Overhaul — Part 2 Step Log

## Getting Started & Workflow

**New chat?** Read these docs **in order** to get full context:

1. **This file** (`docs/testing/TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_2/INDEX.md`) —
   step log, coverage results, recommended next steps, deferred work, and
   workflow rules for **Part 2**.
2. **[TESTING_INFRASTRUCTURE_OVERHAUL_PART_2.md](../TESTING_INFRASTRUCTURE_OVERHAUL_PART_2.md)** —
   the Part 2 audit & phase plan (populated by the kickoff step).
3. **[TRACKING_RETROSPECTIVE.md](../TRACKING_RETROSPECTIVE.md)** — what made
   Part 1's tracking work for Copilot + recommended improvements to apply in
   Part 2 (read this before the first step).
4. **Historical context (Part 1):**
   [`../TESTING_INFRASTRUCTURE_OVERHAUL_PART_1.md`](../TESTING_INFRASTRUCTURE_OVERHAUL_PART_1.md)
   and [`../STEPS_PART_1/INDEX.md`](../STEPS_PART_1/INDEX.md). Read only as
   needed — Part 2 stands on its own.
5. **The step doc for the work you're about to do** (linked in the table below
   or in the Recommended Next Steps section).

**When completing a step:**

- **Create a step doc** (`docs/testing/TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_2/<Step_Name>.md`)
  describing the goal, what changed, verification results, and any deferred
  items. Add a row to the step log table below linking to the new doc.
- **Run all tests that were changed or impacted by the step** and record the
  results in the step doc. Update the [Final Coverage (measured)](#final-coverage-measured)
  table at the bottom of this file if the step affected coverage of any
  listed assembly (or a new assembly that should be tracked).
- **Re-check the [Blocked Items](#blocked-items) section** to see if anything
  is now unblocked by the completed step. If so, move it into
  `### Recommended Next Steps (priority order)` at an appropriate priority
  and remove it from the Blocked Items table.
- **Sweep `docs/testing/TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_2/` for
  obsoleted docs.** For each obsolete doc, either delete it and update links,
  or add a banner at the top pointing to the step that replaced it.
- **Commit and push** at the end of each step.
- **Recommend whether a new chat should be started** for the next step, based
  on complexity and context.
- **If a new chat is recommended, provide this ready-to-copy prompt** to hand
  off cleanly:

  > Continue Part 2 of the testing infrastructure overhaul on branch
  > `feature/2842_Optimize_Test_Infrastructure_and_Performance_Part_Two`.
  >
  > Start by reading
  > `docs/testing/TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_2/INDEX.md` —
  > it's the entry point and tells you exactly what to read next, how to pick
  > the next step, and the workflow rules for completing one.
  >
  > Then execute the highest-priority item from
  > `### Recommended Next Steps (priority order)` in that file.
- **Wait for explicit go-ahead** before proceeding to the next step.

**Picking the next step (when the list below is thinning):**

1. If `### Recommended Next Steps (priority order)` still has actionable
   items, take the highest-priority one.
2. If that list is empty or only contains blocked/unactionable items, consult
   [TESTING_INFRASTRUCTURE_OVERHAUL_PART_2.md](../TESTING_INFRASTRUCTURE_OVERHAUL_PART_2.md)
   for the next actionable item from the phase plan, and add it back to the
   list below before starting.
3. If `TESTING_INFRASTRUCTURE_OVERHAUL_PART_2.md` is also exhausted of
   actionable work, the next step should itself be **a fresh audit of the
   current testing infrastructure** to identify the next most valuable
   improvements — produce an updated audit doc (Part 3) and a refreshed
   recommended-next-steps list, then resume the cycle.

---

Steps are listed in the order they were **actually completed**, not by the
phase numbers in the [Part 2 overhaul plan](../TESTING_INFRASTRUCTURE_OVERHAUL_PART_2.md).

| # | Completed | Topic | Plan Phase | Doc |
|---|-----------|-------|------------|-----|
| *(empty — awaiting Part 2 kickoff step)* | | | | |

### Recommended Next Steps (priority order)

All items below are actionable unless otherwise noted.

1. **Part 2 kickoff — fresh infrastructure audit.** Populate
   `TESTING_INFRASTRUCTURE_OVERHAUL_PART_2.md` sections 1–4:
   - Re-measure assembly-level line + branch coverage across the whole repo
     (run `eng/testing/run-coverage.ps1` and
     `eng/testing/run-accessmanagement-coverage.ps1`); record the new
     baseline in the [Final Coverage (measured)](#final-coverage-measured)
     table below.
   - Inventory test projects: xUnit version, TFM, MTP routing, fixture style —
     confirm Part 1 unifications have not regressed.
   - Inventory fixtures (`ApiFixture`, `AuthorizationApiFixture`,
     `LegacyApiFixture`, `EFPostgresFactory`, `PostgresFixture`) and any new
     entrants; flag sprawl.
   - Classify every sub-60% assembly (pure-logic reachable / needs live DB /
     key-vault / Azurite / `Program.cs`).
   - Produce a fresh issue list using a new ID namespace (`C1'`, `M1'`,
     `L1'`, …) to avoid Part 1 collision.
   - Produce a phase plan (candidate phases listed in
     `TESTING_INFRASTRUCTURE_OVERHAUL_PART_2.md` §4) and seed this
     *Recommended Next Steps* list from it.
2. **Apply tracking improvements from the retrospective.** Before starting
   coverage/fixture work, walk through
   [`../TRACKING_RETROSPECTIVE.md`](../TRACKING_RETROSPECTIVE.md) and adopt
   any improvements that make sense for Part 2 (e.g. a structured per-step
   front-matter block, a `BLOCKED`/`PARTIAL` legend, a coverage-delta column
   in the step log table).

### Blocked Items

| Item | Blocker | Notes |
|---|---|---|
| Phase 6.5 (Part 1 carry-over): `Host.Lease` tests | Azure Storage Emulator/Azurite required | See [`../TESTING_INFRASTRUCTURE_OVERHAUL_PART_1.md`](../TESTING_INFRASTRUCTURE_OVERHAUL_PART_1.md) Phase 6.5. Natural candidate for Part 2 Phase B — add an Azurite Testcontainers fixture to `TestUtils`. |
| `Sender_ConfirmsDraftRequest_ReturnsPending` (Part 1 carry-over) | Environmental investigation needed | `[Skip]`ped during Step 51 of Part 1 after the `ResourceRegistryMock` cache-hit fix landed. Re-assess during Part 2 kickoff. |

### Final Coverage (measured)

*To be populated by the Part 2 kickoff step. Copy the final Part 1 table from
[`../STEPS_PART_1/INDEX.md`](../STEPS_PART_1/INDEX.md) as a starting point,
then overwrite each row with the newly-measured number.*
