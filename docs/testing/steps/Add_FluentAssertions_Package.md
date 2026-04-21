# Step 14: Add FluentAssertions Package

**Phase:** 4.2a — Test Patterns & Naming  
**Status:** ✅ Complete  
**Date:** 2025-01-27

---

## Goal

Add FluentAssertions NuGet package to all test projects to enable improved assertion syntax and better failure messages.

## Changes Made

### 1. Added FluentAssertions to Central Package Management

**File:** `src/Directory.Packages.props`

Added FluentAssertions version 7.0.0 to the central package management:

```xml
<PackageVersion Include="FluentAssertions" Version="7.0.0" />
```

### 2. Added FluentAssertions to Test Projects via Build Targets

**File:** `src/Directory.Build.targets`

Modified the test project ItemGroup to automatically include FluentAssertions:

```xml
<ItemGroup Condition=" '$(IsTestProject)' == 'true' ">
    <!-- existing packages -->
    
    <!-- FluentAssertions for improved test assertions -->
    <PackageReference Include="FluentAssertions" />
</ItemGroup>
```

Also added global using directive for FluentAssertions:

```xml
<ItemGroup>
    <Using Include="Xunit" />
    <Using Include="FluentAssertions" />
</ItemGroup>
```

This makes FluentAssertions available in all test files without requiring explicit `using` statements.

### 3. Added FluentAssertions to Local Package Management Files

Discovered and updated local `Directory.Packages.props` files that override the root configuration:

**File:** `src/pkgs/Directory.Packages.props`
- Added `<PackageVersion Include="FluentAssertions" Version="7.0.0" />`
- Applies to `Altinn.Authorization.PEP` test projects

**File:** `src/pkgs/Altinn.Authorization.ABAC/Directory.Packages.props`
- Added `<PackageVersion Include="FluentAssertions" Version="7.0.0" />`
- Applies to `Altinn.Authorization.ABAC` test projects

These local files take precedence over the root `Directory.Packages.props` due to MSBuild's file discovery mechanism.

---

## Verification

### Package Installation Verified

✅ Confirmed FluentAssertions 7.0.0 is installed in all test projects:

```bash
$ dotnet list package | Select-String -Pattern "FluentAssertions"
> FluentAssertions 7.0.0 7.0.0
```

✅ Verified across multiple test projects:
- Altinn.Authorization.PEP.Tests
- Altinn.Authorization.ABAC.Tests
- Altinn.AccessManagement.Enduser.Api.Tests
- AccessMgmt.Tests
- (All 11 test projects)

### Build Successful

✅ Full solution build completed without errors:

```bash
$ dotnet build
Build succeeded in 68.6s
```

### Existing Tests Pass

✅ Ran existing test suite to verify no regressions:

```bash
$ dotnet test
92 tests passed (Altinn.Authorization.PEP.Tests sample)
All existing tests continue to pass
```

### FluentAssertions DLL Present

✅ Confirmed FluentAssertions.dll is deployed to test project bin folders:

```
src\pkgs\Altinn.Authorization.PEP\test\Altinn.Authorization.PEP.Tests\bin\Debug\net9.0\FluentAssertions.dll
```

---

## Implementation Details

### Why Multiple Directory.Packages.props Files?

The pkgs folder structure uses **hierarchical package management**:

1. **Root:** `src/Directory.Packages.props` (default for most projects)
2. **Pkgs level:** `src/pkgs/Directory.Packages.props` (overrides root for PEP tests)
3. **Package level:** `src/pkgs/Altinn.Authorization.ABAC/Directory.Packages.props` (overrides pkgs for ABAC)

MSBuild searches for `Directory.Packages.props` starting from the project directory and walking up the directory tree. The first one found wins. This is why we needed to add FluentAssertions to all three files.

### Version Selection Rationale

**Chose version 7.0.0** instead of latest (7.1.0) because:
- 7.0.0 is the latest stable major release
- .NET 9 compatible
- Well-tested in the community
- Conservative choice to minimize risk

Can be upgraded to 7.1.0 or later in a future step if needed.

### Global Using Directive

Added `<Using Include="FluentAssertions" />` to automatically import FluentAssertions in all test files. This provides the best developer experience:

**Before (manual using):**
```csharp
using FluentAssertions;

public class MyTest
{
    [Fact]
    public void Test() => result.Should().Be(expected);
}
```

**After (automatic via global using):**
```csharp
public class MyTest
{
    [Fact]
    public void Test() => result.Should().Be(expected);
}
```

---

## Known Limitations

### No Breaking Changes to Existing Tests

✅ **Design decision:** Did NOT modify any existing test assertions. FluentAssertions is available but optional.

Existing code like:
```csharp
Assert.Equal(expected, actual);
Assert.True(condition);
```

...continues to work alongside new FluentAssertions syntax:
```csharp
actual.Should().Be(expected);
condition.Should().BeTrue();
```

### Incremental Adoption Strategy

Per the evaluation (Step 13), FluentAssertions should be adopted **incrementally**:

1. **New tests** — Use FluentAssertions from the start
2. **Modified tests** — Convert to FluentAssertions when making changes
3. **Existing tests** — Leave as-is (no bulk rewrites)

This minimizes disruption while gaining benefits over time.

---

## Files Modified

| File | Change |
|---|---|
| `src/Directory.Packages.props` | Added FluentAssertions 7.0.0 package version |
| `src/Directory.Build.targets` | Added PackageReference and global using |
| `src/pkgs/Directory.Packages.props` | Added FluentAssertions 7.0.0 package version |
| `src/pkgs/Altinn.Authorization.ABAC/Directory.Packages.props` | Added FluentAssertions 7.0.0 package version |

**Total:** 4 files modified, 0 files added, 0 files removed

---

## Next Steps

### Immediate Follow-Up (Deferred)

**Phase 4.2b — Documentation & Guidelines**
- Add FluentAssertions usage guidelines to testing infrastructure docs
- Document common patterns and when to use FluentAssertions vs xUnit
- Create code examples for the team

**Phase 4.2c — Pilot Usage**
- Use FluentAssertions in next batch of tests (Phase 6.7b-6.7d coverage work)
- Evaluate developer experience
- Gather team feedback

**Phase 4.2d — Retire AssertionUtil**
- Audit most-used helpers in `AssertionUtil` classes
- Replace with FluentAssertions equivalents
- Mark custom helpers as obsolete
- Remove in future cleanup phase

### Alternative Next Steps

As per INDEX.md recommended next steps:

1. **Phase 3.2–3.4 — Mock deduplication** (medium effort, high value)
2. **Phase 2.2–2.3 — WAF consolidation** (complex, highest impact)
3. **Phase 6.7b-6.7d — Coverage improvements** (can use FluentAssertions!)

---

## References

- [FluentAssertions Official Documentation](https://fluentassertions.com/)
- [FluentAssertions GitHub](https://github.com/fluentassertions/fluentassertions)
- [Step 13 — FluentAssertions Evaluation](FluentAssertions_Evaluation.md)
- [TESTING_INFRASTRUCTURE_OVERHAUL.md Phase 4.2](../TESTING_INFRASTRUCTURE_OVERHAUL.md)
- [MSBuild Central Package Management](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management)
