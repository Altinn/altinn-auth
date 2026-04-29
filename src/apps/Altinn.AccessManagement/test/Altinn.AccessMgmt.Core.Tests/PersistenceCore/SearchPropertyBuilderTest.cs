using Altinn.AccessMgmt.Persistence.Core.Utilities.Search;

namespace Altinn.AccessMgmt.Core.Tests.PersistenceCore;

/// <summary>
/// Pure unit tests for <see cref="SearchPropertyBuilder{T}"/> — no database, no
/// external dependencies. The builder is used by <c>PackageService.Search</c> to
/// register weighted properties for fuzzy matching, so the dictionary keys it
/// produces are load-bearing: a wrong key silently misweights real search
/// results.
/// </summary>
public class SearchPropertyBuilderTest
{
    // ── test models ──────────────────────────────────────────────────────────

    private class Sample
    {
        public string Name { get; set; }
        public int Count { get; set; }
        public string Description { get; set; }
        public Inner Area { get; set; }
        public IEnumerable<Resource> Resources { get; set; }

        public string ComputeKey() => Name + Count;
    }

    private class Inner
    {
        public string Name { get; set; }
        public Group Group { get; set; }
    }

    private class Group
    {
        public string Name { get; set; }
    }

    private class Resource
    {
        public string Name { get; set; }
    }

    // ── Add — happy path & key derivation ────────────────────────────────────

    [Fact]
    public void Add_SingleProperty_RegistersUnderPropertyName()
    {
        var built = new SearchPropertyBuilder<Sample>()
            .Add(s => s.Name, weight: 2.0, FuzzynessLevel.High)
            .Build();

        built.Should().ContainKey("Name");
        built["Name"].A.Should().Be(2.0);
        built["Name"].FuzzynessLevel.Should().Be(FuzzynessLevel.High);
    }

    [Fact]
    public void Add_NestedMemberExpression_JoinsPropertyPathWithUnderscore()
    {
        // The bug class: a regression in GetFullPropertyName would silently
        // remap nested keys (e.g. "Name" instead of "Area_Group_Name") and
        // break PackageService.Search's per-field weighting at runtime
        // without any compile error or visible test failure elsewhere.
        var built = new SearchPropertyBuilder<Sample>()
            .Add(s => s.Area.Group.Name, weight: 1.5, FuzzynessLevel.Medium)
            .Build();

        built.Should().ContainKey("Area_Group_Name");
        built.Should().NotContainKey("Name");
    }

    [Fact]
    public void Add_TwoLevelNestedExpression_JoinsBothLevels()
    {
        var built = new SearchPropertyBuilder<Sample>()
            .Add(s => s.Area.Name, weight: 1.0, FuzzynessLevel.Low)
            .Build();

        built.Should().ContainKey("Area_Name");
    }

    [Fact]
    public void Add_ValueTypeMember_BoxedAsUnaryExpression_StillExtractsMemberName()
    {
        // When T is a reference type and the selector returns a value-type
        // property, the C# compiler wraps the access in a Convert/Box
        // UnaryExpression. The builder's GetPropertyName has a dedicated
        // branch for that — verify it doesn't fall through to "UnknownProperty".
        var built = new SearchPropertyBuilder<Sample>()
            .Add(s => s.Count, weight: 1.0, FuzzynessLevel.Low)
            .Build();

        built.Should().ContainKey("Count");
        built.Should().NotContainKey("UnknownProperty");
    }

    [Fact]
    public void Add_MethodCallExpression_RegistersUnderMethodName()
    {
        // pkg => pkg.GetSomething() falls through the member-/unary-expression
        // branches and into the MethodCallExpression branch, which uses the
        // method name as the key. Documented behavior — pin it down.
        var built = new SearchPropertyBuilder<Sample>()
            .Add(s => s.ComputeKey(), weight: 1.0, FuzzynessLevel.Low)
            .Build();

        built.Should().ContainKey("ComputeKey");
    }

    [Fact]
    public void Add_SamePropertyTwice_SecondAddOverwritesFirst()
    {
        // Documents the replace-not-accumulate semantics of the internal
        // dictionary. If a future change accidentally appended (e.g. switched
        // to a multi-map) PackageService.Search's weighting would double up
        // for repeated registrations — this test pins the contract.
        var built = new SearchPropertyBuilder<Sample>()
            .Add(s => s.Name, weight: 1.0, FuzzynessLevel.Low)
            .Add(s => s.Name, weight: 5.0, FuzzynessLevel.High)
            .Build();

        built.Should().HaveCount(1);
        built["Name"].A.Should().Be(5.0);
        built["Name"].FuzzynessLevel.Should().Be(FuzzynessLevel.High);
    }

    [Fact]
    public void Add_DifferentExpressions_ProduceDistinctKeysWithoutCollision()
    {
        var built = new SearchPropertyBuilder<Sample>()
            .Add(s => s.Name, weight: 1.0, FuzzynessLevel.Low)
            .Add(s => s.Description, weight: 2.0, FuzzynessLevel.Medium)
            .Add(s => s.Area.Name, weight: 3.0, FuzzynessLevel.High)
            .Build();

        built.Keys.Should().BeEquivalentTo(new[] { "Name", "Description", "Area_Name" });
        built["Name"].A.Should().Be(1.0);
        built["Description"].A.Should().Be(2.0);
        built["Area_Name"].A.Should().Be(3.0);
    }

    [Fact]
    public void Add_RegisteredSelector_EvaluatesAgainstActualInstance()
    {
        // The compiled selector on the dictionary value must actually run
        // against the bound instance — a regression where Compile() returned
        // a stale closure or threw at runtime would fail this test before
        // it could silently break Search.
        var built = new SearchPropertyBuilder<Sample>()
            .Add(s => s.Name, weight: 1.0, FuzzynessLevel.Low)
            .Build();

        var sample = new Sample { Name = "Skattegrunnlag" };
        var value = built["Name"].Callback(sample);

        value.Should().Be("Skattegrunnlag");
    }

    [Fact]
    public void Add_NestedMemberSelector_EvaluatesThroughTheChain()
    {
        var built = new SearchPropertyBuilder<Sample>()
            .Add(s => s.Area.Group.Name, weight: 1.0, FuzzynessLevel.Low)
            .Build();

        var sample = new Sample
        {
            Area = new Inner { Group = new Group { Name = "Bransje" } },
        };

        built["Area_Group_Name"].Callback(sample).Should().Be("Bransje");
    }

    // ── AddCollection — combined vs detailed mode ────────────────────────────

    [Fact]
    public void AddCollection_DefaultMode_RegistersUnderPropertyNameSuffixedCombined()
    {
        var built = new SearchPropertyBuilder<Sample>()
            .AddCollection(s => s.Resources, r => r.Name, weight: 1.2, FuzzynessLevel.High)
            .Build();

        built.Should().ContainKey("Resources (Combined)");
        built.Should().NotContainKey("Resources (Detailed)");
    }

    [Fact]
    public void AddCollection_DetailedMode_RegistersUnderPropertyNameSuffixedDetailed()
    {
        var built = new SearchPropertyBuilder<Sample>()
            .AddCollection(s => s.Resources, r => r.Name, weight: 1.2, FuzzynessLevel.High, detailed: true)
            .Build();

        built.Should().ContainKey("Resources (Detailed)");
        built.Should().NotContainKey("Resources (Combined)");
    }

    [Fact]
    public void AddCollection_CombinedMode_JoinsItemsWithCommaSpace()
    {
        // The bug class: a wrong join string here would silently change what
        // FuzzySearch matches against — e.g. "First, Second" matches "First,"
        // but "First|Second" doesn't, and PackageService.Search would lose
        // hits on resource collections.
        var built = new SearchPropertyBuilder<Sample>()
            .AddCollection(s => s.Resources, r => r.Name, weight: 1.0, FuzzynessLevel.Low)
            .Build();

        var sample = new Sample
        {
            Resources = new[] { new Resource { Name = "First" }, new Resource { Name = "Second" } },
        };

        built["Resources (Combined)"].Callback(sample).Should().Be("First, Second");
    }

    [Fact]
    public void AddCollection_DetailedMode_JoinsItemsWithSpacePipeSpace()
    {
        var built = new SearchPropertyBuilder<Sample>()
            .AddCollection(s => s.Resources, r => r.Name, weight: 1.0, FuzzynessLevel.Low, detailed: true)
            .Build();

        var sample = new Sample
        {
            Resources = new[] { new Resource { Name = "First" }, new Resource { Name = "Second" } },
        };

        built["Resources (Detailed)"].Callback(sample).Should().Be("First | Second");
    }

    [Fact]
    public void AddCollection_EmptyCollection_ProducesEmptyJoinedString()
    {
        var built = new SearchPropertyBuilder<Sample>()
            .AddCollection(s => s.Resources, r => r.Name, weight: 1.0, FuzzynessLevel.Low)
            .Build();

        var sample = new Sample { Resources = Array.Empty<Resource>() };

        built["Resources (Combined)"].Callback(sample).Should().Be(string.Empty);
    }

    [Fact]
    public void AddCollection_BothModesOnSameProperty_RegisterUnderDistinctKeys()
    {
        // A caller may want both combined-and-detailed weighting on the same
        // collection. The suffix scheme makes that work; pin it.
        var built = new SearchPropertyBuilder<Sample>()
            .AddCollection(s => s.Resources, r => r.Name, weight: 1.0, FuzzynessLevel.Low)
            .AddCollection(s => s.Resources, r => r.Name, weight: 2.0, FuzzynessLevel.High, detailed: true)
            .Build();

        built.Keys.Should().BeEquivalentTo(new[] { "Resources (Combined)", "Resources (Detailed)" });
        built["Resources (Combined)"].A.Should().Be(1.0);
        built["Resources (Detailed)"].A.Should().Be(2.0);
    }

    // ── Fluent contract ──────────────────────────────────────────────────────

    [Fact]
    public void Add_ReturnsSameBuilderInstance_ForChaining()
    {
        var builder = new SearchPropertyBuilder<Sample>();
        var returned = builder.Add(s => s.Name, weight: 1.0, FuzzynessLevel.Low);

        returned.Should().BeSameAs(builder);
    }

    [Fact]
    public void AddCollection_ReturnsSameBuilderInstance_ForChaining()
    {
        var builder = new SearchPropertyBuilder<Sample>();
        var returned = builder.AddCollection(s => s.Resources, r => r.Name, weight: 1.0, FuzzynessLevel.Low);

        returned.Should().BeSameAs(builder);
    }

    // ── Build() — empty + structural ─────────────────────────────────────────

    [Fact]
    public void Build_OnEmptyBuilder_ReturnsEmptyDictionary()
    {
        var built = new SearchPropertyBuilder<Sample>().Build();

        built.Should().NotBeNull();
        built.Should().BeEmpty();
    }

    [Fact]
    public void Build_ReturnsLiveInternalDictionary_NotASnapshot()
    {
        // Documents the (somewhat surprising) mutability boundary: Build()
        // hands back the internal dictionary, so a subsequent Add on the
        // builder is visible through the previously-returned reference.
        // PackageService.Search holds the dictionary briefly and doesn't
        // mutate it, so the current contract is fine — but pinning it
        // means a future change to defensive-copy here is a deliberate
        // decision rather than a silent regression.
        var builder = new SearchPropertyBuilder<Sample>();
        var first = builder.Build();
        builder.Add(s => s.Name, weight: 1.0, FuzzynessLevel.Low);

        first.Should().ContainKey("Name");
    }
}
