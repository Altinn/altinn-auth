# Writing Tests

Short, opinionated guide. For naming, see
[TEST_NAMING_CONVENTION.md](TEST_NAMING_CONVENTION.md). For assertions, see
[AWESOME_ASSERTIONS_GUIDELINES.md](AWESOME_ASSERTIONS_GUIDELINES.md).

## Pick the right test type

Choose the simplest thing that exercises the code:

| The code you're testing… | Write… |
|---|---|
| A pure method, static helper, or class with injectable dependencies | **Unit test** with Moq / hand-constructed dependencies. No fixture. |
| A controller action | **Unit test** by instantiating the controller directly with Moq, unless you need the full MVC pipeline (model binding, filters, auth). |
| The full MVC pipeline / middleware / auth / routing | **Integration test** with [`ApiFixture`](FIXTURES.md) (or `AuthorizationApiFixture` for the Authorization app). |
| Repository / EF Core query | **Integration test** with `ApiFixture` (template-cloned DB). |

**Default to unit tests.** They're orders of magnitude faster and more
precise. Almost all controller and service coverage can be achieved with
Moq-based unit tests; integration tests are reserved for the cases that
genuinely need the pipeline.

## Tag and place each test

Every test class must carry a category marker:

- `[UnitTest]` — pure/fast, no fixture.
- `[IntegrationTest]` — uses `ApiFixture` / a database / the real pipeline.

The markers (`src/testing/TestCategories.cs`) emit a `Category` trait that CI
uses to run unit and integration as separate lanes, so an **untagged test runs
in neither lane**. Put the marker on the test class (and on each nested test
class, where that pattern is used).

In a project that has both kinds, place the file under the `Unit/` or
`Integration/` folder matching its marker; the namespace gains the matching
`.Unit` / `.Integration` segment (namespace = folder). Single-type projects
stay flat.

## xUnit v3 specifics

All projects are on **xUnit v3**. A few things differ from v2:

- Test projects build as **executables** (`OutputType=Exe`) and run on
  Microsoft Testing Platform. Don't add `Microsoft.NET.Test.Sdk` or
  `xunit.runner.visualstudio`.
- Prefer `Assert.Skip("reason")` over returning early. It renders as
  *skipped*, not *passed*.
- `IAsyncLifetime` is `ValueTask`-based (`InitializeAsync`/`DisposeAsync`
  both return `ValueTask`).
- `TheoryData<T>` is more strongly typed — no more boxed `object[]` unless
  you opt in.

## Integration test template

```csharp
[IntegrationTest]
public class ConnectionsControllerTest : IClassFixture<ApiFixture>
{
    private readonly HttpClient _client;

    public ConnectionsControllerTest(ApiFixture fixture)
    {
        fixture.ConfigureServices(services =>
        {
            services.AddSingleton<IExternalClient, ExternalClientMock>();
        });

        fixture.EnsureSeedOnce<ConnectionsControllerTest>(async db =>
        {
            db.Parties.AddRange(TestData.Parties);
            await db.SaveChangesAsync();
        });

        _client = fixture.CreateTestClient(c =>
            c.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", TestTokens.ForUser(1337)));
    }

    [Fact]
    public async Task GetConnection_ForExistingParty_Returns200()
    {
        var response = await _client.GetAsync("/accessmanagement/api/v1/connections/42");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

### Rules

1. **Wire up the fixture in the constructor.** After the first
   `CreateClient()` the host is frozen.
2. **Seed once.** Use `EnsureSeedOnce<TKey>` so re-entrant test classes don't
   duplicate data.
3. **One `HttpClient` per class, stored in a field.** Don't build a new one
   per test.

## Unit test template

```csharp
[UnitTest]
public class RoleServiceTests
{
    private readonly Mock<IRoleRepository> _repo = new();
    private readonly RoleService _sut;

    public RoleServiceTests()
    {
        _sut = new RoleService(_repo.Object);
    }

    [Fact]
    public async Task GetById_WhenMissing_ReturnsNull()
    {
        _repo.Setup(r => r.Get(It.IsAny<Guid>(), default))
             .ReturnsAsync((Role?)null);

        var result = await _sut.GetById(Guid.NewGuid());

        result.Should().BeNull();
    }
}
```

## Parallelism

- **Inside a class:** tests run sequentially (xUnit default).
- **Across classes:** they run in parallel.
- `[Collection("name")]` serialises across classes in the same collection.
  Use it when the tests share mutable state — e.g. all classes using a
  `LegacyApiFixture` that resets the Yuniql schema.

## Skipping tests

```csharp
[Fact]
public void Foo()
{
    if (!Dockerinstalled())
        Assert.Skip("Docker not available");
    ...
}
```

If a whole fixture can't initialise (e.g. no Docker), skip at the fixture
level — every test that depends on it will report as skipped rather than
failed.

## Keep tests deterministic

- No `DateTime.Now` in assertions — inject an `IClock` or freeze the time.
- Avoid background work that outlives the test.
- No `Thread.Sleep` — use `TaskCompletionSource` or polling with a timeout.
- No dependency on a live external endpoint. A test that calls out to a real
  service (e.g. a resource-registry lookup) is flaky by construction and must
  mock the boundary instead.

### Flaky / environment-dependent tests

We track known-flaky and environment-dependent tests as GitHub issues, not as a
list in this repo (an in-repo register goes stale faster than it is read). When
you hit one:

- If it is genuinely environmental (needs Azurite, a live endpoint, a real
  container runtime), gate it with a clear `Skip` reason naming the dependency,
  and link the tracking issue.
- Otherwise treat the flake as a bug: file an issue, and fix the determinism
  problem rather than retrying or widening tolerances.

## Next: [COVERAGE.md](COVERAGE.md)
