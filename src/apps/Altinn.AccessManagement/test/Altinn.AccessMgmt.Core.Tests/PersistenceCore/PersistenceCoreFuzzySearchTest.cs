using Altinn.AccessMgmt.Core.Utils;

namespace Altinn.AccessMgmt.Core.Tests.PersistenceCore;

public class PersistenceCoreFuzzySearchTest
{
    private record Item(string Name, string Description);

    private record ItemWithTags(string Name, List<string> Tags);

    private static SearchPropertyBuilder<Item> NameOnlyBuilder(FuzzynessLevel level = FuzzynessLevel.High)
        => new SearchPropertyBuilder<Item>().Add(x => x.Name, 1.0, level);

    // ── FuzzySearch.PerformFuzzySearch ─────────────────────────────────────
    [Fact]
    public void PerformFuzzySearch_EmptyTerm_ReturnsEmpty()
    {
        var data = new List<Item> { new("Apple", "Fruit") };
        var results = FuzzySearch.PerformFuzzySearch(data, string.Empty, NameOnlyBuilder());
        Assert.Empty(results);
    }

    [Fact]
    public void PerformFuzzySearch_NullTerm_ReturnsEmpty()
    {
        var data = new List<Item> { new("Apple", "Fruit") };
        var results = FuzzySearch.PerformFuzzySearch(data, null, NameOnlyBuilder());
        Assert.Empty(results);
    }

    [Fact]
    public void PerformFuzzySearch_ExactMatch_ReturnsResult()
    {
        var data = new List<Item>
        {
            new("Apple", "Fruit"),
            new("Banana", "Fruit"),
        };

        var results = FuzzySearch.PerformFuzzySearch(data, "Apple", NameOnlyBuilder());

        Assert.NotEmpty(results);
        Assert.Equal("Apple", results.First().Object.Name);
    }

    [Fact]
    public void PerformFuzzySearch_ExactMatch_ScoreIsPositive()
    {
        var data = new List<Item> { new("Apple", "Fruit") };
        var results = FuzzySearch.PerformFuzzySearch(data, "Apple", NameOnlyBuilder());
        Assert.True(results.First().Score > 0);
    }

    [Fact]
    public void PerformFuzzySearch_NoMatch_ReturnsEmpty()
    {
        var data = new List<Item> { new("Apple", "Fruit") };
        var results = FuzzySearch.PerformFuzzySearch(data, "zzzzzzz", NameOnlyBuilder(FuzzynessLevel.Low));
        Assert.Empty(results);
    }

    [Fact]
    public void PerformFuzzySearch_EmptyDataList_ReturnsEmpty()
    {
        var results = FuzzySearch.PerformFuzzySearch(new List<Item>(), "Apple", NameOnlyBuilder());
        Assert.Empty(results);
    }

    [Fact]
    public void PerformFuzzySearch_FuzzyTypo_HighFuzziness_FindsResult()
    {
        // "Applr" is a one-character typo of "Apple" — should match with High fuzziness
        var data = new List<Item> { new("Apple", "Fruit") };
        var results = FuzzySearch.PerformFuzzySearch(data, "Applr", NameOnlyBuilder(FuzzynessLevel.High));
        Assert.NotEmpty(results);
    }

    [Fact]
    public void PerformFuzzySearch_TwoProperties_HitsOnSecondProperty()
    {
        var builder = new SearchPropertyBuilder<Item>()
            .Add(x => x.Name, 0.5, FuzzynessLevel.High)
            .Add(x => x.Description, 0.5, FuzzynessLevel.High);

        var data = new List<Item> { new("SomethingElse", "Fruit") };

        var results = FuzzySearch.PerformFuzzySearch(data, "Fruit", builder);
        Assert.NotEmpty(results);
    }

    [Fact]
    public void PerformFuzzySearch_CollectionCombined_MatchesItemInCollection()
    {
        var builder = new SearchPropertyBuilder<ItemWithTags>()
            .AddCollection(x => x.Tags, t => t, 1.0, FuzzynessLevel.High, detailed: false);

        var data = new List<ItemWithTags>
        {
            new("Entity", new List<string> { "Apple", "Banana" }),
        };

        var results = FuzzySearch.PerformFuzzySearch(data, "Apple", builder);
        Assert.NotEmpty(results);
    }

    [Fact]
    public void PerformFuzzySearch_CollectionDetailed_MatchesItemInCollection()
    {
        var builder = new SearchPropertyBuilder<ItemWithTags>()
            .AddCollection(x => x.Tags, t => t, 1.0, FuzzynessLevel.High, detailed: true);

        var data = new List<ItemWithTags>
        {
            new("Entity", new List<string> { "Apple", "Banana" }),
        };

        var results = FuzzySearch.PerformFuzzySearch(data, "Apple", builder);
        Assert.NotEmpty(results);
    }

    // ── SearchPropertyBuilder<T> ────────────────────────────────────────────
    [Fact]
    public void SearchPropertyBuilder_Add_RegistersProperty()
    {
        var builder = new SearchPropertyBuilder<Item>()
            .Add(x => x.Name, 1.0, FuzzynessLevel.Medium);

        var props = builder.Build();
        Assert.Single(props);
        Assert.True(props.ContainsKey("Name"));
    }

    [Fact]
    public void SearchPropertyBuilder_AddMultiple_RegistersAllProperties()
    {
        var builder = new SearchPropertyBuilder<Item>()
            .Add(x => x.Name, 1.0, FuzzynessLevel.Medium)
            .Add(x => x.Description, 0.5, FuzzynessLevel.Low);

        var props = builder.Build();
        Assert.Equal(2, props.Count);
        Assert.True(props.ContainsKey("Name"));
        Assert.True(props.ContainsKey("Description"));
    }

    [Fact]
    public void SearchPropertyBuilder_WeightStoredCorrectly()
    {
        var builder = new SearchPropertyBuilder<Item>()
            .Add(x => x.Name, 0.75, FuzzynessLevel.Low);

        var (_, weight, _) = builder.Build()["Name"];
        Assert.Equal(0.75, weight);
    }

    [Fact]
    public void SearchPropertyBuilder_FuzzynessStoredCorrectly()
    {
        var builder = new SearchPropertyBuilder<Item>()
            .Add(x => x.Name, 1.0, FuzzynessLevel.Low);

        var (_, _, fuzz) = builder.Build()["Name"];
        Assert.Equal(FuzzynessLevel.Low, fuzz);
    }

    [Fact]
    public void SearchPropertyBuilder_AddCollection_Combined_StoresWithCombinedSuffix()
    {
        var builder = new SearchPropertyBuilder<ItemWithTags>()
            .AddCollection(x => x.Tags, t => t, 1.0, FuzzynessLevel.Medium, detailed: false);

        var props = builder.Build();
        Assert.Single(props);
        Assert.Contains("(Combined)", props.Keys.First());
    }

    [Fact]
    public void SearchPropertyBuilder_AddCollection_Detailed_StoresWithDetailedSuffix()
    {
        var builder = new SearchPropertyBuilder<ItemWithTags>()
            .AddCollection(x => x.Tags, t => t, 1.0, FuzzynessLevel.Medium, detailed: true);

        var props = builder.Build();
        Assert.Single(props);
        Assert.Contains("(Detailed)", props.Keys.First());
    }

    [Fact]
    public void SearchPropertyBuilder_SelectorIsCallable()
    {
        var item = new Item("Apple", "Fruit");
        var builder = new SearchPropertyBuilder<Item>()
            .Add(x => x.Name, 1.0, FuzzynessLevel.High);

        var (selector, _, _) = builder.Build()["Name"];
        Assert.Equal("Apple", selector(item));
    }
}
