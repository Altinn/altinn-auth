---
step: 3
title: Delete empty Altinn.Authorization.ABAC.Tests project
phase: A
status: complete
linkedIssues:
  feature: 2946
  task: 2947
coverageDelta:
  # No production code touched; ABAC's indirect coverage via
  # Altinn.Authorization.Tests is unchanged.
  Altinn.Authorization.ABAC: { line: 0.0, branch: 0.0, note: indirect_coverage_unchanged }
verifiedTests: 0
touchedFiles: 5
auditIds: [C1']
---

# Step 3 — Delete empty Altinn.Authorization.ABAC.Tests project

## Goal

Resolve audit finding **C1'**: `Altinn.Authorization.ABAC.Tests`
contained no `[Fact]` / `[Theory]` methods (only auto-generated
files), so the test runner discovered zero tests and the project's
cobertura output was a 181-byte stub. ABAC's 63 % line coverage was
already produced indirectly by `Altinn.Authorization.Tests` exercising
PEP → ABAC paths — the empty project was misleading dead weight.

Decision per audit: **delete** rather than populate. Future direct
ABAC unit tests can recreate the project from scratch (the parent
`test/Directory.Build.props` was 213 bytes of standard wiring that
is trivial to regenerate, and `git log` preserves the original).

## What changed

### Removed from solution files

```
dotnet sln Altinn.Authorization.sln remove \
  src/pkgs/Altinn.Authorization.ABAC/test/Altinn.Authorization.ABAC.Tests/Altinn.Authorization.ABAC.Tests.csproj
dotnet sln src/pkgs/Altinn.Authorization.ABAC/Altinn.Authorization.ABAC.sln remove \
  src/pkgs/Altinn.Authorization.ABAC/test/Altinn.Authorization.ABAC.Tests/Altinn.Authorization.ABAC.Tests.csproj
```

Both the root and per-package solution files updated.

### Deleted files

- `src/pkgs/Altinn.Authorization.ABAC/test/Altinn.Authorization.ABAC.Tests/Altinn.Authorization.ABAC.Tests.csproj`
  (190 bytes — minimal csproj inheriting all wiring from
  `test/Directory.Build.props`).
- `src/pkgs/Altinn.Authorization.ABAC/test/Directory.Build.props`
  (213 bytes — orphan once its only consumer is gone).
- `src/pkgs/Altinn.Authorization.ABAC/test/` — directory removed
  (only contained the deleted project and the gitignored bin/obj
  artifacts).

### Documentation

- [`docs/testing/TEST_PROJECTS.md`](../../TEST_PROJECTS.md) `### pkg: ABAC`
  table replaced with a short note explaining ABAC is exercised
  indirectly via `Altinn.Authorization.Tests` (~63 % line / 61 %
  branch), plus a how-to for recreating the project if a direct
  unit-test suite is ever wanted.
- Audit doc [`TESTING_INFRASTRUCTURE_OVERHAUL_PART_2.md`](../TESTING_INFRASTRUCTURE_OVERHAUL_PART_2.md)
  §2 — C1' marked resolved; §4 Phase A — A.1 marked done.
- Step log row + Recommended Next Steps refresh in
  [`INDEX.md`](INDEX.md).

### Solution-file references

Only the two `.sln` files referenced the project. CI workflows
discover test projects dynamically via `Get-ChildItem -Filter
'*.Tests.csproj' -Recurse` (in
[`eng/testing/run-coverage.ps1`](../../../../eng/testing/run-coverage.ps1)),
so no CI config required updating.

## Verification

### Build clean

```
dotnet build src/pkgs/Altinn.Authorization.ABAC/Altinn.Authorization.ABAC.sln \
  --configuration Release --verbosity minimal
```

Result: 0 warnings, 0 errors. `Altinn.Authorization.ABAC` builds for
both `net8.0` and `net9.0` targets. The deleted test project was the
only thing in `test/`, and nothing in `src/` referenced it (one-way
dependency: tests reference production, not the other way).

### Test-project count

11 → **10**. Step 1's audit table in
[`PART_2.md` §1.1](../TESTING_INFRASTRUCTURE_OVERHAUL_PART_2.md#11-test-project-inventory)
remains as the historical Step 1 snapshot (the row stays so the audit
remains a faithful point-in-time record); the C1' resolution marker
in §2 documents that the project is deleted as of Step 3.

### CI references

```
grep -rni 'ABAC\.Tests\|abac.tests' .github/
```

Returned nothing — confirms CI does not hardcode the project anywhere.

### Coverage impact

None. `Altinn.Authorization.ABAC`'s 63 % line / 61 % branch coverage
came entirely from `Altinn.Authorization.Tests` exercising PEP → ABAC
paths during XACML decision tests. Removing the empty test project
does not change which lines are hit. The centrally-enforced 60 %
`Altinn.Authorization.ABAC` threshold in
[`eng/testing/coverage-thresholds.json`](../../../../eng/testing/coverage-thresholds.json)
continues to gate the indirect coverage.

## Deferred / follow-up

- None for C1' itself.
- Test-project table refresh in `PART_2.md` §1.1 (11 → 10) bundled
  with the deferred §1.4 baseline refresh at T1 closing — single
  sweep after the remaining Phase A sub-items.

## Blocked-items sweep

No items unblocked. Refresh `Last re-checked = step 3` on the three
Blocked Items rows in [`INDEX.md`](INDEX.md#blocked-items).

## Obsolete-docs sweep

None. No prior step doc covered this work.
