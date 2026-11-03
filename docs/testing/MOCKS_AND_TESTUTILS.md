# Shared Test Infrastructure (`TestUtils`)

All reusable test code lives in
**`src/apps/Altinn.AccessManagement/test/Altinn.AccessManagement.TestUtils/`**.
Every AccessManagement test project references it; the Authorization test
projects have their own small set of mocks under `MockServices/` for historical
reasons.

## What's in `TestUtils`

| Folder | Purpose |
|---|---|
| `Fixtures/` | `ApiFixture` — the canonical integration fixture (see [FIXTURES.md](FIXTURES.md)) |
| `Factories/` | `EFPostgresFactory` — template-cloned Postgres database provisioning |
| `Mocks/` | Canonical mock implementations of external clients and platform services |
| `Models/` | Shared test models (`ResourceRegistry/Right.cs`) |
| `Data/` | `TestData` and `TestEntities`, the named baseline actors that `TestDataSeeds` writes to the template DB, plus a few UserProfile and AuthorizedParties JSON files |
| `TestTokenGenerator.cs` | Mints test JWTs, signed with the shared in-memory test certificate |

The canned XACML policies, roles, resources and party JSON that most mocks
read live in `AccessMgmt.Tests/Data/`; `TestDataDirectory` finds that folder
from any test project's build output.

## Canonical mocks

The table below lists the shared mocks and where each lives. Most are in
`TestUtils/Mocks/`; two are elsewhere.

| Mock | Interface it implements | Lives in |
|---|---|---|
| `PolicyRepositoryMock` | `IPolicyRepository` | `TestUtils/Mocks/` |
| `PolicyRetrievalPointMock` | `IPolicyRetrievalPoint` | `TestUtils/Mocks/` |
| `PolicyFactoryMock` | `IPolicyFactory` | `TestUtils/Mocks/` |
| `ResourceRegistryClientMock` | `IResourceRegistryClient` | `TestUtils/Mocks/` |
| `ProfileClientMock` | `IProfile` | `TestUtils/Mocks/` |
| `AltinnRolesClientMock` | `IAltinnRolesClient` | `TestUtils/Mocks/` |
| `PartiesClientMock` | `IPartiesClient` | `AccessMgmt.Tests/Mocks/` |
| `PublicSigningKeyProviderMock` | `IPublicSigningKeyProvider` | `Altinn.Authorization.Testing` (shared library) |

**If you find yourself writing a new mock for an interface already in the
table, stop.** Reuse the existing one and add behavior via its existing
extension points instead.

## Moq vs hand-written mocks

- **Hand-written mocks** live in `TestUtils/Mocks/` and are used when the mock
  needs to look up canned responses from `AccessMgmt.Tests/Data/` (e.g. resource definitions
  or policies keyed by resource id). They are reusable and stateful.
- **Moq (`Mock<T>`)** is used in focused unit tests where each test wants
  different behaviour. Don't add a hand-written mock just to express one-off
  `Returns()` calls.

## Test certificates

Access Management and Authorization check in no certificate files. Test tokens
are signed with `TestSigningCertificate` from the shared
`Altinn.Authorization.Testing` library, a self-signed certificate generated in
memory once per test process. `TestTokenGenerator` and the older
`PrincipalUtil` both sign with it, and `ApiFixture` registers its key as the
issuer signing key. Authentication and Resource Registry still use their own
token helpers and checked-in `.pfx` files.

## Adding a new mock

1. Check if the interface is already mocked (see table above + `TestUtils/Mocks/`).
2. If genuinely new, implement it in `TestUtils/Mocks/YourThingMock.cs`.
3. Register it via `ApiFixture.ConfigureServices` in the test that needs it.
4. If it keeps canned responses, add the data to `AccessMgmt.Tests/Data/` and read it through `TestDataDirectory`.
5. Extract common seeding behaviour into a static helper so other projects can
   reuse it.

## Assertion library is globally imported

`AwesomeAssertions` is added via `Directory.Build.targets` for every test
project and test library. It's in `<Using>` so you **don't** need a `using
AwesomeAssertions;` directive — just call `.Should()` directly. See
[AWESOME_ASSERTIONS_GUIDELINES.md](AWESOME_ASSERTIONS_GUIDELINES.md).

## Next: [WRITING_TESTS.md](WRITING_TESTS.md)
