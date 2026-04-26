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
| `Models/` | Shared DTO constants, seed data |
| `Data/` | Static seed data (XACML policies, roles, packages, …) used by the template DB and by unit tests |
| `TestCertificates/` | Consolidated `.pfx` / `.pem` files used across the repo (Step 11) |

## Canonical mocks

The table below lists the mocks owned by `TestUtils`. Each has a single
implementation that every test project uses — duplicates were removed in
[`TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_1/15_Mock_Deduplication_Implementation.md`](TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_1/15_Mock_Deduplication_Implementation.md).

| Mock | Interface it implements |
|---|---|
| `PolicyRepositoryMock` | `IPolicyRepository` |
| `PolicyRetrievalPointMock` | `IPolicyRetrievalPoint` |
| `PolicyFactoryMock` | `IPolicyFactory` |
| `DelegationChangeEventQueueMock` | `IDelegationChangeEventQueue` |
| `ResourceRegistryClientMock` | `IResourceRegistryClient` |
| `ProfileClientMock` | `IProfile` |
| `AltinnRolesClientMock` | `IAltinnRolesClient` |
| `Altinn2RightsClientMock` | `IAltinn2RightsClient` |
| `PartiesClientMock` | `IPartiesClient` |
| `PublicSigningKeyProviderMock` | `IPublicSigningKeyProvider` |

**If you find yourself writing a new mock for an interface already in the
table, stop.** Reuse the existing one and add behavior via its existing
extension points instead.

## Moq vs hand-written mocks

- **Hand-written mocks** live in `TestUtils/Mocks/` and are used when the mock
  needs to look up canned responses from `Data/` (e.g. resource definitions
  or policies keyed by resource id). They are reusable and stateful.
- **Moq (`Mock<T>`)** is used in focused unit tests where each test wants
  different behaviour. Don't add a hand-written mock just to express one-off
  `Returns()` calls.

## Test certificates

Test certificates are **not duplicated per project**. They live in
`TestUtils/TestCertificates/` and are consumed by whichever test project
needs them. If you add a new certificate, add it there — not next to a single
test class.

## Adding a new mock

1. Check if the interface is already mocked (see table above + `TestUtils/Mocks/`).
2. If genuinely new, implement it in `TestUtils/Mocks/YourThingMock.cs`.
3. Register it via `ApiFixture.ConfigureServices` in the test that needs it.
4. If it keeps canned responses, add the data to `TestUtils/Data/`.
5. Extract common seeding behaviour into a static helper so other projects can
   reuse it.

## FluentAssertions is globally imported

`FluentAssertions` 7.0.0 is added via `Directory.Build.targets` for every test
project and test library. It's in `<Using>` so you **don't** need a `using
FluentAssertions;` directive — just call `.Should()` directly. See
[FLUENT_ASSERTIONS_GUIDELINES.md](FLUENT_ASSERTIONS_GUIDELINES.md).

## Next: [WRITING_TESTS.md](WRITING_TESTS.md)
