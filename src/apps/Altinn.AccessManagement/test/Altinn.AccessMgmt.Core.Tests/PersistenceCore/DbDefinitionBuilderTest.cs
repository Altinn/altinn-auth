using Altinn.AccessMgmt.Persistence.Core.Definitions;

namespace Altinn.AccessMgmt.Core.Tests.PersistenceCore;

/// <summary>
/// Pure unit tests for <see cref="DbDefinitionBuilder{T}"/> fluent API — no database required.
/// </summary>
public class DbDefinitionBuilderTest
{
    // ── test model ───────────────────────────────────────────────────────────

    private class SampleModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int Count { get; set; }
    }

    // ── Build() ──────────────────────────────────────────────────────────────

    [Fact]
    public void Build_DefaultBuilder_ReturnsDefinitionWithCorrectModelType()
    {
        var def = new DbDefinitionBuilder<SampleModel>().Build();

        def.ModelType.Should().Be(typeof(SampleModel));
    }

    [Fact]
    public void Build_DefaultBuilder_DefaultsToTableType()
    {
        var def = new DbDefinitionBuilder<SampleModel>().Build();

        def.DefinitionType.Should().Be(DbDefinitionType.Table);
    }

    [Fact]
    public void Build_DefaultBuilder_VersionIsOne()
    {
        var def = new DbDefinitionBuilder<SampleModel>().Build();

        def.Version.Should().Be(1);
    }

    // ── SetVersion ───────────────────────────────────────────────────────────

    [Fact]
    public void SetVersion_CustomVersion_IsReflectedInDefinition()
    {
        var def = new DbDefinitionBuilder<SampleModel>()
            .SetVersion(3)
            .Build();

        def.Version.Should().Be(3);
    }

    // ── SetType ──────────────────────────────────────────────────────────────

    [Fact]
    public void SetType_View_IsReflectedInDefinition()
    {
        var def = new DbDefinitionBuilder<SampleModel>()
            .SetType(DbDefinitionType.View)
            .Build();

        def.DefinitionType.Should().Be(DbDefinitionType.View);
    }

    // ── SetQuery ─────────────────────────────────────────────────────────────

    [Fact]
    public void SetQuery_WithQuery_IsReflectedInDefinition()
    {
        const string sql = "SELECT * FROM sample";
        var def = new DbDefinitionBuilder<SampleModel>()
            .SetQuery(sql)
            .Build();

        def.Query.Should().Be(sql);
    }

    [Fact]
    public void SetQuery_WithExtendedQuery_BothAreStored()
    {
        const string basic = "SELECT id FROM sample";
        const string extended = "SELECT * FROM sample JOIN other ON sample.id = other.id";
        var def = new DbDefinitionBuilder<SampleModel>()
            .SetQuery(basic, extended)
            .Build();

        def.Query.Should().Be(basic);
        def.ExtendedQuery.Should().Be(extended);
    }

    // ── EnableTranslation ────────────────────────────────────────────────────

    [Fact]
    public void EnableTranslation_Default_DisabledByDefault()
    {
        var def = new DbDefinitionBuilder<SampleModel>().Build();
        def.EnableTranslation.Should().BeFalse();
    }

    [Fact]
    public void EnableTranslation_True_IsReflectedInDefinition()
    {
        var def = new DbDefinitionBuilder<SampleModel>()
            .EnableTranslation()
            .Build();

        def.EnableTranslation.Should().BeTrue();
    }

    [Fact]
    public void EnableTranslation_False_RemainsDisabled()
    {
        var def = new DbDefinitionBuilder<SampleModel>()
            .EnableTranslation(false)
            .Build();

        def.EnableTranslation.Should().BeFalse();
    }

    // ── EnableAudit ──────────────────────────────────────────────────────────

    [Fact]
    public void EnableAudit_True_IsReflectedInDefinition()
    {
        var def = new DbDefinitionBuilder<SampleModel>()
            .EnableAudit()
            .Build();

        def.EnableAudit.Should().BeTrue();
    }

    // ── RegisterProperty ─────────────────────────────────────────────────────

    [Fact]
    public void RegisterProperty_SinglePrimitive_AddsOneProperty()
    {
        var def = new DbDefinitionBuilder<SampleModel>()
            .RegisterProperty(m => m.Name)
            .Build();

        def.Properties.Should().HaveCount(1);
        def.Properties[0].Name.Should().Be(nameof(SampleModel.Name));
    }

    [Fact]
    public void RegisterProperty_MultiplePrimitives_AllRegistered()
    {
        var def = new DbDefinitionBuilder<SampleModel>()
            .RegisterProperty(m => m.Id)
            .RegisterProperty(m => m.Name)
            .RegisterProperty(m => m.Count)
            .Build();

        def.Properties.Should().HaveCount(3);
        def.Properties.Select(p => p.Name).Should().BeEquivalentTo(
            nameof(SampleModel.Id),
            nameof(SampleModel.Name),
            nameof(SampleModel.Count));
    }

    [Fact]
    public void RegisterProperty_Nullable_IsMarkedNullable()
    {
        var def = new DbDefinitionBuilder<SampleModel>()
            .RegisterProperty(m => m.Name, nullable: true)
            .Build();

        def.Properties[0].IsNullable.Should().BeTrue();
    }

    [Fact]
    public void RegisterProperty_NotNullable_IsMarkedNotNullable()
    {
        var def = new DbDefinitionBuilder<SampleModel>()
            .RegisterProperty(m => m.Name, nullable: false)
            .Build();

        def.Properties[0].IsNullable.Should().BeFalse();
    }

    // ── AddManualDependency ───────────────────────────────────────────────────

    [Fact]
    public void AddManualDependency_AddsTypeToManualDependencies()
    {
        var def = new DbDefinitionBuilder<SampleModel>()
            .AddManualDependency<string>()
            .Build();

        def.ManualDependencies.Should().Contain(typeof(string));
    }

    // ── fluent chaining returns same builder ─────────────────────────────────

    [Fact]
    public void FluentChaining_MultipleOperations_AllApplied()
    {
        var def = new DbDefinitionBuilder<SampleModel>()
            .SetVersion(2)
            .SetType(DbDefinitionType.View)
            .EnableTranslation()
            .EnableAudit()
            .RegisterProperty(m => m.Id)
            .RegisterProperty(m => m.Name)
            .Build();

        def.Version.Should().Be(2);
        def.DefinitionType.Should().Be(DbDefinitionType.View);
        def.EnableTranslation.Should().BeTrue();
        def.EnableAudit.Should().BeTrue();
        def.Properties.Should().HaveCount(2);
    }
}
