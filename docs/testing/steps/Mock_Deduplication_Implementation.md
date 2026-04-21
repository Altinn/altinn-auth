# Step 15 — Mock Deduplication Implementation (Phase 3.2-3.4)

## Goal

Consolidate 8 duplicated AccessManagement mocks between `AccessMgmt.Tests/Mocks/` and `TestUtils/Mocks/` to eliminate maintenance overhead and behavioral drift.

## What Changed

### 1. Removed Duplicate Mocks from AccessMgmt.Tests

Deleted 7 mock files from `src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/Mocks/`:

1. ✅ **DelegationChangeEventQueueMock.cs** — Identical to TestUtils version
2. ✅ **ProfileClientMock.cs** — Identical to TestUtils version  
3. ✅ **PolicyFactoryMock.cs** — TestUtils version is superset (has optional `WrittenPolicies` dictionary for tracking written policies)
4. ✅ **PolicyRepositoryMock.cs** — TestUtils version is superset (has optional `ConcurrentDictionary` parameter, defaults to null for backward compatibility)
5. ✅ **PolicyRetrievalPointMock.cs** — TestUtils version is enhanced (181 vs 239 lines, adds input validation, security hardening, logging improvements, support for "app_" prefixed resource IDs)
6. ✅ **ResourceRegistryClientMock.cs** — Nearly identical (449 vs 450 lines, TestUtils uses fully qualified `Models.ResourceRegistry.Right`)
7. ✅ **AltinnRolesClientMock.cs** — Identical to TestUtils version

### 2. Kept Separate (Cannot Consolidate)

❌ **Altinn2RightsClientMock.cs** — Fundamentally different implementations:
- `AccessMgmt.Tests` version: Reads test data from JSON files (`Data/Json/DelegationCheck/...`)
- `TestUtils` version: No-op stub returning empty `DelegationCheckResponse` and `DelegationActionResult`

Consolidating this would break existing AccessMgmt.Tests that depend on file-based mock data.

### 3. Updated Project References

**File:** `src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/AccessMgmt.Tests.csproj`

Added project reference to TestUtils:

```xml
<ProjectReference Include="..\Altinn.AccessManagement.TestUtils\Altinn.AccessManagement.TestUtils.csproj" />
```

### 4. Updated Using Statements (18 files)

Added both `TestUtils.Mocks` and `Tests.Mocks` namespaces to all test files that reference mocks:

```csharp
using Altinn.AccessManagement.TestUtils.Mocks;  // For consolidated mocks
using Altinn.AccessManagement.Tests.Mocks;      // For local-only mocks (18 remain)
```

**Files updated:**
- `PolicyAdministrationPointTest.cs`
- `Controllers/Altinn2RightsControllerTest.cs`
- `Controllers/DelegationsControllerTest.cs`
- `Controllers/MaskinportenSchemaControllerTest.cs`
- `Controllers/PartyControllerTests.cs`
- `Controllers/PolicyInformationPointControllerTest.cs`
- `Controllers/ResourceControllerTest.cs`
- `Controllers/RightsInternalControllerTest.cs`
- `Controllers/Bff/ConsentControllerTestBFF.cs`
- `Controllers/Enterprise/ConsentControllerTestEnterprise.cs`
- `Controllers/MaskinPorten/ConsentControllerTest.cs`
- `Controllers/ResourceOwnerAPI/AppsInstanceDelegationControllerTest.cs`
- `Fixtures/WebApplicationFixture.cs`
- `Helpers/DelegationHelperTest.cs`
- `Helpers/PolicyHelperTest.cs`
- `Resolvers/ResolverServiceCollection.cs`
- `Utils/SetupUtils.cs`
- `Utils/TestDataUtil.cs`

### 5. Fully Qualified Ambiguous References

Since `Altinn2RightsClientMock` exists in both namespaces, fully qualified 5 references to use the `Tests.Mocks` version:

```csharp
// Before:
services.AddSingleton<IAltinn2RightsClient, Altinn2RightsClientMock>();

// After:
services.AddSingleton<IAltinn2RightsClient, Tests.Mocks.Altinn2RightsClientMock>();
```

**Files with qualified references:**
- `Controllers/Altinn2RightsControllerTest.cs`
- `Controllers/ResourceOwnerAPI/AppsInstanceDelegationControllerTest.cs`
- `Controllers/RightsInternalControllerTest.cs`
- `Fixtures/WebApplicationFixture.cs`
- `Resolvers/ResolverServiceCollection.cs`

## Verification

### Build Status

✅ **Build successful** — All AccessManagement projects compile without errors.

```powershell
dotnet build
```

**Result:** Success with 0 errors (stylecop warnings only).

### AccessMgmt.Tests-Only Mocks (18 remaining)

These mocks stay in `AccessMgmt.Tests/Mocks/` as they are not duplicated and are test-specific:

1. `AccessListsAuthorizationClientMock`
2. `Altinn2ConsentClientMock`
3. `Altinn2RightsClientMock` ⚠️ (exists in both, but different implementations)
4. `AMPartyServiceMock`
5. `AuthenticationMock`
6. `AuthenticationNullRefreshMock`
7. `ConfigurationManagerStub`
8. `ConsentRepositoryMock`
9. `DelegationMetadataRepositoryMock`
10. `DelegationRequestMock`
11. `JwtCookiePostConfigureOptionsStub`
12. `MessageHandlerMock`
13. `PartiesClientMock`
14. `PdpDenyMock`
15. `PdpPermitMock`
16. `PepWithPDPAuthorizationMock`
17. `ResourceMetadataRepositoryMock`
18. `SigningKeyResolverMock`

## Impact

### Benefits

1. **Single source of truth** — 7 mocks now have one canonical implementation in TestUtils
2. **Enhanced functionality** — TestUtils versions have additional features (WrittenPolicies tracking, input validation, security hardening)
3. **Easier maintenance** — Bug fixes and enhancements to shared mocks benefit all test projects
4. **No behavioral drift** — Consolidated mocks can't diverge between test projects

### Deferred Work

**Altinn2RightsClientMock consolidation** remains blocked until one of:
1. AccessMgmt.Tests migrates to `ApiFixture` pattern (Phase 2.2) using real Postgres instead of file-based mocks
2. TestUtils version is enhanced to support file-based data loading

This is tracked under **Phase 2.2-2.3 — AccessMgmt.Tests WAF consolidation**.

## Files Changed

### Deleted (7 files)
- `src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/Mocks/DelegationChangeEventQueueMock.cs`
- `src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/Mocks/ProfileClientMock.cs`
- `src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/Mocks/PolicyFactoryMock.cs`
- `src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/Mocks/PolicyRepositoryMock.cs`
- `src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/Mocks/PolicyRetrievalPointMock.cs`
- `src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/Mocks/ResourceRegistryClientMock.cs`
- `src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/Mocks/AltinnRolesClientMock.cs`

### Modified (19 files)
- `src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/AccessMgmt.Tests.csproj` — Added TestUtils project reference
- 18 test files — Updated namespace imports and qualified Altinn2RightsClientMock references

## Summary

**7 out of 8 duplicate mocks successfully consolidated** from AccessMgmt.Tests to TestUtils, eliminating 1,100+ lines of duplicate code. TestUtils versions are supersets or enhanced versions, providing backward compatibility while adding optional tracking, validation, and security features. Only Altinn2RightsClientMock remains duplicated due to fundamentally different implementations (file-based vs no-op stub).

