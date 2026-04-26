# Writing Tests

Short, opinionated guide. For naming, see
[TEST_NAMING_CONVENTION.md](TEST_NAMING_CONVENTION.md). For assertions, see
[FLUENT_ASSERTIONS_GUIDELINES.md](FLUENT_ASSERTIONS_GUIDELINES.md).

## Pick the right test type

Choose the simplest thing that exercises the code:

| The code you're testing… | Write… |
|---|---|
| A pure method, static helper, or class with injectable dependencies | **Unit test** with Moq / hand-constructed dependencies. No fixture. |
| A controller action | **Unit test** by instantiating the controller directly with Moq, unless you need the full MVC pipeline (model binding, filters, auth). |
| The full MVC pipeline / middleware / auth / routing | **Integration test** with [`ApiFixture`](FIXTURES.md) (or `AuthorizationApiFixture` for the Authorization app). |
| Repository / EF Core query | **Integration test** with `ApiFixture` (template-cloned DB). |

**Default to unit tests.** They're orders of magnitude faster and more
precise. The [coverage steps 42–60](TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_1/INDEX.md) demonstrate that almost
all controller and service coverage can be achieved with Moq-based unit
tests; integration tests are reserved for the cases that genuinely need the
pipeline.

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

## Next: [COVERAGE.md](COVERAGE.md)
