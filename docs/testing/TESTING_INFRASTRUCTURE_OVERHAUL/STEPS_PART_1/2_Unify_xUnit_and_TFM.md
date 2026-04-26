# Unify xUnit Version & Target Framework

## Status: ✅ Complete

## Objective
Migrate all 11 test projects from xUnit v2 to xUnit v3, and ensure all test projects target net9.0.

## Changes Made

### 1.1 — xUnit v3 Migration

**Directory.Build.props (5 files):**
- `src/apps/Altinn.AccessManagement/test/Directory.Build.props` — added `<XUnitVersion>v3</XUnitVersion>`
- `src/apps/Altinn.Authorization/test/Directory.Build.props` — added `<XUnitVersion>v3</XUnitVersion>`
- `src/libs/Altinn.Authorization.Host/test/Directory.Build.props` — added `<XUnitVersion>v3</XUnitVersion>`
- `src/pkgs/Altinn.Authorization.ABAC/test/Directory.Build.props` — added `<XUnitVersion>v3</XUnitVersion>`
- `src/pkgs/Altinn.Authorization.PEP/test/Directory.Build.props` — added `<XUnitVersion>v3</XUnitVersion>`

**Directory.Packages.props (2 files):**
- `src/pkgs/Directory.Packages.props` — added `xunit.v3`, `xunit.v3.assert`, `xunit.v3.extensibility.core`
- `src/pkgs/Altinn.Authorization.ABAC/Directory.Packages.props` — added `xunit.v3`, `xunit.v3.assert`, `xunit.v3.extensibility.core`

**Redundant XUnitVersion removed from 5 csproj files** that now inherit from Directory.Build.props:
- Altinn.AccessManagement.Enduser.Api.Tests.csproj
- Altinn.AccessManagement.Api.Tests.csproj
- Altinn.AccessManagement.ServiceOwner.Api.Tests.csproj
- Altinn.AccessMgmt.Core.Tests.csproj
- Altinn.AccessMgmt.PersistenceEF.Tests.csproj

### 1.2 — xUnit v3 Breaking Changes Fixed

| Issue | Files Affected | Fix |
|-------|---------------|-----|
| `IAsyncLifetime` returns `ValueTask` (was `Task`) | `PostgresFixture.cs`, `WebApplicationFixture.cs` | Changed return types to `ValueTask` / `ValueTask.CompletedTask` |
| `Xunit.Abstractions` namespace removed | `ConsentControllerTestBFF.cs`, `ConsentControllerTest.cs` | Removed `using Xunit.Abstractions;` |
| `TheoryData<T>` collection initializer ambiguity (CS0121) | `V2ResourceControllerTest.cs`, `V2MaskinportenSchemaControllerTest.cs`, `V2RightsInternalControllerTest.cs` | Changed from collection expression `[new(...)]` to explicit `data.Add(new TypeName(...))` |
| Unused `Microsoft.VisualStudio.TestPlatform` reference | `Altinn2ConsentClientMock.cs` | Removed unused `using` |

### 1.3 — Target Framework (net9.0)

**Test csproj overrides** (pkgs test projects inherited `net8.0;net9.0` from `src/pkgs/Directory.Build.props`):
- `Altinn.Authorization.ABAC.Tests.csproj` — added `<TargetFrameworks>net9.0</TargetFrameworks>`
- `Altinn.Authorization.PEP.Tests.csproj` — added `<TargetFrameworks>net9.0</TargetFrameworks>`

All other test projects already targeted net9.0 via `src/Directory.Build.props`.

### 1.4 — Build System (Directory.Build.targets)

- `Microsoft.NET.Test.Sdk` — kept for all test projects (required by `dotnet test`)
- `xunit.runner.visualstudio` — now v2-only (xUnit v3 bundles its own VSTest adapter)
- `OutputType=Exe` — set for v3 test projects (required by xUnit v3 self-hosting)

## Verification

- ✅ Build succeeds
- ✅ PEP tests: 40/40 passed
- ✅ Host.Lease tests: 2/2 discovered (skipped — requires storage account)
- ✅ Core tests: 6/6 discovered
- ✅ ABAC tests: project has no test files (empty test project)
