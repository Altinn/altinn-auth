using Altinn.ResourceRegistry.TestUtils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace Altinn.ResourceRegistry.Persistence.Tests;

/// <summary>
/// Tests for the tx_nextval/tx_max_safeval functions used by the resource change feed,
/// ported from altinn-register (PostgreSqlFunctionsTests).
/// </summary>
public class PostgreSqlFunctionsTests : DbTests
{
    private const string SequenceName = "resourceregistry.resource_change_id_seq";

    public PostgreSqlFunctionsTests(DbFixture dbFixture)
        : base(dbFixture)
    {
    }

    protected NpgsqlDataSource DataSource => Services.GetRequiredService<NpgsqlDataSource>();

    protected override void ConfigureHost(IHostApplicationBuilder builder)
    {
        builder.AddResourceRegistryRepository();
        base.ConfigureHost(builder);
    }

    [Fact]
    public async Task TxMaxSafeval_Returns_Max_When_No_Concurrent_Transactions()
    {
        var value = await TxMaxSafeval();

        value.Should().Be(long.MaxValue);
    }

    [Fact]
    public async Task TxNextval_Returns_Distinct_Increasing_Values_Across_Concurrent_Transactions()
    {
        await using var tx1 = await OpenTransaction();
        await using var tx2 = await OpenTransaction();
        await using var tx3 = await OpenTransaction();

        List<long> values = new();
        for (var i = 0; i < 3; i++)
        {
            values.Add(await TxNextval(tx1));
            values.Add(await TxNextval(tx2));
            values.Add(await TxNextval(tx3));
        }

        values.Should().OnlyHaveUniqueItems();
        values.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task TxMaxSafeval_Returns_Value_Before_Smallest_InFlight_Transaction()
    {
        await using var tx1 = await OpenTransaction();
        await using var tx2 = await OpenTransaction();
        await using var tx3 = await OpenTransaction();

        var nv1 = await TxNextval(tx1);
        var nv2 = await TxNextval(tx2);
        var nv3 = await TxNextval(tx3);

        nv1.Should().BeLessThan(nv2);
        nv2.Should().BeLessThan(nv3);

        (await TxMaxSafeval()).Should().Be(nv1 - 1);

        // an out-of-order commit must not advance past the still-running transaction holding nv1
        await tx2.CommitAsync();
        (await TxMaxSafeval()).Should().Be(nv1 - 1);

        await tx1.CommitAsync();
        (await TxMaxSafeval()).Should().Be(nv3 - 1);

        await tx3.CommitAsync();
        (await TxMaxSafeval()).Should().Be(long.MaxValue);
    }

    [Fact]
    public async Task TxMaxSafeval_Handles_Sequence_Values_Past_32_Bits()
    {
        // Regression test: the bigint advisory lock key is split across classid (high 32 bits)
        // and objid (low 32 bits) in pg_locks, and tx_max_safeval must reconstruct it.
        const long FirstValuePast32Bits = 4294967297L;

        await SetNextSequenceValue(FirstValuePast32Bits);

        await using var tx = await OpenTransaction();
        var nv = await TxNextval(tx);

        nv.Should().Be(FirstValuePast32Bits);
        (await TxMaxSafeval()).Should().Be(nv - 1);
    }

    private async Task<Tx> OpenTransaction()
    {
        var conn = await DataSource.OpenConnectionAsync();
        var tx = await conn.BeginTransactionAsync();
        return new Tx(conn, tx);
    }

    private async Task<long> TxNextval(Tx tx)
    {
        await using var cmd = tx.Connection.CreateCommand();
        cmd.Transaction = tx.Transaction;
        cmd.CommandText = /*strpsql*/$"SELECT resourceregistry.tx_nextval('{SequenceName}')";

        var result = await cmd.ExecuteScalarAsync();
        return (long)result!;
    }

    private async Task<long> TxMaxSafeval()
    {
        await using var conn = await DataSource.OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = /*strpsql*/$"SELECT resourceregistry.tx_max_safeval('{SequenceName}')";

        var result = await cmd.ExecuteScalarAsync();
        return (long)result!;
    }

    private async Task SetNextSequenceValue(long value)
    {
        await using var conn = await DataSource.OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = /*strpsql*/$"SELECT setval('{SequenceName}', {value}, false)";

        await cmd.ExecuteScalarAsync();
    }

    private sealed class Tx(NpgsqlConnection connection, NpgsqlTransaction transaction) : IAsyncDisposable
    {
        public NpgsqlConnection Connection => connection;

        public NpgsqlTransaction Transaction => transaction;

        public Task CommitAsync() => transaction.CommitAsync();

        public async ValueTask DisposeAsync()
        {
            await transaction.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
