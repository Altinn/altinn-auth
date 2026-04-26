# Step 51 — Fix 6.7f: `ResourceRegistryMock` cache-hit bug causes `PermitWithActionFilterMatch` to be flaky

## Goal

Remove the `[Skip]` from
`PDP_Decision_ResourceRegistry_AccessListAuthorization_Json_PermitWithActionFilterMatch`
in `ResourceRegistry_DecisionTests` by fixing the ordering-dependent failure
that was deferred from Step 35.

## Root Cause

`ResourceRegistryMock.GetMembershipsForResourceForParty` has a cache-hit path
that silently returns `Enumerable.Empty<>()` instead of the cached memberships.

```csharp
// Before (buggy)
if (!_memoryCache.TryGetValue(cacheKey, out IEnumerable<...> memberships))
{
    // ... compute memberships ...
    if (memberships != null)
    {
        PutInCache(cacheKey, 5, memberships);
        return Task.FromResult(memberships);   // only reached on cache MISS with data
    }
}

return Task.FromResult(Enumerable.Empty<...>());  // ← reached on cache HIT too!
```

On a **cache miss** with data: computes, caches, and returns inside the `if`
block — correct.  
On a **cache miss** with no data: falls through to `Enumerable.Empty` — correct.  
On a **cache hit**: `TryGetValue` sets `memberships` from the cache and returns
`true`, so the outer `if (!...)` block is *skipped* entirely, and the method
falls through to `Enumerable.Empty<>()` — **wrong**.

### Why it only fails in full-class runs

`DenyActionFilterNotMatching` and `PermitWithActionFilterMatch` both call the
mock with the same `partyOrgNum="910459880"` + `resourceId="ttd-accesslist-resource-with-actionfilter"`.
The `DenyActionFilterNotMatching` test runs first (alphabetical ordering in xUnit v3),
primes the cache with the correct membership list (action filter `["read"]`), and
returns cleanly. When `PermitWithActionFilterMatch` runs next, `TryGetValue`
returns `true`, the if-block is bypassed, and the method returns empty memberships
— the PDP issues Deny instead of Permit.

The test passes in isolation because the cache is cold.

## What Changed

### `ResourceRegistryMock.cs`

Replaced the early-return-inside-if pattern with a single return at the end of
the method:

```csharp
// After (fixed)
if (!_memoryCache.TryGetValue(cacheKey, out IEnumerable<...> memberships))
{
    // ... compute memberships ...
    if (memberships != null)
    {
        PutInCache(cacheKey, 5, memberships);
    }
}

return Task.FromResult(memberships ?? Enumerable.Empty<...>());
```

Now `memberships` is populated by `TryGetValue` on a cache hit and returned
correctly.

### `ResourceRegistry_DecisionTests.cs`

- Removed the `[Fact(Skip = "Flaky due to AuthorizationApiFixture state pollution …")]`
  attribute and replaced it with `[Fact]`.
- Removed the `// TODO:` comment above the test that attributed the flakiness to
  `featureManageMock`/`timeProviderMock` registration in the fixture constructor
  (that diagnosis was incorrect — the mock mismatch in the cache was the actual
  cause).

## Verification

Full `ResourceRegistry_DecisionTests` class run (all 21 tests):

```
21 / 21 passed  (0 failed, 0 skipped)
```

Includes `PDP_Decision_ResourceRegistry_AccessListAuthorization_Json_PermitWithActionFilterMatch`
which was previously `[Skip]`ped.

## Deferred

- `Sender_ConfirmsDraftRequest_ReturnsPending` in `AccessManagement.Enduser.Api.Tests`
  remains `[Skip]`ped — separate environmental investigation still needed.
- `MaskinportenConsumersController` / `MaskinportenSuppliersController` coverage
  gap — requires PDP stubbing or seeding of `altinn_maskinporten_scope_delegation`
  resource (tracked in Phase 6.7c).
