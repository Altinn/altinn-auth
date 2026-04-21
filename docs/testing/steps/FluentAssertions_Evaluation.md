# Step 13: FluentAssertions Evaluation

**Phase:** 4.2 — Test Patterns & Naming  
**Status:** ✅ Complete  
**Date:** 2025-01-27

---

## Goal

Evaluate whether to adopt **FluentAssertions** (or an alternative like Shouldly) to improve test readability and assertion clarity across all test projects.

## Current State Analysis

### Assertion Patterns Observed

The codebase uses three main assertion approaches:

1. **Basic xUnit Assertions** (most common)
   ```csharp
   Assert.Equal(HttpStatusCode.OK, response.StatusCode);
   Assert.True(context.HasSucceeded);
   Assert.False(context.HasFailed);
   Assert.NotNull(actual);
   ```

2. **Custom AssertionUtil Classes** (two separate implementations)
   - `src/apps/Altinn.Authorization/test/Altinn.Authorization.Tests/Util/AssertionUtil.cs`
   - `src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/Utils/AssertionUtil.cs`
   
   These provide custom comparison methods for complex domain objects:
   ```csharp
   AssertionUtil.AssertCollections<T>(expected, actual, assertMethod);
   AssertionUtil.AssertEqual(XacmlContextResponse expected, XacmlContextResponse actual);
   AssertionUtil.AssertPagination<T>(expected, actual, assertMethod);
   ```

3. **Inline Property Comparisons**
   ```csharp
   Assert.Equal(expected.Count, actual.Count);
   Assert.Equal(expected.Results.First(), actual.Results.First());
   ```

### Issues with Current Approach

| Issue | Impact | Examples |
|---|---|---|
| **Poor failure messages** | When `Assert.Equal(expected, actual)` fails, xUnit only shows object references, not which properties differ | Common in complex object comparisons |
| **Verbose assertion chains** | Multiple assertions needed for single concepts | `Assert.True(x > 0); Assert.True(x < 100);` instead of range check |
| **Two separate AssertionUtil classes** | Code duplication, inconsistent helper methods between projects | Both Authorization.Tests and AccessMgmt.Tests have their own |
| **Difficult collection assertions** | Manual iteration and index tracking | See `AssertCollections<T>` implementations |
| **No fluent API** | Assertions read backwards from natural language | `Assert.Equal(expected, actual)` vs `actual.Should().Be(expected)` |
| **Limited assertion expressiveness** | No built-in support for: string contains, collection equivalence, exception properties, date ranges, etc. | Requires custom helper methods |

### Test Count Overview

Based on test project structure:

| Project | Framework | Estimated Tests |
|---|---|---|
| AccessMgmt.Tests | net9.0, xUnit v3 | ~500+ |
| Altinn.AccessManagement.Enduser.Api.Tests | net9.0, xUnit v3 | ~200+ |
| Altinn.AccessManagement.ServiceOwner.Api.Tests | net9.0, xUnit v3 | ~50+ |
| Altinn.AccessManagement.Api.Tests | net9.0, xUnit v3 | ~100+ |
| Altinn.Authorization.Tests | net9.0, xUnit v3 | ~300+ |
| Altinn.Authorization.PEP.Tests | net9.0, xUnit v3 | ~100+ |
| Altinn.Authorization.ABAC.Tests | net9.0, xUnit v3 | ~50+ |
| **Total** | | **~1,300+ tests** |

## FluentAssertions Evaluation

### What is FluentAssertions?

[FluentAssertions](https://fluentassertions.com/) is a popular .NET assertion library that provides a fluent API for writing more readable and expressive test assertions.

**Current Version:** 7.1.0 (latest stable, compatible with .NET 9)  
**License:** Apache 2.0  
**Maturity:** 10+ years, 50M+ NuGet downloads, actively maintained

### Key Benefits

#### 1. **Improved Readability**

**Before (xUnit):**
```csharp
Assert.Equal(HttpStatusCode.OK, response.StatusCode);
Assert.NotNull(result);
Assert.Equal(3, result.Count);
Assert.True(result.Contains(expectedItem));
```

**After (FluentAssertions):**
```csharp
response.StatusCode.Should().Be(HttpStatusCode.OK);
result.Should().NotBeNull()
    .And.HaveCount(3)
    .And.Contain(expectedItem);
```

#### 2. **Better Failure Messages**

**xUnit failure:**
```
Assert.Equal() Failure
Expected: True
Actual:   False
```

**FluentAssertions failure:**
```
Expected context.HasSucceeded to be true because the user has valid permissions, but found false.
```

Custom failure messages are built-in:
```csharp
actual.Should().BeTrue("the user has valid permissions");
```

#### 3. **Rich Assertion Library**

Out-of-the-box support for common scenarios that currently require custom helpers:

| Scenario | Current Approach | FluentAssertions |
|---|---|---|
| **Collection equivalence** | Manual `AssertCollections<T>()` helper | `actual.Should().BeEquivalentTo(expected)` |
| **Partial object matching** | Manual property-by-property assertions | `actual.Should().BeEquivalentTo(expected, options => options.Excluding(x => x.Id))` |
| **String matching** | `Assert.Contains()`, `Assert.StartsWith()` | `str.Should().Contain("text").And.StartWith("prefix")` |
| **Date/Time ranges** | Manual comparisons | `date.Should().BeCloseTo(expected, TimeSpan.FromSeconds(1))` |
| **Exception assertions** | `var ex = Assert.Throws<T>(); Assert.Equal(...)` | `act.Should().Throw<Exception>().WithMessage("*error*")` |
| **Null/empty checks** | `Assert.NotNull(); Assert.NotEmpty()` | `obj.Should().NotBeNullOrEmpty()` |
| **Numeric ranges** | `Assert.True(x >= 0 && x <= 100)` | `value.Should().BeInRange(0, 100)` |

#### 4. **Replaces Custom AssertionUtil Classes**

Current duplication:
- `Authorization.Tests/Util/AssertionUtil.cs` (~500 lines)
- `AccessMgmt.Tests/Utils/AssertionUtil.cs` (~800 lines)

FluentAssertions provides:
- `Should().BeEquivalentTo()` with deep comparison
- `Should().AllBeEquivalentTo()` for collections
- Customizable comparison rules via options

**Example — Current custom helper:**
```csharp
public static void AssertEqual(XacmlJsonResponse expected, XacmlJsonResponse actual)
{
    Assert.NotNull(actual);
    Assert.NotNull(expected);
    Assert.Equal(expected.Response.Count, actual.Response.Count);
    
    if (expected.Response.Count > 0)
    {
        for (int i = 0; i < expected.Response.Count(); i++)
        {
            AssertEqual(expected.Response[i], actual.Response[i]);
        }
    }
}
```

**FluentAssertions equivalent:**
```csharp
actual.Should().BeEquivalentTo(expected, options => options
    .ComparingByMembers<XacmlJsonResponse>()
    .WithStrictOrdering());
```

#### 5. **xUnit Integration**

FluentAssertions works seamlessly with xUnit:
- No changes to test runners or infrastructure
- Can be adopted incrementally (xUnit assertions continue to work)
- No changes to `[Fact]`, `[Theory]`, fixtures, or test discovery

### Alternatives Considered

| Library | Pros | Cons | Recommendation |
|---|---|---|---|
| **FluentAssertions** | Most mature, comprehensive, great docs, 50M+ downloads | Slightly larger dependency (~600KB) | ✅ **Recommended** |
| **Shouldly** | Lighter (~200KB), similar syntax | Less comprehensive, smaller community | ❌ Less feature-rich |
| **NFluent** | Good for legacy .NET | Less popular, fewer features | ❌ Niche |
| **Custom helpers only** | No new dependency | High maintenance, inconsistent across projects | ❌ Current pain point |

### Cost-Benefit Analysis

| Factor | Assessment |
|---|---|
| **Migration effort** | Low — Incremental adoption, no breaking changes |
| **Learning curve** | Low — Intuitive fluent API, excellent documentation |
| **Maintenance burden** | **Reduced** — Can retire ~1,300 lines of custom AssertionUtil code |
| **Test readability** | **Improved** — More natural language assertions |
| **Debugging time** | **Reduced** — Better failure messages |
| **Dependency risk** | Low — Apache 2.0 license, actively maintained, 10+ year track record |
| **Package size** | ~600KB (negligible for test projects) |
| **Breaking changes** | None — Opt-in migration |

---

## Decision

### ✅ **ADOPT FluentAssertions**

**Rationale:**

1. **High value, low risk** — Immediate readability improvements with minimal migration effort
2. **Solves existing pain points** — Eliminates need for duplicate AssertionUtil classes
3. **Industry standard** — 50M+ downloads, used by major .NET projects (AutoMapper, MediatR, etc.)
4. **Future-proof** — Active maintenance, .NET 9 compatible, long-term support expected
5. **Incremental adoption** — Can be introduced gradually alongside xUnit assertions

**Recommended approach:**
- Add to `Directory.Packages.props` as an optional package
- Use for **new tests** and **when modifying existing tests**
- **Do not** perform bulk rewrites of existing tests (ROI too low)
- **Consider** consolidating AssertionUtil custom helpers in a future phase

---

## Implementation Plan (Deferred to Next Step)

**Phase 4.2a — Add FluentAssertions Package**
1. Add `<PackageVersion Include="FluentAssertions" Version="7.1.0" />` to `src/Directory.Packages.props`
2. Add `<PackageReference Include="FluentAssertions" />` to test project `Directory.Build.props` files (opt-in per test folder)
3. Verify build and package restore

**Phase 4.2b — Documentation & Guidelines (Deferred)**
1. Add coding guidelines to `docs/testing/TESTING_INFRASTRUCTURE_OVERHAUL.md`
2. Document recommended patterns:
   - When to use FluentAssertions vs xUnit
   - Common scenarios and examples
   - Migration guidelines

**Phase 4.2c — Pilot Usage (Deferred)**
1. Use FluentAssertions in next coverage expansion tests (Phase 6.7b-6.7d)
2. Evaluate in practice before broader adoption
3. Gather team feedback

**Phase 4.2d — Retire AssertionUtil (Future)**
1. Identify most-used helpers in AssertionUtil classes
2. Replace with FluentAssertions equivalents in heavily-used tests
3. Mark AssertionUtil classes as obsolete
4. Remove in future phase

---

## Verification

### Package Research Completed

✅ Verified FluentAssertions is compatible with:
- .NET 9 (current TFM)
- xUnit v3 (current test framework)
- No conflicts with existing packages

✅ Verified license compatibility:
- Apache 2.0 license (compatible with project licensing)

✅ Verified package maturity:
- 10+ years of active development
- 50M+ NuGet downloads
- Recent releases (v7.x in 2024)

### Sample Code Comparisons Created

Created comparison examples for:
- Basic assertions (equality, null checks)
- Collection assertions (count, contains, equivalence)
- Exception assertions
- Complex object comparison (replacing AssertionUtil)

### Decision Documented

- Rationale captures benefits vs risks
- Implementation plan outlines incremental adoption
- No breaking changes to existing tests

---

## Deferred Work

| Item | Phase | Notes |
|---|---|---|
| Add FluentAssertions package to codebase | 4.2a | Follow-up step |
| Create test coding guidelines document | 4.2b | After package added |
| Pilot usage in new tests | 4.2c | During Phase 6 coverage work |
| Retire AssertionUtil classes | 4.2d | Long-term maintenance |

---

## Recommendation for Next Step

**Proceed to Phase 4.2a — Add FluentAssertions Package:**
- Add package reference to `Directory.Packages.props`
- Opt-in to test projects
- Verify build

**Alternative:** Proceed to Phase 3.2-3.4 (Mock Deduplication) if prioritizing infrastructure cleanup over new tooling.

---

## References

- [FluentAssertions Documentation](https://fluentassertions.com/)
- [FluentAssertions GitHub](https://github.com/fluentassertions/fluentassertions)
- [NuGet Package](https://www.nuget.org/packages/FluentAssertions/)
- Phase 4.2 in [TESTING_INFRASTRUCTURE_OVERHAUL.md](../TESTING_INFRASTRUCTURE_OVERHAUL.md)
