using Altinn.AccessMgmt.Persistence.Core.Helpers;

namespace Altinn.AccessMgmt.Core.Tests.PersistenceCore;

public class GenericFilterBuilderTest
{
    private record SampleEntity(string Name, int Age);

    // ── Empty ───────────────────────────────────────────────────────────────
    [Fact]
    public void Empty_WhenNoFilters_ReturnsTrue()
    {
        var builder = new GenericFilterBuilder<SampleEntity>();
        Assert.True(builder.Empty);
    }

    [Fact]
    public void Empty_AfterAdd_ReturnsFalse()
    {
        var builder = new GenericFilterBuilder<SampleEntity>()
            .Add(x => x.Name, "Alice");
        Assert.False(builder.Empty);
    }

    // ── Add ─────────────────────────────────────────────────────────────────
    [Fact]
    public void Add_CreatesFilterWithCorrectPropertyAndValue()
    {
        var builder = new GenericFilterBuilder<SampleEntity>()
            .Add(x => x.Name, "Bob");

        var filters = builder.ToList();
        Assert.Single(filters);
        Assert.Equal("Name", filters[0].PropertyName);
        Assert.Equal("Bob", filters[0].Value);
        Assert.Equal(FilterComparer.Equals, filters[0].Comparer);
    }

    [Fact]
    public void Add_WithCustomComparer_StoresComparer()
    {
        var builder = new GenericFilterBuilder<SampleEntity>()
            .Add(x => x.Name, "Alice", FilterComparer.Contains);

        var filter = builder.First();
        Assert.Equal(FilterComparer.Contains, filter.Comparer);
    }

    [Fact]
    public void Add_IntProperty_CreatesFilterCorrectly()
    {
        var builder = new GenericFilterBuilder<SampleEntity>()
            .Add(x => x.Age, 42);

        var filter = builder.First();
        Assert.Equal("Age", filter.PropertyName);
        Assert.Equal(42, filter.Value);
    }

    [Fact]
    public void Add_IsFluentAndReturnsBuilder()
    {
        var builder = new GenericFilterBuilder<SampleEntity>();
        var returned = builder.Add(x => x.Name, "X");
        Assert.Same(builder, returned);
    }

    // ── Equal ───────────────────────────────────────────────────────────────
    [Fact]
    public void Equal_CreatesEqualsFilter()
    {
        var builder = new GenericFilterBuilder<SampleEntity>()
            .Equal(x => x.Name, "Charlie");

        var filter = builder.First();
        Assert.Equal(FilterComparer.Equals, filter.Comparer);
        Assert.Equal("Charlie", filter.Value);
        Assert.Equal("Name", filter.PropertyName);
    }

    // ── NotSet ──────────────────────────────────────────────────────────────
    [Fact]
    public void NotSet_CreatesNullEqualsFilter()
    {
        var builder = new GenericFilterBuilder<SampleEntity>()
            .NotSet(x => x.Name);

        var filter = builder.First();
        Assert.Equal("Name", filter.PropertyName);
        Assert.Null(filter.Value);
        Assert.Equal(FilterComparer.Equals, filter.Comparer);
    }

    // ── In ──────────────────────────────────────────────────────────────────
    [Fact]
    public void In_AddsOneEqualsFilterPerValue()
    {
        var builder = new GenericFilterBuilder<SampleEntity>()
            .In(x => x.Name, new[] { "Alice", "Bob", "Charlie" });

        var filters = builder.ToList();
        Assert.Equal(3, filters.Count);
        Assert.All(filters, f => Assert.Equal(FilterComparer.Equals, f.Comparer));
        Assert.All(filters, f => Assert.Equal("Name", f.PropertyName));
    }

    [Fact]
    public void In_WithNullValues_ThrowsArgumentException()
    {
        var builder = new GenericFilterBuilder<SampleEntity>();
        Assert.Throws<ArgumentException>(() => builder.In(x => x.Name, null));
    }

    [Fact]
    public void In_WithEmptyValues_ThrowsArgumentException()
    {
        var builder = new GenericFilterBuilder<SampleEntity>();
        Assert.Throws<ArgumentException>(() => builder.In(x => x.Name, Array.Empty<string>()));
    }

    // ── NotIn ───────────────────────────────────────────────────────────────
    [Fact]
    public void NotIn_AddsOneNotEqualFilterPerValue()
    {
        var builder = new GenericFilterBuilder<SampleEntity>()
            .NotIn(x => x.Name, new[] { "Alice", "Bob" });

        var filters = builder.ToList();
        Assert.Equal(2, filters.Count);
        Assert.All(filters, f => Assert.Equal(FilterComparer.NotEqual, f.Comparer));
    }

    [Fact]
    public void NotIn_WithNullValues_ThrowsArgumentException()
    {
        var builder = new GenericFilterBuilder<SampleEntity>();
        Assert.Throws<ArgumentException>(() => builder.NotIn(x => x.Name, null));
    }

    // ── IEnumerable ─────────────────────────────────────────────────────────
    [Fact]
    public void IEnumerable_IteratesAllFilters()
    {
        var builder = new GenericFilterBuilder<SampleEntity>()
            .Add(x => x.Name, "X")
            .Add(x => x.Age, 5);

        Assert.Equal(2, builder.Count());
    }

    [Fact]
    public void IEnumerable_NonGeneric_IteratesAllFilters()
    {
        var builder = new GenericFilterBuilder<SampleEntity>()
            .Add(x => x.Name, "X");

        // Exercise the non-generic IEnumerable path
        var count = 0;
        foreach (object _ in (System.Collections.IEnumerable)builder)
        {
            count++;
        }

        Assert.Equal(1, count);
    }
}
