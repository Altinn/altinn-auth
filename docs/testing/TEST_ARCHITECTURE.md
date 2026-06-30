# Test architecture

How the AccessManagement integration / web-app test suite is structured to stay
fast and shareable. It follows Authorization's pattern: a **project-local** base
fixture that bakes its own mock graph in, rather than centralising mocks in
`TestUtils`.

## Principles

- **Decouple three axes:** host configuration, data, and isolation.
- **Shared host by default, divergence by exception.**
- **Own your data:** a test creates and reads/writes only entities under unique
  IDs and asserts only against them, so sharing a host is safe.
- **Pay only for what you use:** a test that doesn't touch the database doesn't
  provision one.

## Host fixtures and profiles

Every web-app fixture descends from `ApiFixture`. The project-local base
fixture `AccessMgmtApiFixture : ApiFixture` registers the external-platform
client catalog once, so most test classes need no `ConfigureServices` and share
one host.

Genuine host variation is a small set of named profiles, each shared across a
cohort via `[Collection]` / `ICollectionFixture` so a profile builds its host
once. The axes that justify a separate profile:

- **PDP:** `PermitPdpMock` (default), `PdpPermitMock`, `PepWithPDPAuthorizationMock`.
- **Signing key:** `PublicSigningKeyProviderMock` (default), `SigningKeyResolverMock`
  (issuer-cert tokens).
- **HTTP context:** default, `MutableHttpContextAccessor`.
- **Feature flags** (e.g. Altinn2 role-revoke, request assignment resource/package,
  enduser Maskinporten admin).

Profile fixtures: `AccessMgmtApiFixture`, `LegacyApiFixture`, `ConsentApiFixture`,
`RightsApiFixture`, `NoDbApiFixture`. Shared cohorts:
`PolicyInformationPointDbCollection`, `ConsentDbCollection`, `RightsDbCollection`.
A class with an idiosyncratic config, or that needs per-test isolation, keeps its
own `IClassFixture`.

`LegacyApiFixture` is the full-schema (EF + Yuniql) profile and `ConsentApiFixture`'s
base, used by the Dapper-backed consent / resource tests. It is a retained profile,
not a separate convention.

## Two tiers

- **DB-less web-app tier** — tests that mock the entire data layer use
  `NoDbApiFixture` (`ProvisionsDatabase = false`): no Postgres clone or
  connection. Mocking the named repositories is necessary but not sufficient —
  most controller endpoints still reach Postgres via party / context resolution,
  so a class qualifies only once verified by running it, and the genuinely DB-less
  set is small.
- **DB-integration tier** — real `AppDbContext` + migrations against a cloned
  template database.

## Data ownership

A rich baseline template is seeded once. Each test then creates its own entities
under unique IDs and asserts only against them: additive seeds never collide and
exact assertions stay safe, so a test that mutates only its own rows can share a
host. A class with global / exact-count assertions or fixed colliding IDs keeps
its own fixture until it is converted to owned data.

## Author-facing pattern

```csharp
[Collection(DefaultHost.Name)]                     // one host for the whole profile
public class GetInstanceRights(DefaultHost host) {
    [Fact] public async Task ... {
        var party = host.NewOwnedOrg();            // unique id; no collision possible
        // seed under `party`; assert only on `party`'s results
    }
}
```

## Host-build count

`AccessMgmt.Tests` builds ~65 hosts. There is no CI guard on the count — CI does
not fail a test for being inefficient. `FixtureTiming` measures per-assembly
setup time for anyone optimising locally (see
[TEST_SETUP_TIMING.md](TEST_SETUP_TIMING.md)).

## Conventions

These are review-time conventions; without them the structure rots back:

- Own your data; don't add per-class DI when a profile fixture already covers it.
- Keep project-local mocks in the test project (the `AccessMgmtApiFixture`
  pattern), not `TestUtils` — shared test data and cross-assembly paths make
  centralising costly for no benefit.

## Related

- [TEST_SETUP_TIMING.md](TEST_SETUP_TIMING.md), [FIXTURES.md](FIXTURES.md),
  [CI.md](CI.md), [../SONARCLOUD.md](../SONARCLOUD.md).
