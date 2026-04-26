# Step 27: FluentAssertions Guidelines

**Phase:** 4.2b — Test Patterns & Naming
**Status:** ✅ Complete
**Date:** 2025-01-27

---

## Goal

Publish usage guidelines and recommended patterns for FluentAssertions so the
team can adopt it consistently in new and modified tests, following the
package installation in Step 14.

## Changes

### Added `docs/testing/FLUENT_ASSERTIONS_GUIDELINES.md`

Canonical guidance document covering:

- **Adoption policy** — use in new tests, convert on touch, no bulk rewrites.
- **Pattern cheatsheet** — xUnit → FluentAssertions for:
  equality/nullness, booleans with `because`, strings, numeric ranges,
  dates/times, collections (ordered/unordered/predicate-based), complex
  object comparison via `BeEquivalentTo`, synchronous/async exceptions,
  and HTTP responses.
- **When to keep xUnit** — `Assert.Fail`, trivial theory guards, and APIs
  without a FluentAssertions equivalent (`Assert.Raises` etc.). Plus the
  rule "don't mix styles on the same value".
- **Relationship to the two `AssertionUtil` classes** — freeze them (no new
  helpers), migrate call sites on touch, retirement tracked as Phase 4.2d.
- **Failure message quality** — when to add `because` clauses.
- Cross-links to Steps 13 and 14 and the naming convention doc.

### Updated `docs/testing/steps/INDEX.md`

- Added Step 27 row to the step log.
- Removed the completed Phase 4.2b item from
  `### Recommended Next Steps (priority order)` and renumbered the remaining
  items.

## Verification

Documentation-only change. No code modified, no tests impacted.

- `docs/testing/FLUENT_ASSERTIONS_GUIDELINES.md` created.
- Markdown renders and internal links resolve (`steps/...`, `../`).
- No changes to `src/`, so build and test coverage are unaffected.

## Deferred

- **Phase 4.2c — Pilot usage** will happen organically during Phase 6.7b–d
  coverage work; no separate step required.
- **Phase 4.2d — Retire `AssertionUtil`** stays deferred until enough call
  sites migrate.

## Recommendation for Next Step

Pick priority **1** from the refreshed list in `INDEX.md`:

> **Phase 5.1b — CI coverage thresholds for AccessManagement**

This can stay in the current chat — it's a focused edit to the coverage
threshold config plus a new step doc, similar in scope to Step 8
([CI_Coverage_Threshold.md](CI_Coverage_Threshold.md)).
