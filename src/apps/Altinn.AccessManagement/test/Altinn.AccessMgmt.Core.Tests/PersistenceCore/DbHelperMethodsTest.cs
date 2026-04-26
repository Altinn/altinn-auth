using Altinn.AccessMgmt.Persistence.Core.Helpers;
using NpgsqlTypes;

namespace Altinn.AccessMgmt.Core.Tests.PersistenceCore;

public class DbHelperMethodsTest
{
    // ── GetPostgresType(Type) ─────────────────────────────────────────────────

    [Fact]
    public void GetPostgresType_String_ReturnsText()
        => DbHelperMethods.GetPostgresType(typeof(string)).Should().Be(NpgsqlDbType.Text);

    [Fact]
    public void GetPostgresType_Int_ReturnsInteger()
        => DbHelperMethods.GetPostgresType(typeof(int)).Should().Be(NpgsqlDbType.Integer);

    [Fact]
    public void GetPostgresType_Long_ReturnsBigint()
        => DbHelperMethods.GetPostgresType(typeof(long)).Should().Be(NpgsqlDbType.Bigint);

    [Fact]
    public void GetPostgresType_Short_ReturnsSmallint()
        => DbHelperMethods.GetPostgresType(typeof(short)).Should().Be(NpgsqlDbType.Smallint);

    [Fact]
    public void GetPostgresType_Guid_ReturnsUuid()
        => DbHelperMethods.GetPostgresType(typeof(Guid)).Should().Be(NpgsqlDbType.Uuid);

    [Fact]
    public void GetPostgresType_Bool_ReturnsBoolean()
        => DbHelperMethods.GetPostgresType(typeof(bool)).Should().Be(NpgsqlDbType.Boolean);

    [Fact]
    public void GetPostgresType_DateTime_ReturnsTimestamp()
        => DbHelperMethods.GetPostgresType(typeof(DateTime)).Should().Be(NpgsqlDbType.Timestamp);

    [Fact]
    public void GetPostgresType_DateTimeOffset_ReturnsTimestampTz()
        => DbHelperMethods.GetPostgresType(typeof(DateTimeOffset)).Should().Be(NpgsqlDbType.TimestampTz);

    [Fact]
    public void GetPostgresType_Float_ReturnsReal()
        => DbHelperMethods.GetPostgresType(typeof(float)).Should().Be(NpgsqlDbType.Real);

    [Fact]
    public void GetPostgresType_Double_ReturnsDouble()
        => DbHelperMethods.GetPostgresType(typeof(double)).Should().Be(NpgsqlDbType.Double);

    [Fact]
    public void GetPostgresType_Decimal_ReturnsNumeric()
        => DbHelperMethods.GetPostgresType(typeof(decimal)).Should().Be(NpgsqlDbType.Numeric);

    [Fact]
    public void GetPostgresType_NullableGuid_ReturnsUuid()
        => DbHelperMethods.GetPostgresType(typeof(Guid?)).Should().Be(NpgsqlDbType.Uuid);

    [Fact]
    public void GetPostgresType_NullableInt_ReturnsInteger()
        => DbHelperMethods.GetPostgresType(typeof(int?)).Should().Be(NpgsqlDbType.Integer);

    [Fact]
    public void GetPostgresType_UnsupportedType_Throws()
    {
        var act = () => DbHelperMethods.GetPostgresType(typeof(object));
        act.Should().Throw<NotSupportedException>();
    }

    // ── GetPostgresType(PropertyInfo) ─────────────────────────────────────────

    [Fact]
    public void GetPostgresType_PropertyInfo_String_ReturnsText()
    {
        var prop = typeof(SampleModel).GetProperty(nameof(SampleModel.Name))!;
        DbHelperMethods.GetPostgresType(prop).Should().Be(NpgsqlDbType.Text);
    }

    [Fact]
    public void GetPostgresType_PropertyInfo_Guid_ReturnsUuid()
    {
        var prop = typeof(SampleModel).GetProperty(nameof(SampleModel.Id))!;
        DbHelperMethods.GetPostgresType(prop).Should().Be(NpgsqlDbType.Uuid);
    }

    private class SampleModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }
}
