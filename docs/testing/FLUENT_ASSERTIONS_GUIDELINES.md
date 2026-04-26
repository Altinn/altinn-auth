# FluentAssertions Usage Guidelines

**Status:** Adopted (see [14_Add_FluentAssertions_Package.md](TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_1/14_Add_FluentAssertions_Package.md))
**Scope:** All test projects in this repository.
**Package:** `FluentAssertions` 7.0.0, globally imported via `Directory.Build.targets`.

---

## TL;DR

- **New tests:** Use FluentAssertions (`.Should()`).
- **Modified tests:** Convert the assertions you touch to FluentAssertions.
- **Untouched existing tests:** Leave as-is. No bulk rewrites.
- **Custom `AssertionUtil` helpers:** Prefer `Should().BeEquivalentTo(...)` for
  new code. Do not add new helpers to the two existing `AssertionUtil` classes.

`Xunit` and `FluentAssertions` are both in `<Using>` — no `using` directive
needed.

---

## Why FluentAssertions

Rationale captured in [13_FluentAssertions_Evaluation.md](TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_1/13_FluentAssertions_Evaluation.md).
In short: more readable assertions, better failure messages, rich built-in
support for collections / equivalence / exceptions / dates, and it lets us
retire ~1,300 lines of bespoke `AssertionUtil` code over time.

---

## Patterns — xUnit → FluentAssertions

### Equality and nullness

```csharp
// xUnit
Assert.Equal(HttpStatusCode.OK, response.StatusCode);
Assert.NotNull(result);
Assert.Null(error);

// FluentAssertions
response.StatusCode.Should().Be(HttpStatusCode.OK);
result.Should().NotBeNull();
error.Should().BeNull();
```

### Booleans with a reason

Always provide a `because` clause when the intent isn't obvious from the
expression — it shows up verbatim in the failure message.

```csharp
context.HasSucceeded.Should().BeTrue("the user has the required role");
context.HasFailed.Should().BeFalse();
```

### Strings

```csharp
// xUnit
Assert.Contains("denied", message);
Assert.StartsWith("Bearer ", header);

// FluentAssertions
message.Should().Contain("denied");
header.Should().StartWith("Bearer ");

// Chained
message.Should().NotBeNullOrEmpty()
    .And.Contain("denied")
    .And.NotContain("password");
```

### Numeric ranges

```csharp
// xUnit
Assert.True(count >= 1 && count <= 10);

// FluentAssertions
count.Should().BeInRange(1, 10);
```

### Dates / times

Use `BeCloseTo` for timestamps that depend on `DateTimeOffset.UtcNow` or
similar, so tests don't flake on millisecond drift.

```csharp
created.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
expiry.Should().BeAfter(DateTimeOffset.UtcNow);
```

### Collections

```csharp
// xUnit
Assert.Equal(3, results.Count);
Assert.Contains(expectedItem, results);
Assert.Empty(errors);

// FluentAssertions
results.Should().HaveCount(3).And.Contain(expectedItem);
errors.Should().BeEmpty();

// Order-sensitive sequence equality
results.Should().Equal(expected);

// Order-insensitive, deep structural equality
results.Should().BeEquivalentTo(expected);

// Every element matches a predicate
results.Should().OnlyContain(r => r.IsValid);
```

### Complex object comparison (replaces most `AssertionUtil` helpers)

```csharp
// Preferred
actual.Should().BeEquivalentTo(expected);

// Ignore volatile properties
actual.Should().BeEquivalentTo(expected, o => o
    .Excluding(x => x.Id)
    .Excluding(x => x.Created));

// Strict ordering for lists
actual.Should().BeEquivalentTo(expected, o => o.WithStrictOrdering());
```

Use `BeEquivalentTo` for DTO/record/response-object assertions instead of
walking properties by hand or adding another overload to `AssertionUtil`.

### Exceptions

```csharp
// xUnit
var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => svc.DoAsync());
Assert.Contains("not allowed", ex.Message);

// FluentAssertions — synchronous
Action act = () => svc.Do();
act.Should().Throw<InvalidOperationException>()
    .WithMessage("*not allowed*");

// FluentAssertions — async
Func<Task> act = () => svc.DoAsync();
await act.Should().ThrowAsync<InvalidOperationException>()
    .WithMessage("*not allowed*");

// Assert no exception
await act.Should().NotThrowAsync();
```

### HTTP responses

```csharp
response.StatusCode.Should().Be(HttpStatusCode.OK);
response.Headers.Location.Should().NotBeNull();
(await response.Content.ReadAsStringAsync())
    .Should().Contain("\"status\":\"ok\"");
```

---

## When to keep using xUnit assertions

FluentAssertions is preferred, but these cases are fine with plain xUnit:

- **`Assert.Fail("reason")`** inside a `catch` or unreachable branch — it
  short-circuits flow analysis cleanly.
- **`[Theory]` parameter guards** where the theory data itself is the
  assertion (e.g. `Assert.Equal(input.Length, parsed.Length)` as the single
  line of a tiny parametrised test). Either style is acceptable; don't churn
  existing code for this.
- **xUnit's `Assert.Raises` / `Assert.PropertyChanged`** — no FluentAssertions
  equivalent is currently used in this repo, keep the xUnit API.

Never mix `Assert.Xxx(...)` and `.Should()` assertions on the **same value**
in the same test — pick one style per assertion target for readability.

---

## Relationship to the two `AssertionUtil` classes

The existing `AssertionUtil` classes live at:

- `src/apps/Altinn.Authorization/test/Altinn.Authorization.Tests/Util/AssertionUtil.cs`
- `src/apps/Altinn.AccessManagement/test/AccessMgmt.Tests/Utils/AssertionUtil.cs`

Guidelines:

- **Do not** add new helper methods to either class.
- **Do** migrate call sites you already touch to `Should().BeEquivalentTo(...)`.
- Bulk retirement of these classes is tracked as **Phase 4.2d** in the
  overhaul plan and will be executed as a dedicated step once enough call
  sites have migrated organically.

---

## Failure message quality

FluentAssertions failure messages include the expression text, expected
value, actual value, and any `because` reason you provide. Favor precise
reasons over generic ones:

```csharp
// Good
user.Roles.Should().Contain("admin",
    "the test arranges a user promoted via PromoteToAdmin()");

// Less useful
user.Roles.Should().Contain("admin", "it should be admin");
```

A good rule of thumb: if the `because` clause only restates the assertion,
omit it.

---

## References

- [FluentAssertions docs](https://fluentassertions.com/)
- [Step 13 — Evaluation](TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_1/13_FluentAssertions_Evaluation.md)
- [Step 14 — Package install](TESTING_INFRASTRUCTURE_OVERHAUL/STEPS_PART_1/14_Add_FluentAssertions_Package.md)
- [TEST_NAMING_CONVENTION.md](TEST_NAMING_CONVENTION.md)
- [TESTING_INFRASTRUCTURE_OVERHAUL.md — Phase 4.2](TESTING_INFRASTRUCTURE_OVERHAUL/TESTING_INFRASTRUCTURE_OVERHAUL_PART_1.md)
