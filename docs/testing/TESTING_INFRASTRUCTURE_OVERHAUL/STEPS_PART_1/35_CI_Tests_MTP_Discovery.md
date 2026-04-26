# Step 35 — Route `dotnet test` to Microsoft Testing Platform so CI discovers xUnit v3 tests

## Goal

Fix CI failures where `dotnet test` reports **"No test is available … Make sure
that test discoverer & executors are registered"** for every xUnit v3 test
assembly in the repo, causing the CI test step to pass vacuously (0 tests run)
while hiding real regressions.

Seen in the `.github/workflows/tpl-vertical-ci.yml` run of every vertical after
the previous coverage-threshold-scoping fix (step 34) unblocked the coverage
stage, e.g.:

```text
No test is available in .../Altinn.AccessManagement.Api.Tests.dll. Make sure that
test discoverer & executors are registered and platform & framework version
settings are appropriate and try again.
```

## Root cause

xUnit v3 runs on **Microsoft Testing Platform (MTP)**, not on VSTest. The
default `dotnet test` command drives VSTest, which has no discoverer for xUnit
v3 assemblies — hence the "0 tests" result.

Opting into MTP requires **both** of the following on every test project:

1. `<TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>` —
   tells the .NET SDK that `dotnet test` should invoke the MTP entry point.
2. `<TargetFrameworks>` (plural) — empirically, when a v3 test project uses
   `<TargetFramework>` (singular), `dotnet test` silently falls back to VSTest
   even if (1) is set. Switching to `<TargetFrameworks>net9.0</TargetFrameworks>`
   makes the routing stick. (Confirmed locally: `pkg: PEP` and `pkg: ABAC` were
   working because their csprojs already set `<TargetFrameworks>`; the other
   verticals inherited the root `src/Directory.Build.props`'s singular
   `<TargetFramework>net9.0</TargetFramework>` and fell back to VSTest.)

A third issue was surfaced by enabling MTP globally:
`Altinn.AccessManagement.TestUtils` is a shared test-helper **library**
(`IsTestLibrary=true`) but was inheriting `IsTestProject=true` from
`src/apps/Altinn.AccessManagement/test/Directory.Build.props`. Under MTP,
`dotnet test` tried to run it and failed with "0 tests discovered".

## Changes

### 1. `src/Directory.Build.targets` — enable MTP routing for all v3 test projects

```xml
<TestingPlatformDotnetTestSupport
    Condition=" '$(IsTestProject)' == 'true' And '$(XUnitVersion)' == 'v3' ">true</TestingPlatformDotnetTestSupport>
```

Placed next to the existing `<OutputType>Exe</OutputType>` branch so the two
v3-specific settings live together.

### 2. All six `test/Directory.Build.props` files — force `<TargetFrameworks>` (plural)

Files updated (one per vertical):

- `src/apps/Altinn.AccessManagement/test/Directory.Build.props`
- `src/apps/Altinn.Authorization/test/Directory.Build.props`
- `src/libs/Altinn.Authorization.Host/test/Directory.Build.props`
- `src/libs/Altinn.Authorization.Integration/test/Directory.Build.props`
- `src/pkgs/Altinn.Authorization.ABAC/test/Directory.Build.props`
- `src/pkgs/Altinn.Authorization.PEP/test/Directory.Build.props`

Each now adds:

```xml
<TargetFramework></TargetFramework>
<TargetFrameworks>net9.0</TargetFrameworks>
```

Clearing the inherited singular property is required — leaving both set causes
the SDK to warn and prefer the singular form.

### 3. `Altinn.AccessManagement.TestUtils.csproj` — opt out of test execution

```xml
<IsTestProject>false</IsTestProject>
```

Overrides the value inherited from `test/Directory.Build.props`. Combined with
the existing `<IsTestLibrary>true</IsTestLibrary>`, this produces
`OutputType=Library` and `TestingPlatformDotnetTestSupport=False`, so
`dotnet test` skips the assembly entirely.

## Verification

Locally ran `dotnet test -c Release --no-build` at each vertical root:

| Vertical | Before | After |
|---|---|---|
| `pkg: Altinn.Authorization.PEP` | 92/92 (already worked: plural TFM) | 92/92 |
| `pkg: Altinn.Authorization.ABAC` | worked (plural TFM) | worked |
| `lib: Altinn.Authorization.Integration` | **"No test is available"** | 4 pass + 5 skip (9 discovered) |
| `lib: Altinn.Authorization.Host` | **"No test is available"** | tests discovered (Host.Lease.Tests still needs Azurite — pre-existing) |
| `app: Altinn.AccessManagement` | **"No test is available"** on all 6 DLLs | All 6 DLLs discovered; TestUtils correctly skipped |
| `app: Altinn.Authorization` | **"No test is available"** | 402 tests discovered, 401 pass, 1 flaky |

Also verified `dotnet msbuild -getProperty:IsTestProject,IsTestLibrary,OutputType,TestingPlatformDotnetTestSupport`
on `Altinn.AccessManagement.TestUtils`:

```text
IsTestProject=False
IsTestLibrary=True
OutputType=Library
TestingPlatformDotnetTestSupport=False
```

## Deferred / not in scope

- ~~**`PDP_Decision_ResourceRegistry_AccessListAuthorization_Json_PermitWithActionFilterMatch`**~~
  **Resolved in Step 51.** Root cause was a cache-hit bug in `ResourceRegistryMock.GetMembershipsForResourceForParty`:
  on a cache hit `TryGetValue` populates `memberships` but the enclosing `if (!TryGetValue(...))` block
  is skipped, causing a fall-through to `return Enumerable.Empty<>()` instead of returning the cached
  value. `DenyActionFilterNotMatching` (same `partyOrgNum` + same resource) always ran first, priming
  the cache; the next call for `PermitWithActionFilterMatch` got a cache hit, returned empty, and the
  PDP correctly (but wrongly from the test's perspective) issued Deny. Fixed by replacing the
  early-return-inside-if pattern with `return memberships ?? Enumerable.Empty<>()` after the block.
  `[Skip]` removed; all 21 tests in the class pass deterministically. See [51_Fix_6_7f_AccessListAuthorizationMockCacheBug.md](51_Fix_6_7f_AccessListAuthorizationMockCacheBug.md).
- Any real failures in `AccessManagement.ServiceOwner.Api.Tests` and
  `AccessManagement.Enduser.Api.Tests` that were previously masked by VSTest's
  "0 tests" result. These will now surface in CI and can be addressed
  individually.
- `Altinn.Authorization.Host.Lease.Tests` requires Azurite — pre-existing
  blocker, already tracked.
