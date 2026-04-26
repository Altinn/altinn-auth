# Mock Deduplication Audit

## Summary

Phase 3 audits mock duplication across the three mock locations:

1. **`Authorization.Tests/MockServices/`** — 19 mocks implementing `Altinn.Platform.Authorization.*` interfaces
2. **`AccessMgmt.Tests/Mocks/`** — 25 mocks implementing `Altinn.AccessManagement.Core.*` interfaces
3. **`TestUtils/Mocks/`** — 12 mocks implementing `Altinn.AccessManagement.Core.*` interfaces

## Key Finding: Cross-App Mocks Are NOT Duplicates

Authorization.Tests mocks implement **different interfaces** than AccessManagement mocks:

| Mock Name | Authorization Interface | AccessManagement Interface |
|---|---|---|
| `PolicyRepositoryMock` | `Altinn.Platform.Authorization.Repositories.Interface.IPolicyRepository` | `Altinn.AccessManagement.Core.Repositories.Interfaces.IPolicyRepository` |

These are **different contracts** from different apps. They cannot be consolidated.

## Intra-AccessManagement Duplicates (8 mocks)

The following mocks exist in **both** `AccessMgmt.Tests/Mocks/` and `TestUtils/Mocks/`:

| Mock | AccessMgmt.Tests (lines) | TestUtils (lines) | Drop-in? |
|---|---|---|---|
| `DelegationChangeEventQueueMock` | 26 | 26 | ⚠️ Need verify |
| `PolicyRetrievalPointMock` | 181 | 239 | ❌ Different scope |
| `ProfileClientMock` | 40 | 35 | ⚠️ Need verify |
| `PolicyRepositoryMock` | 143 | 151 | ❌ Different constructor (TestUtils has `ConcurrentDictionary` param) |
| `PolicyFactoryMock` | 23 | 29 | ⚠️ Need verify |
| `ResourceRegistryClientMock` | 450 | 449 | ⚠️ Nearly identical |
| `AltinnRolesClientMock` | 87 | 81 | ⚠️ Need verify |
| `Altinn2RightsClientMock` | 68 | 30 | ❌ AccessMgmt reads JSON data files; TestUtils is no-op stub |

### Why Simple Replacement Won't Work

- **AccessMgmt.Tests mocks** read test data from JSON files on disk (`Data/Json/...`).
  Tests depend on specific response shapes (delegation checks, rights, etc.).
- **TestUtils mocks** are mostly no-op stubs that return empty/default results.
  They're designed for integration tests where real services are replaced by
  Postgres + EF-backed repositories.

Swapping would break existing AccessMgmt.Tests controller tests that rely on
file-based mock responses.

### Consolidation Strategy (Future Work)

To consolidate, one of these approaches is needed:

1. **Enhance TestUtils mocks** to support file-based data loading via constructor
   parameters (e.g., `PolicyRepositoryMock(string filepath, ...)`). Some TestUtils
   mocks already do this.
2. **Migrate AccessMgmt.Tests controller tests** to `ApiFixture` pattern (Phase 2.2),
   which uses real Postgres instead of file-based mocks. This would eliminate the
   need for file-based mocks entirely.
3. **Defer** until the `AcceptanceCriteriaComposer` / `WebApplicationFixture` tests
   are retired or simplified.

**Recommendation:** Approach 2 is the cleanest long-term path. Consolidate mocks
as a by-product of migrating AccessMgmt.Tests to `ApiFixture`.

## AccessMgmt.Tests-Only Mocks (17)

These have no TestUtils equivalent and would stay in AccessMgmt.Tests
(or be moved to TestUtils if generally useful):

- `AccessListsAuthorizationClientMock`
- `Altinn2ConsentClientMock`
- `AMPartyServiceMock`
- `AuthenticationMock` / `AuthenticationNullRefreshMock`
- `ConfigurationManagerStub`
- `ConsentRepositoryMock`
- `DelegationMetadataRepositoryMock`
- `DelegationRequestMock`
- `JwtCookiePostConfigureOptionsStub`
- `MessageHandlerMock`
- `PartiesClientMock`
- `PdpDenyMock` / `PdpPermitMock`
- `PepWithPDPAuthorizationMock`
- `ResourceMetadataRepositoryMock`
- `SigningKeyResolverMock`

## Authorization.Tests Mocks (19) — Already Consolidated

These are used exclusively by `AuthorizationApiFixture` (created in Phase 2)
and cannot be shared with AccessManagement:

- `AccessManagementWrapperMock`, `ConfigurationManagerStub`, `ContextHandlerMock`,
  `DelegationChangeEventQueueMock`, `DelegationMetadataRepositoryMock`,
  `InstanceMetadataRepositoryMock`, `JwtCookiePostConfigureOptionsStub`,
  `OedRoleAssignmentWrapperMock`, `OidcProviderPostConfigureSettingsStub`,
  `OidcProviderSettingsStub`, `PartiesMock`, `PolicyInformationPointMock`,
  `PolicyRepositoryMock`, `PolicyRetrievalPointMock`, `ProfileMock`,
  `PublicSigningKeyProviderMock`, `RegisterServiceMock`, `ResourceRegistryMock`,
  `RolesMock`

## Conclusion

Mock deduplication is **blocked by the AccessMgmt.Tests architecture**. The 8
overlapping mocks cannot be safely consolidated until AccessMgmt.Tests controller
tests are migrated to the `ApiFixture` pattern (Phase 2.2).

No code changes made in this step — audit only.
