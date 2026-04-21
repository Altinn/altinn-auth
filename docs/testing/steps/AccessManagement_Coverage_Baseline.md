# Step 12 — AccessManagement Coverage Baseline Attempt (Phase 6.7a)

> ⚠️ **OBSOLETE — superseded by [AccessManagement_Coverage_Baseline_Success.md](AccessManagement_Coverage_Baseline_Success.md).**
> This doc records the initial Podman/Testcontainers troubleshooting attempt.
> The working configuration and the actual measured baseline live in the `_Success` doc,
> which is the one linked from [INDEX.md](INDEX.md). Kept for troubleshooting history only.

## Goal

Run baseline coverage collection for AccessManagement projects to:
1. Verify Docker/Podman works with Testcontainers
2. Establish baseline coverage metrics for AccessManagement assemblies
3. Identify which tests require database infrastructure

## Environment

- **Docker:** Not installed (command not found)
- **Podman:** 5.2.5 (installed via Podman Desktop)
- **Podman Machine:** `podman-machine-default` (WSL, running)
- **Testcontainers:** Integrated via NuGet packages

## What We Discovered

### Test Project Architecture

AccessManagement test suite consists of 6 projects:

| Project | Type | Test Count | Framework | Uses DB |
|---|---|---|---|---|
| `AccessMgmt.Tests` | xUnit v3 exe | 540 | net9.0 | ✅ (PostgresFixture) |
| `Altinn.AccessManagement.Api.Tests` | xUnit v3 exe | 2 | net9.0 | ❌ |
| `Altinn.AccessManagement.Enduser.Api.Tests` | xUnit v3 exe | 185 | net9.0 | ✅ (ApiFixture → EFPostgresFactory) |
| `Altinn.AccessManagement.ServiceOwner.Api.Tests` | xUnit v3 exe | 11 | net9.0 | ✅ (ApiFixture → EFPostgresFactory) |
| `Altinn.AccessMgmt.Core.Tests` | xUnit v3 exe | 6 | net9.0 | ✅ (PostgresFixture) |
| `Altinn.AccessMgmt.PersistenceEF.Tests` | xUnit v3 exe | 41 | net9.0 | ✅ (EFPostgresFactory) |

**Total:** 785 tests across AccessManagement suite

### Testcontainers + Podman Compatibility Issue

**Problem:** Testcontainers for .NET does not work out-of-the-box with Podman on Windows.

**Root Cause:** `DOCKER_HOST` environment variable format incompatibility:
- Podman uses: `npipe:////pipe/podman-machine-default`
- Testcontainers expects: `npipe:////./pipe/...`  
- Error: `npipe:////pipe/podman-machine-default is not a valid npipe URI`

**Attempted Workarounds:**

1. **Set `TESTCONTAINERS_RYUK_DISABLED=true`** — Ryuk is a cleanup container that sometimes conflicts with Podman. Setting this allowed `PersistenceEF.Tests` to run successfully (34 passed, 7 skipped).

2. **Fix `DOCKER_HOST` format** — Changed to `npipe:////./pipe/podman-machine-default`, but tests still failed with connection errors.

3. **SSH connection** — Podman exposes SSH endpoints (`ssh://user@127.0.0.1:51872/...`) but Testcontainers doesn't support SSH-based Docker endpoints.

### Partial Success: PersistenceEF.Tests

**One project worked:**
```
Altinn.AccessMgmt.PersistenceEF.Tests
  Total: 41, Errors: 0, Failed: 0, Skipped: 7
  Coverage: Altinn.AccessMgmt.PersistenceEF 3.73% line, 1.54% branch
```

This suggests Testcontainers **can** work with Podman under specific conditions, but the configuration is fragile.

### Test Execution Results (with Testcontainers errors)

Despite connection issues, the coverage script collected partial results:

| Assembly | Line% | Branch% | Notes |
|---|---|---|---|
| **AccessManagement.Core** | 59.29 | 56.65 | ✅ Near threshold |
| **AccessManagement** (main app) | 58.11 | 60.73 | ✅ Controllers tested |
| AccessManagement.Persistence | 9.11 | 12.50 | ❌ Low coverage |
| AccessMgmt.PersistenceEF | 7.19 | 0.59 | ❌ Low coverage |
| AccessMgmt.Persistence | 32.27 | 9.42 | ❌ Below threshold |
| AccessMgmt.Core | 5.71 | 2.34 | ❌ Confusingly low |
| AccessManagement.Api.Enduser | 1.19 | 0.15 | ❌ Tests failed |
| AccessManagement.Api.Internal | 1.31 | 0.54 | ❌ Low coverage |
| AccessManagement.Integration | 47.57 | 43.75 | ⚠️ Near threshold |
| AccessManagement.Api.Enterprise | 0 | 0 | ❌ No coverage |
| AccessManagement.Api.Maskinporten | 0 | 0 | ❌ No coverage |
| AccessManagement.Api.Metadata | 0 | 0 | ❌ No coverage |
| AccessManagement.Api.ServiceOwner | 0 | 0 | ❌ Tests failed |
| AccessMgmt.Persistence.Core | 8.78 | 3.21 | ❌ Low coverage |

**Notes:**
- Multiple assemblies show 0% because their tests failed to initialize (Testcontainers issue)
- `AccessManagement.Core` at **59.29%** is encouraging but needs threshold defined
- Persistence layer coverage is critically low across all variants

### Test Failures Summary

```
AccessMgmt.Tests                     540 total, 147 failed (27% failure rate)
Altinn.AccessManagement.Api.Tests      2 total,   0 failed (✅ all passed)
Enduser.Api.Tests                    185 total, 185 failed (100% failure)
ServiceOwner.Api.Tests                11 total,  11 failed (100% failure)
AccessMgmt.Core.Tests                  6 total,   6 failed (100% failure)
AccessMgmt.PersistenceEF.Tests        41 total,   0 failed, 7 skipped (✅)
```

**Failure Pattern:** All failures are `PostgresFixture` / `EFPostgresFactory` initialization errors (Testcontainers can't connect to Podman).

## Verification

- ✅ Podman 5.2.5 installed and running
- ✅ Coverage script executes all test projects
- ✅ One test project runs successfully (`PersistenceEF.Tests`)
- ❌ Testcontainers + Podman integration unreliable on Windows
- ❌ Cannot establish reliable baseline for DB-dependent tests
- ⚠️ Partial coverage data collected (but suspect due to test failures)

## Recommended Path Forward

### Option 1: Install Docker Desktop (Recommended)

**Testcontainers officially supports Docker Desktop**, not Podman. While Podman claims Docker API compatibility, the Windows named pipe implementation has subtle differences that break Testcontainers' auto-detection.

**Action:** Install Docker Desktop for Windows and re-run coverage collection.

### Option 2: Skip DB-Dependent Coverage for Now

**What works without Testcontainers:**
- Authorization.Tests (✅ all 402 passed)
- Authorization.PEP.Tests (✅ all 92 passed)
- Authorization.ABAC.Tests (✅ all tests pass)
- AccessManagement.Api.Tests (✅ 2/2 passed — no DB needed)

**What requires fixing:**
- All AccessManagement integration tests (6 projects, ~783 tests)

**Action:** Defer AccessManagement coverage baseline to Step 13+ after Docker Desktop installation, proceed with other work (FluentAssertions, Collection standardization, etc.).

### Option 3: Linux CI Environment

Run AccessManagement coverage collection in a Linux CI environment where Podman/Docker integration is better supported. This would give accurate baseline metrics but wouldn't solve local development testing.

## Files Changed

**None** — this was a reconnaissance step.

---

**Note:** This was the initial failed attempt. See [AccessManagement_Coverage_Baseline_Success.md](AccessManagement_Coverage_Baseline_Success.md) for the successful resolution using Podman Desktop with proper Testcontainers configuration. For current recommendations and next steps, see [INDEX.md](INDEX.md).

## Coverage Script Output

Full output saved to `coverage-output.txt` (785 total tests attempted).
