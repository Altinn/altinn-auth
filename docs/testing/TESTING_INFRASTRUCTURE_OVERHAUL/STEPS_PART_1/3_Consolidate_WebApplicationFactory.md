# Consolidate WebApplicationFactory Variants (Authorization.Tests)

## Goal

Eliminate duplicated mock-wiring across all 7 integration-test classes in
`Altinn.Authorization.Tests` by introducing a single shared fixture that
registers the full set of common service mocks once.

## Before

Every test class was an `IClassFixture<CustomWebApplicationFactory<DecisionController>>`
where `CustomWebApplicationFactory` was a **no-op** (empty `ConfigureWebHost`).
Each class' `GetTestClient()` helper manually registered 12-15 identical mock
services, leading to ~100 lines of duplicated DI wiring per file.

### Factory variants (Authorization.Tests)

| Class | Location | Purpose |
|---|---|---|
| `CustomWebApplicationFactory<TEntryPoint>` | `Webfactory/` | Empty shell – no shared configuration |

### Consumers (7 test classes)

| Class | Unique mocks beyond common set |
|---|---|
| `HealthCheckTests` | *(none)* |
| `PolicyControllerTest` | `IContextHandler` |
| `PartiesControllerTest` | `IFeatureManager` |
| `AccessListAuthorizationControllerTest` | *(none)* |
| `AltinnApps_DecisionTests` | `IFeatureManager`, `IEventsQueueClient`, `TimeProvider` |
| `ResourceRegistry_DecisionTests` | `IFeatureManager`, `IEventsQueueClient`, `TimeProvider` |
| `ExternalDecisionTest` | `IFeatureManager`, `IEventsQueueClient`, `TimeProvider` |

## Changes

### New: `AuthorizationApiFixture`

**File:** `Fixtures/AuthorizationApiFixture.cs`

A `WebApplicationFactory<Program>` subclass that registers the full common mock
set in `ConfigureWebHost`:

- **Auth stubs:** `JwtCookiePostConfigureOptionsStub`, `OidcProviderPostConfigureSettingsStub`, `PublicSigningKeyProviderMock`
- **Policy/delegation:** `PolicyRetrievalPointMock`, `PolicyRepositoryMock`, `DelegationMetadataRepositoryMock`, `DelegationChangeEventQueueMock`
- **External services:** `InstanceMetadataRepositoryMock`, `PartiesMock`, `ProfileMock`, `RolesMock`, `OedRoleAssignmentWrapperMock`, `RegisterServiceMock`, `ResourceRegistryMock`, `AccessManagementWrapperMock`

Provides two composition helpers:

- `ConfigureServices(Action<IServiceCollection>)` – queue per-class overrides applied during `ConfigureWebHost`
- `BuildClient()` – shorthand for `CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false })`

### Migrated consumers

Each test class was updated to:

1. Replace `IClassFixture<CustomWebApplicationFactory<DecisionController>>` → `IClassFixture<AuthorizationApiFixture>`
2. Remove all common mock registrations from `GetTestClient()`
3. Use `ConfigureServices()` or `WithWebHostBuilder()` only for unique per-class/per-test mocks

### Program.cs

Added `public partial class Program { }` to make the auto-generated entry point
visible to test projects (required for `WebApplicationFactory<Program>`).

### Deleted

- `Webfactory/CustomWebApplicationFactory.cs` – no longer referenced

## Verification

- **Build:** ✅ Successful
- **Tests:** ✅ 210/210 passed (0 failed, 0 skipped)

## Files Changed

| File | Action |
|---|---|
| `Fixtures/AuthorizationApiFixture.cs` | **Created** |
| `Program.cs` (Authorization app) | Added `public partial class Program { }` |
| `HealthCheckTests.cs` | Migrated |
| `PolicyControllerTest.cs` | Migrated |
| `PartiesControllerTest.cs` | Migrated |
| `AccessListAuthorizationControllerTest.cs` | Migrated |
| `AltinnApps_DecisionTests.cs` | Migrated |
| `ResourceRegistry_DecisionTests.cs` | Migrated |
| `ExternalDecisionTest.cs` | Migrated |
| `Webfactory/CustomWebApplicationFactory.cs` | **Deleted** |

## Out of Scope (deferred to future steps)

- `AccessMgmt.Tests/CustomWebApplicationFactory.cs` – 22 references across 7
  controller test files; uses `appsettings.test.json` + `IAuditAccessor`.
  Migration requires either extending `ApiFixture` or creating a lightweight
  mock-only fixture (no Postgres).
- `AccessMgmt.Tests/Fixtures/WebApplicationFixture.cs` – scenario-based with
  `PostgresServer`; many consumer tests already `[Skip]`ped. Migration path
  is to converge on `ApiFixture` from `TestUtils`.
