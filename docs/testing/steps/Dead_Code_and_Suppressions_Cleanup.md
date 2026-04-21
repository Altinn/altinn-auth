# Step 10 — Dead Code & Suppressions Cleanup

**Phase:** 4.5 / 4.6 (L1–L3)  
**Goal:** Remove dead csproj entries: `GlobalSuppressions.cs`, `Compile Remove`,
`None Remove`, and empty `Folder Include` items.

---

## Audit Results

| Issue | File | Finding | Action |
|---|---|---|---|
| **L1** | `GlobalSuppressions.cs` | No files found in the repo — already removed in prior steps | No action needed |
| **L2** | `Altinn.AccessMgmt.PersistenceEF.csproj` | `<Folder Include="Utils\Values\" />` — directory does not exist | **Removed** |
| **L2** | `Altinn.AccessMgmt.PersistenceEF.csproj` | `<Folder Include="Migrations\" />` — directory exists | Kept |
| **L3** | `Altinn.AccessMgmt.PersistenceEF.csproj` | `<Compile Remove="20250623220238_Functions.cs" />` and `Designer` variant — files do not exist | **Removed** |
| **L3** | `AccessMgmt.Tests.csproj` | `<None Remove="Data\AuthorizedParties\TestDataAppsInstanceDelegation.cs" />` — unnecessary in SDK-style project (file is auto-compiled as `*.cs`) | **Removed** |

## Files Changed

1. `src/apps/Altinn.AccessManagement/src/Altinn.AccessMgmt.PersistenceEF/Altinn.AccessMgmt.PersistenceEF.csproj`
   - Removed `Compile Remove` ItemGroup for two non-existent migration files.
   - Removed `Folder Include` for non-existent `Utils\Values\` directory.
2. `src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/AccessMgmt.Tests.csproj`
   - Removed `None Remove` ItemGroup for `.cs` file that is auto-compiled.

## Verification

- ✅ Build successful
- ✅ All 402 Altinn.Authorization.Tests pass (0 failures)
- ✅ No behavioral changes — only dead csproj metadata removed
