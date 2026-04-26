# Step 11 — Certificate Consolidation (Phase 3.5 / M8)

## Goal

Eliminate duplicate certificate files from `Authorization.Tests` by replacing
file-based certificate loading with programmatic in-memory certificate generation.
This addresses issue **M8** (certificate files duplicated between `AccessMgmt.Tests`
and `Authorization.Tests`).

## What Changed

### New File

- **`Util/TestCertificates.cs`** — Static helper that generates a self-signed
  X.509 certificate in memory using `RSA.Create(2048)` +
  `CertificateRequest.CreateSelfSigned()`. Provides:
  - `TestCertificates.Default` — the `X509Certificate2` instance
  - `TestCertificates.SigningCredentials` — for token generation
  - `TestCertificates.SecurityKey` — for token validation

### Modified Files

| File | Change |
|---|---|
| `Util/JwtTokenMock.cs` | Removed `GetSigningCredentials()` method and file-based cert loading. Now uses `TestCertificates.SigningCredentials` directly. Removed `System.IO` and `System.Security.Cryptography.X509Certificates` imports. |
| `MockServices/ConfigurationManagerStub.cs` | Replaced `GetSigningKeys()` helper (loaded `.cer` file) with direct `TestCertificates.SecurityKey` usage. Simplified `GetConfigurationAsync` to synchronous. |
| `MockServices/PublicSigningKeyProviderMock.cs` | Replaced `{issuer}-org.pem` file loading with `TestCertificates.SecurityKey`. All issuers now use the same key (matching how `JwtTokenMock` signs all tokens). |
| `Altinn.Authorization.Tests.csproj` | Removed `<None Update>` entries for the 4 certificate files. |

### Deleted Files

- `selfSignedTestCertificate.pfx`
- `selfSignedTestCertificatePublic.cer`
- `platform-org.pem`
- `platform-org.pfx`

## Why This Works

Previously, `JwtTokenMock` signed tokens using either `selfSignedTestCertificate.pfx`
or `{issuer}-org.pfx`, and the verification stubs (`ConfigurationManagerStub`,
`PublicSigningKeyProviderMock`) provided the matching public keys from `.cer`/`.pem`
files. Now all three classes use the same in-memory certificate from
`TestCertificates`, so signing and verification remain consistent.

## Verification

- **Build:** ✅ Successful
- **Tests:** ✅ All 402 Authorization.Tests pass (0 failures)

## What's NOT Changed (Deferred)

- **`AccessMgmt.Tests` certificates** — These are still file-based. Migrating them
  requires the Docker-dependent Phase 2.2 (WAF consolidation) work. The 4 cert
  files remain in `AccessMgmt.Tests/` along with the additional `ttd-org.pfx` and
  `ttd-org.pem` files.
- **`AccessMgmt.Tests/Utils/JwtTokenMock.cs`** and
  **`AccessMgmt.Tests/Mocks/ConfigurationManagerStub.cs`** — Same pattern as
  Authorization.Tests but blocked by the broader migration.

## Design Note

The `TestCertificates` pattern mirrors `TestTokenGenerator` in
`Altinn.AccessManagement.TestUtils`, which also generates certificates
programmatically. When Authorization.Tests eventually gets its own shared test
utilities library, `TestCertificates` can be promoted there.
