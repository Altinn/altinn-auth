# Tracking Retrospective — What Worked for GitHub Copilot in Part 1

> **Purpose:** Capture the specific properties of
> [`TESTING_INFRASTRUCTURE_OVERHAUL_PART_1.md`](TESTING_INFRASTRUCTURE_OVERHAUL_PART_1.md)
> and [`STEPS_PART_1/INDEX.md`](STEPS_PART_1/INDEX.md) that made
> long-running, multi-session, AI-assisted work efficient across **60+ steps
> and multiple chat sessions** — and recommend concrete improvements to
> apply in Part 2.
>
> **Audience:** The human driving the work, and any Copilot instance picking
> the work up in a fresh chat.

---

## 1. What made Part 1 tracking effective

### 1.1 A single, canonical entry point (`INDEX.md`)

Every new Copilot session was onboarded with one file to read first. Because
`INDEX.md` contained:

- The **workflow rules** (how to complete a step) at the very top.
- A **chronological step log** (what has already been done, with one-line
  summaries rich enough to avoid re-reading step docs for context).
- A **Recommended Next Steps (priority order)** list (what to do next).
- A **Blocked Items** table (what *not* to pick up).
- A **Final Coverage (measured)** table (ground truth for ratchet decisions).

…Copilot could reconstitute full project context in seconds without sampling
the entire repo. This is the single highest-leverage property of the whole
system.

### 1.2 Self-describing handoff prompt embedded in the workflow

The `INDEX.md` workflow section contained the **exact prompt** to paste into
a new chat. No prompt rewriting, no context drift between sessions, no
re-deriving what "continue the work" means. Copy, paste, go.

### 1.3 One-line-but-dense table rows in the step log

Each row in the step log was long — intentionally. It captured:

- Phase/plan reference (`Phase 6.7d`, `Phase 2.2`, etc.).
- The **concrete artifact** (class names, test counts, coverage deltas).
- The **why** (bug symptom + fix, or test technique used).
- A link to the full step doc.

This made the step log itself a **searchable grep corpus**. Questions like
"when did we enable `InternalsVisibleTo` for `AccessMgmt.Core`?" were
answerable by scanning `INDEX.md` alone — no need to open 40 step docs.

### 1.4 Step docs as reproducible records

Each `<N>_<Name>.md` in `STEPS_PART_1/` followed a consistent skeleton:

- **Goal** — what problem this step addresses.
- **What changed** — file-by-file.
- **Verification** — tests run + results.
- **Deferred / follow-up** — explicitly written down so it doesn't get lost.

This made it trivial to revive a cold step from scratch, and to attribute any
later regression to the step that introduced it.

### 1.5 `TESTING_INFRASTRUCTURE_OVERHAUL_PART_1.md` as a frozen audit

The plan doc was a stable historical anchor. Once a phase was complete, its
checkbox flipped with a pointer at the resolving step(s); the doc was never
re-structured to chase new scope. This kept "what was the original plan?"
answerable in one glance.

### 1.6 Deterministic path conventions

Every step doc lived at exactly `STEPS_PART_1/<N>_<Name>.md`. Every
`INDEX.md` link was relative. Every cross-reference from C# comments used
`docs/testing/TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_1/<N>_<Name>.md`
(post-reorg). Copilot could construct any path algorithmically from a step
number — which it did, repeatedly, without guessing.

### 1.7 The step doc *sweep* rule

Every step concluded with: *"Re-check Blocked Items; sweep for obsoleted
docs."* This is what prevented the step-doc folder from becoming a graveyard
of stale plans. It pushed rot-reduction into the critical path.

### 1.8 Commit-per-step discipline

Because the workflow demanded a push at the end of each step, the
`git log --oneline` itself became a second step log. Cold-start Copilot
could cross-check `INDEX.md`'s claims against `git log` and reconcile drift
immediately.

---

## 2. What didn't work (or worked less well)

### 2.1 Silent table drift

Over 60 steps, the step-log table accumulated two genuine bugs caught only on
manual review:

- A **duplicate Step 51 row** (same step, two adjacent rows with slightly
  different phrasing).
- An **orphaned `| 52 |` stub row** where an edit left only the step number
  surviving.

Copilot doesn't naturally validate table structure unless asked. A cheap
structural check (`awk`/`grep` rule, or a pre-commit check) would have caught
both.

### 2.2 The "missing heading" bug

An earlier edit silently deleted the `### Recommended Next Steps (priority
order)` heading, leaving an internal anchor (`#recommended-next-steps-priority-order`)
pointing at nothing. The workflow section still referenced the heading by
name. Again — structural, not semantic.

### 2.3 Non-semantic step numbering

Steps were numbered in completion order, not by topic/phase. This is fine
for chronology but makes grouping-by-topic harder. "Show me all the CI-fix
steps" requires reading every row. A phase-tag column would have helped.

### 2.4 Coverage deltas hidden in prose

Coverage numbers appeared inside step-log row text (e.g. "49.93% →
62.76%"). They were never normalized into a column. A
`CoverageΔ (line)` column would have made the ratchet trivially queryable.

### 2.5 No machine-readable step index

`INDEX.md` is markdown-only. A tiny `steps.json` or YAML sidecar
(`{ number, title, phase, status, covDelta, docPath }[]`) would let Copilot
answer structural queries (`find all PARTIAL steps`, `plot coverage over
time`) without re-reading the table.

### 2.6 Blocked-item staleness

A blocked item (`Host.Lease` / Azurite) sat in the Blocked Items table across
all 60 steps with no automated re-check. A "last re-checked at step N"
timestamp on blocked rows would at least surface neglect.

### 2.7 Loose cross-references from source code

C# test files carried plain-text comments like
`// See docs/testing/TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_1/17_*.md`.
When the folder was reorganized, these had to be bulk-found and updated
manually. A shorter comment (`// See step 17`) plus a single convention
"step N = STEPS_PART_1/<N>_*.md" would make future renames a one-file fix.

---

## 3. Recommended improvements for Part 2

The goal is to keep everything that worked and add lightweight structure
around the edges.

### 3.1 Add a coverage-delta column to the step log

Before the "Doc" column, add `CovΔ (line)` holding either a delta
(`+3.18pp`), an `n/a` marker, or the absolute new number if the step
introduced a new tracked assembly.

### 3.2 Add a phase-tag column

Rename the current "Plan Phase" column to "Phase/Tag" and use short tags
(`6.7d`, `CI`, `DOC`, `FIX`) so a reader can filter topics at a glance.

### 3.3 Adopt step-doc front matter

Begin every `STEPS_PART_2/<N>_*.md` file with a YAML block:

```yaml
---
step: 7
title: Coverage — Api.Internal controllers
phase: 6.7d
status: complete          # complete | partial | blocked | reverted
coverageDelta:
  AccessManagement.Api.Internal:
    linePp: +3.40
verifiedTests: 34
touchedFiles: 12
---
```

Copilot can ingest this block in one glance and cross-check it against the
`INDEX.md` row. A trivial script can also detect drift between the two.

### 3.4 Maintain a machine-readable sidecar

`STEPS_PART_2/steps.json` (or `.yaml`) generated from the front-matter
blocks. Then:

- `INDEX.md` stays the human-friendly narrative.
- `steps.json` is the structured source of truth for any tooling.

A simple pre-commit or CI check can regenerate `steps.json` and fail if it
differs from committed version.

### 3.5 Pre-commit structural checks on `INDEX.md`

Tiny PowerShell/bash script invoked by `.husky/` or CI:

- Step log table rows have exactly 5 cells.
- Step numbers are strictly increasing and have no gaps.
- Every `Doc` link points to an existing file.
- Every anchor referenced elsewhere in the doc exists as a heading.

This would have caught the duplicate-row, orphan-stub, and missing-heading
bugs automatically.

### 3.6 Timestamp Blocked Items

Add a `Last re-checked` column to the Blocked Items table and enforce that
every N steps the oldest row is revisited. Prevents silent rot of the kind
`Host.Lease` showed in Part 1.

### 3.7 Normalize source-code cross-references

Replace verbose comments with a short convention:

```csharp
// See: overhaul step 17 (STEPS_PART_1)
// See: overhaul part-2 step 4
```

Plus a single repo-level note (e.g. in `docs/testing/README.md`) explaining
the mapping. One place to update when paths change.

### 3.8 Cap the step log at ~50 steps per part

Part 1 ended at 61 steps and the `INDEX.md` table is just barely readable.
Split proactively: when a part approaches 50 steps, close it and start the
next part. Part numbers are cheap.

### 3.9 Keep the "handoff prompt" verbatim — and version it

The ready-to-copy prompt embedded in `INDEX.md` should be updated in exactly
one place (there) and version-tagged in commit messages when it changes
(`docs(testing): update Part 2 handoff prompt to reference retrospective`).

### 3.10 Keep the *sweep step* rule — expand it

Part 1's "sweep for obsoleted docs" rule was gold. In Part 2, extend it to:
also sweep `TESTING_INFRASTRUCTURE_OVERHAUL_PART_2.md` for any now-obsolete
phase items, and mark them `~~struck through~~` with a pointer to the
resolving step.

---

## 4. TL;DR — the Part 2 opening move

Before the Part 2 **kickoff audit** step is started, apply the six lightweight
improvements that cost nothing and pay out immediately:

1. Add `CovΔ (line)` + `Phase/Tag` columns to the step-log table.
2. Adopt YAML front matter on every step doc.
3. Add the structural-check script (`INDEX.md` linter).
4. Add `Last re-checked` to Blocked Items.
5. Timestamp and version the handoff prompt.
6. Enforce "split at ~50 steps per part".

Everything else (sidecar JSON, source-comment convention) can land later
without breaking continuity.
