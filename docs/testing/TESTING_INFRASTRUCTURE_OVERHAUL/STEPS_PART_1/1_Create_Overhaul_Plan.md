# Create Overhaul Plan

## Status: ✅ Complete

## Work Completed

- Audited all 11 test projects in the solution.
- Documented current state: xUnit versions, target frameworks, fixture patterns, mock duplication, database strategies.
- Identified 5 critical, 8 moderate, and 3 minor issues.
- Documented best practices already in place.
- Created a 6-phase improvement plan with dependencies and execution order.
- Output: [`docs/testing/TESTING_INFRASTRUCTURE_OVERHAUL.md`](../TESTING_INFRASTRUCTURE_OVERHAUL_PART_1.md)

## Key Findings Summary

| Category | Count |
|---|---|
| Test projects on xUnit v2 | 5 |
| Test projects on xUnit v3 | 6 |
| Projects on net8.0 (should be net9.0) | 1 (`ABAC.Tests`) |
| Duplicate WebApplicationFactory implementations | 4 |
| Duplicate mock classes across projects | ~10 interfaces mocked 2-3 times |

## Next Task

**Phase 1, Step 1.1 — Migrate all test projects to xUnit v3** by setting `<XUnitVersion>v3</XUnitVersion>` in the five `test/Directory.Build.props` files that currently default to v2. Then fix any resulting compilation errors from xUnit v2 → v3 API changes.

See: [TESTING_INFRASTRUCTURE_OVERHAUL.md — Phase 1](../TESTING_INFRASTRUCTURE_OVERHAUL_PART_1.md#phase-1-foundation--unify-xunit-version--target-framework)
