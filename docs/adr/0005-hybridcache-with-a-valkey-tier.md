# ADR-0005: Cache through HybridCache, with Valkey as an optional distributed tier

- **Status:** Accepted
- **Date:** 2026-10-27
- **Deciders:** Jon Kjetil (`@jonkjetiloye`, tech lead) on 2026-10-27, and the altinn-auth maintainers through review of the PR that added this ADR

**TL;DR**

- Every service in Access Management and Authorization caches through Microsoft `HybridCache`, registered once in `Altinn.Authorization.ServiceDefaults`, never through its own `IMemoryCache`.
- The distributed tier is a Valkey from the platform's `dis-cache-operator`, switched on per app by a feature flag, off everywhere until at22 has proven it.
- There is no backplane. A short local expiration bounds how long other pods serve a value a writer has invalidated; that bound is accepted instead of building invalidation, and call sites take their entry options from the shared registration so the bound actually applies to them.
- Values the serializer cannot carry, or that are too large for it, stay in memory only, by reference, and the PR that caches a new type proves it round-trips.
- Chosen because the platform now provides the cache, the library is already in ServiceDefaults, and one registration gives every repository the same connection handling, keys, limits and defaults.

This ADR was drafted with Claude (Fable 5.1). Human reader: @howieandersen.

## Context

Both apps cached in `IMemoryCache`, one cache per pod, about 25 call sites in Access Management and 14 in Authorization ([#155](https://github.com/Altinn/altinn-auth/issues/155)). Entries only expired. Nothing invalidated them when the same app wrote the data they were built from, so an app policy stored through `PolicyController.WritePolicy` could be served stale for ten minutes, and a resource sync left the authorized-parties filters stale for five. Concurrent misses on one key each called the backend. A pod restart lost everything.

The platform now offers a cache: a `Cache` resource from `dis-cache-operator` ([RFC 0014](https://github.com/Altinn/altinn-platform/blob/main/rfcs/0014-self-service-cache.md)) gives a Valkey in the app's namespace, a Secret with an ACL user and a password the operator rotates every 90 days while keeping the old one valid for 7 days. It runs in-cluster behind the service mesh, which encrypts the traffic. The tech lead decided that this is acceptable even for values that hold national identity numbers, which both apps cache.

`Altinn.Authorization.ServiceDefaults` already called `AddHybridCache()` for its own signing-key provider, so one library was in use before the decision.

## Decision

1. **We cache through Microsoft `HybridCache`.** No service takes `IMemoryCache` for data. The exceptions are the libraries that take it from dependency injection, the failed-lookup counter in `UserProfileLookupService`, and the SQL-hash tracing cache in `Altinn.AccessMgmt.PersistenceEF`; neither of the last two holds data.
2. **One registration, in ServiceDefaults.** `AddAltinnCache()` (Altinn/altinn-authorization-utils#804) registers the in-memory tier always, and the Valkey tier when the app passes `enabled: true` from its own flag and `Altinn:Cache:Host`, `Altinn:Cache:Username` and `Altinn:Cache:PasswordFile` are configured. The password is read from the mounted Secret file on connect and re-read every minute, so rotation needs no restart. The client uses `BacklogPolicy.FailFast` and 200 ms timeouts: with Valkey unreachable a miss costs 0 to 32 ms, where the library defaults cost 5 to 13 s. Keys in Valkey carry `{app}:v{n}:`, and `n` is bumped when a cached type changes shape.
3. **The flag decides the tier.** `AccessManagement.Cache.Distributed` in App Configuration and `FeatureManagement:Cache.Distributed` in Authorization's configuration, off in every environment until at22 has shown the tier working. The Valkey resources exist in at22 first ([#4747](https://github.com/Altinn/altinn-auth/issues/4747)).
4. **No backplane; a stated staleness bound instead.** A writer that removes a key or a tag clears Valkey at once; every other pod keeps its in-memory copy until that copy's local expiration, and that is the bound. The library derives the local expiration from an entry's own `Expiration` whenever a call sets only that, so the thirty-second local default the registration sets reaches only entries that use the defaults. Call sites therefore take their entry options from the shared registration, which sets the full lifetime for the in-memory tier while the Valkey tier is off and a short local lifetime of thirty seconds once it is on, instead of constructing `HybridCacheEntryOptions` themselves. A call site that sets its own expiration has that expiration as its bound. The writers we know invalidate: `PolicyAdministrationPoint` after storing an app policy, and `ResourceSyncService` after a run that wrote resources or package and role links. Invalidation across services, where Authorization caches Access Management, Register, Profile and Resource Registry data, needs an event feed and is out of scope.
5. **Prefixes and tags.** Every key has a family prefix (`party:`, `roles:`, `resource:`, `package:`, `policy:`, `profile:`, `delegations:`, `translation:`, `consent:`, `provider:`, `access-list:`), and every entry a tag for its family, so a writer can invalidate a family without knowing the keys.
6. **What the serializer cannot carry stays in memory.** `HybridCache` serializes values with System.Text.Json even for the in-memory tier, so a hit returns a copy, a `SortedDictionary` loses its comparer, and `XacmlPolicy` throws on read. Values that do not round-trip, or whose size makes the copy a cost, are kept in a sealed `[ImmutableObject(true)]` wrapper with the distributed tier disabled and the local expiration set to the entry's full lifetime; the library then keeps the instance by reference, and a tag removal reaches them only on the pod that runs it. Today that is the resource registry lists (1.1 MB serialized in at22, 5 MB in prod, 10 MB in tt02), the package search lists, and the parsed resource policy in Authorization. `MaximumPayloadBytes` stays at the library's 1 MB.
7. **Behaviour we keep from the old code.** A `null` from a client is not cached, so a party or resource created a moment ago is looked up again on the next call; the batch lookups fetch their misses in one client call. Where the old code cached a `null` or an empty list, the PR says so. Sliding expirations became absolute, and `CacheItemPriority` has no equivalent.
8. **Every cached type has a round-trip test.** A type that does not survive the serializer fails on the second read, not the first, so each PR that caches a new type adds a case to that app's round-trip test.

### Alternatives considered

- **FusionCache.** Has a backplane and fail-safe modes the Microsoft library lacks. Rejected by the tech lead: one more dependency for every repository, and `HybridCache` was already in ServiceDefaults; the staleness bound in decision 4 covers what the backplane would have bought.
- **Pub/sub invalidation across pods.** Would remove the thirty-second window. Deferred: a short local expiration gives the same visibility for a fraction of the code, and no caller has asked for less than that.
- **Raising `MaximumPayloadBytes` so the large lists reach Valkey.** Rejected: the lists are up to 10 MB, every pod fetches them once per expiry anyway, and serializing them on every write and read is what the wrapper avoids.
- **Caching `null` results** as `GetOrCreateAsync` does by default. Rejected for lookups of parties and resources: a newly created party would be invisible for the entry's lifetime.
- **Keeping the resource policy as XML and parsing per hit.** Measured in [#4799](https://github.com/Altinn/altinn-auth/pull/4799): 28 to 51 µs and 35 to 66 KB per parse, read twice per decision, against about 0.1 µs for the wrapped instance. Rejected in favour of the memory-only wrapper.

## Consequences

- One miss per key per process: `GetOrCreateAsync` gives single-flight where the old code let concurrent misses through. The lookups that must not cache `null` keep the old behaviour and no single-flight.
- Hits return copies. Code that mutated a cached object in place, or compared instances, had to change; tests that asserted `Assert.Same` on cached values became equality assertions.
- The memory-only entries are per pod. A tag removal clears them only where the writer runs; other pods follow at the local expiration. The resource sync's invalidation therefore reaches Valkey at once and the other pods within their expiry.
- The shared registration has to provide the entry options (Altinn/altinn-authorization-utils#804); until the migration PRs switch to them, every call site sets its own expiration and its local lifetime equals it, so a tag invalidation reaches other pods only when that lifetime ends.
- The batch lookups (parties by id and by UUID, main units, subject resources) read each key before one client call for the misses. With the tier on, each miss is one Valkey round trip before the batch; acceptable at today's batch sizes, to be measured in at22.
- Three central package pins moved to 10.0.13 because `Microsoft.Extensions.Caching.Hybrid` 10.11.0 requires them.
- Until ServiceDefaults 6.1 ships `AddAltinnCache()`, the apps register the cache with a temporary `AddHybridCache()`; the four migration PRs stay drafts until that line is swapped. The frontend reads of the `detail` field that ProblemDetails 6.0.0 no longer sets are a separate follow-up from the same upgrade.
- Two helpers, a try-get and a non-null fetch, exist in both apps; they belong in the shared package once its API has settled.

## References

- Feature [#155](https://github.com/Altinn/altinn-auth/issues/155); sub-issues [#4741](https://github.com/Altinn/altinn-auth/issues/4741), [#4743](https://github.com/Altinn/altinn-auth/issues/4743), [#4744](https://github.com/Altinn/altinn-auth/issues/4744), [#4745](https://github.com/Altinn/altinn-auth/issues/4745), [#4746](https://github.com/Altinn/altinn-auth/issues/4746), [#4747](https://github.com/Altinn/altinn-auth/issues/4747), [#4749](https://github.com/Altinn/altinn-auth/issues/4749), [#4752](https://github.com/Altinn/altinn-auth/issues/4752).
- Shared registration: [Altinn/altinn-authorization-utils#802](https://github.com/Altinn/altinn-authorization-utils/issues/802), PR [#804](https://github.com/Altinn/altinn-authorization-utils/pull/804).
- Platform: [RFC 0014](https://github.com/Altinn/altinn-platform/blob/main/rfcs/0014-self-service-cache.md), `dis-cache-operator` v0.5.0, `dis-console` in at22 as the reference consumer.
- Library: [Microsoft.Extensions.Caching.Hybrid](https://learn.microsoft.com/aspnet/core/performance/caching/hybrid) 10.11.0.
