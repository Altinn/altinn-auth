# Step 12 — AccessManagement Coverage Baseline with Podman (Phase 6.7a)

## Goal

Establish baseline coverage metrics for AccessManagement projects using Podman Desktop as the container runtime for Testcontainers.

## SUCCESS: Podman Desktop Works!

**The key was finding the right Testcontainers configuration for Podman on Windows.**

### Working Configuration

```powershell
$env:TESTCONTAINERS_DOCKER_SOCKET_OVERRIDE = '//./pipe/podman-machine-default'
$env:TESTCONTAINERS_RYUK_DISABLED = 'true'
$env:DOCKER_HOST = ''  # Must be cleared to avoid conflicts
```

**Why this works:**
- `TESTCONTAINERS_DOCKER_SOCKET_OVERRIDE` directly tells Testcontainers which named pipe to use
- `TESTCONTAINERS_RYUK_DISABLED` prevents Ryuk container cleanup issues with Podman
- Clearing `DOCKER_HOST` prevents conflicting autodiscovery

## Test Execution Results

**All 6 AccessManagement test projects ran successfully:**

| Project | Total | Passed | Skipped | Failed | Time (s) |
|---|---|---|---|---|---|
| AccessMgmt.Tests | 1,051 | 1,049 | 2 | 0 | 149.6 |
| AccessMgmt.Core.Tests | 6 | 6 | 0 | 0 | 38.0 |
| AccessMgmt.PersistenceEF.Tests | 41 | 34 | 7 | 0 | 2.5 |
| AccessManagement.Api.Tests | 2 | 2 | 0 | 0 | 0.9 |
| AccessManagement.Enduser.Api.Tests | 185 | 177 | 8 | 0 | 100.9 |
| AccessManagement.ServiceOwner.Api.Tests | 11 | 11 | 0 | 0 | 37.8 |
| **TOTAL** | **1,296** | **1,279** | **17** | **0** | **329.7** |

**Success rate: 98.7%** (0 failures, 17 skips are expected for Azure Storage-dependent tests)

## Coverage Baseline

### Core Libraries

| Assembly | Line% | Branch% | Assessment |
|---|---|---|---|
| **Altinn.AccessMgmt.PersistenceEF** | **98.59** | **90.78** | ✅ Excellent |
| **AccessManagement.Api.Maskinporten** | **80.36** | **80.00** | ✅ Good |
| **AccessManagement.Api.Enterprise** | **66.39** | **56.52** | ✅ Good |
| **AccessManagement.Core** | **63.43** | **61.49** | ✅ Above 60% |
| **AccessManagement** (main app) | **58.19** | **60.93** | ⚠️ Near 60% |
| AccessManagement.Integration | 47.57 | 43.75 | ⚠️ Below 50% |
| AccessManagement.Api.Internal | 46.74 | 46.20 | ⚠️ Below 50% |
| AccessManagement.Persistence | 44.94 | 30.23 | ❌ Low |
| AccessMgmt.Persistence | 32.51 | 9.42 | ❌ Low |
| AccessMgmt.Core | 17.31 | 12.00 | ❌ Very low |
| AccessManagement.Api.Metadata | 16.59 | 13.33 | ❌ Very low |
| AccessMgmt.Persistence.Core | 8.78 | 3.21 | ❌ Very low |
| AccessManagement.Api.Enduser | 1.19 | 0.15 | ❌ Critically low |
| AccessManagement.Api.ServiceOwner | 0.00 | 0.00 | ❌ No coverage |

### API Endpoints

Multiple instances of assemblies show different coverage (different endpoints/API versions):
- **AccessManagement** ranges from 16.3% to 58.19%
- **AccessMgmt.PersistenceEF** ranges from 97.69% to 98.59%

### Coverage Gaps Identified

1. **Critical:** AccessManagement.Api.ServiceOwner (0% coverage)
2. **High:** AccessManagement.Api.Enduser (1.19% — only 2 descriptor tests exist)
3. **High:** AccessMgmt.Core persistence variants (8-17% coverage)
4. **Medium:** AccessManagement.Persistence layers (32-45% coverage)

## What This Unblocks

✅ **Phase 2.2–2.3** — AccessMgmt.Tests WAF consolidation (complex, now actionable)
✅ **Phase 3.2–3.4** — Mock deduplication implementation
✅ **Phase 6.1** — AccessManagement.Core coverage improvements
✅ **Phase 6.2** — Persistence layer test additions
✅ **Phase 6.3** — API endpoint integration tests

## Files Changed

### New

- **`docs/testing/run-accessmanagement-coverage.ps1`** — Helper script that sets Podman environment variables and runs coverage for all 6 AccessManagement test projects

### Verified Working

All existing test infrastructure works with Podman:
- `EFPostgresFactory` (postgres:16.1-alpine container)
- `ApiFixture` (Testcontainers integration)
- `PostgresFixture` (legacy, still works)

## Environment

- **Container Runtime:** Podman Desktop 5.2.5 (WSL backend)
- **Podman Machine:** `podman-machine-default` (running)
- **Testcontainers:** v3.10.0+ (from NuGet)
- **PostgreSQL Image:** postgres:16.1-alpine (auto-pulled by Testcontainers)

## Verification

- ✅ All 1,279 tests pass (17 expected skips)
- ✅ Coverage data collected for 14 AccessManagement assemblies
- ✅ Podman Desktop confirmed as reliable alternative to Docker Desktop
- ✅ No Docker Desktop installation required

## Initial Investigation (for reference)

**First attempt failed with `DOCKER_HOST` variable incompatibility:**
- Podman sets: `npipe:////pipe/podman-machine-default`  
- Testcontainers expected: `npipe:////./pipe/...`
- Error: "not a valid npipe URI"

**Solution discovered:** Use `TESTCONTAINERS_DOCKER_SOCKET_OVERRIDE` instead of `DOCKER_HOST`.

---

**Note:** For current recommendations and next steps, see [INDEX.md](INDEX.md).

**Full coverage output:** `podman-coverage-accessmgmt.txt`
