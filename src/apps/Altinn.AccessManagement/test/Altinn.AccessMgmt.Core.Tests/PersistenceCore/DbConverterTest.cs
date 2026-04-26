using System.Data;
using Altinn.AccessMgmt.Persistence.Core.Utilities;

namespace Altinn.AccessMgmt.Core.Tests.PersistenceCore;

/// <summary>
/// Pure unit tests for <see cref="DbConverter.ConvertToResult{T}"/> — no database required.
/// Uses <see cref="DataTable"/>/<see cref="DataTableReader"/> to drive the reader interface.
/// </summary>
public class DbConverterTest
{
    // ── test models ───────────────────────────────────────────────────────────

    private sealed class FlatModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int Count { get; set; }
    }

    private sealed class NullableModel
    {
        public Guid? OptionalId { get; set; }
        public string Label { get; set; }
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static IDataReader MakeReader(DataTable table) => table.CreateDataReader();

    // ── tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public void ConvertToResult_EmptyReader_ReturnsEmptyData()
    {
        var table = new DataTable();
        table.Columns.Add("id", typeof(string));
        table.Columns.Add("name", typeof(string));
        table.Columns.Add("count", typeof(int));

        var result = DbConverter.Instance.ConvertToResult<FlatModel>(MakeReader(table));

        result.Data.Should().BeEmpty();
    }

    [Fact]
    public void ConvertToResult_SingleRow_MapsAllProperties()
    {
        var id = Guid.NewGuid();
        var table = new DataTable();
        table.Columns.Add("id", typeof(string));
        table.Columns.Add("name", typeof(string));
        table.Columns.Add("count", typeof(int));
        table.Rows.Add(id.ToString(), "Hello", 42);

        var result = DbConverter.Instance.ConvertToResult<FlatModel>(MakeReader(table));

        result.Data.Should().ContainSingle();
        var item = result.Data.Single();
        item.Id.Should().Be(id);
        item.Name.Should().Be("Hello");
        item.Count.Should().Be(42);
    }

    [Fact]
    public void ConvertToResult_MultipleRows_MapsEachRow()
    {
        var table = new DataTable();
        table.Columns.Add("id", typeof(string));
        table.Columns.Add("name", typeof(string));
        table.Columns.Add("count", typeof(int));
        table.Rows.Add(Guid.NewGuid().ToString(), "First", 1);
        table.Rows.Add(Guid.NewGuid().ToString(), "Second", 2);
        table.Rows.Add(Guid.NewGuid().ToString(), "Third", 3);

        var result = DbConverter.Instance.ConvertToResult<FlatModel>(MakeReader(table));

        result.Data.Should().HaveCount(3);
        result.Data.Select(r => r.Name).Should().BeEquivalentTo(["First", "Second", "Third"]);
    }

    [Fact]
    public void ConvertToResult_ColumnNamesAreCaseInsensitive()
    {
        var id = Guid.NewGuid();
        var table = new DataTable();
        table.Columns.Add("ID", typeof(string));
        table.Columns.Add("NAME", typeof(string));
        table.Columns.Add("COUNT", typeof(int));
        table.Rows.Add(id.ToString(), "CaseTest", 7);

        var result = DbConverter.Instance.ConvertToResult<FlatModel>(MakeReader(table));

        var item = result.Data.Single();
        item.Id.Should().Be(id);
        item.Name.Should().Be("CaseTest");
        item.Count.Should().Be(7);
    }

    [Fact]
    public void ConvertToResult_NullStringColumn_LeavesStringPropertyNull()
    {
        var id = Guid.NewGuid();
        var table = new DataTable();
        table.Columns.Add("id", typeof(string));
        table.Columns.Add("name", typeof(string));
        table.Columns.Add("count", typeof(int));
        table.Rows.Add(id.ToString(), DBNull.Value, 0);

        var result = DbConverter.Instance.ConvertToResult<FlatModel>(MakeReader(table));

        var item = result.Data.Single();
        item.Name.Should().BeNull();
    }

    [Fact]
    public void ConvertToResult_NullableGuid_NullValue_LeavesPropertyNull()
    {
        var table = new DataTable();
        table.Columns.Add("optionalid", typeof(string));
        table.Columns.Add("label", typeof(string));
        table.Rows.Add(DBNull.Value, "NoId");

        var result = DbConverter.Instance.ConvertToResult<NullableModel>(MakeReader(table));

        var item = result.Data.Single();
        item.OptionalId.Should().BeNull();
        item.Label.Should().Be("NoId");
    }

    [Fact]
    public void ConvertToResult_NullableGuid_ValidValue_ParsedCorrectly()
    {
        var id = Guid.NewGuid();
        var table = new DataTable();
        table.Columns.Add("optionalid", typeof(string));
        table.Columns.Add("label", typeof(string));
        table.Rows.Add(id.ToString(), "HasId");

        var result = DbConverter.Instance.ConvertToResult<NullableModel>(MakeReader(table));

        var item = result.Data.Single();
        item.OptionalId.Should().Be(id);
    }

    [Fact]
    public void ConvertToResult_RownumberColumn_SetsFirstAndLastRowOnPage()
    {
        var table = new DataTable();
        table.Columns.Add("id", typeof(string));
        table.Columns.Add("name", typeof(string));
        table.Columns.Add("count", typeof(int));
        table.Columns.Add("_rownumber", typeof(int));
        table.Rows.Add(Guid.NewGuid().ToString(), "A", 1, 3);
        table.Rows.Add(Guid.NewGuid().ToString(), "B", 2, 4);

        var result = DbConverter.Instance.ConvertToResult<FlatModel>(MakeReader(table));

        result.Page.FirstRowOnPage.Should().Be(3);
        result.Page.LastRowOnPage.Should().Be(4);
    }

    [Fact]
    public void ConvertToResult_UnknownColumns_AreIgnoredGracefully()
    {
        var id = Guid.NewGuid();
        var table = new DataTable();
        table.Columns.Add("id", typeof(string));
        table.Columns.Add("name", typeof(string));
        table.Columns.Add("count", typeof(int));
        table.Columns.Add("unknown_extra_column", typeof(string));
        table.Rows.Add(id.ToString(), "Known", 5, "ignored");

        var result = DbConverter.Instance.ConvertToResult<FlatModel>(MakeReader(table));

        var item = result.Data.Single();
        item.Id.Should().Be(id);
        item.Name.Should().Be("Known");
    }

    [Fact]
    public void PreloadCache_DoesNotThrow()
    {
        var act = () => DbConverter.Instance.PreloadCache(typeof(FlatModel), typeof(NullableModel));
        act.Should().NotThrow();
    }
}
