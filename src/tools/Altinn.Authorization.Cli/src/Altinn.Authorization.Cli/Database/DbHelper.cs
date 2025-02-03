using System.Diagnostics.CodeAnalysis;
using Altinn.Authorization.Cli.Database.Metadata;
using CommunityToolkit.Diagnostics;
using Npgsql;

namespace Altinn.Authorization.Cli.Database;

/// <summary>
/// Helper class for working with a database.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class DbHelper
    : IAsyncDisposable
{
    /// <summary>
    /// Creates a new <see cref="DbHelper"/> instance.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>A new <see cref="DbHelper"/> for the <paramref name="connectionString"/>.</returns>
    public static async Task<DbHelper> Create(string connectionString, CancellationToken cancellationToken)
    {
        var connStrBuilder = new NpgsqlConnectionStringBuilder(connectionString);
        connStrBuilder.Pooling = false;
        connStrBuilder.IncludeErrorDetail = true;

        NpgsqlDataSource? source = NpgsqlDataSource.Create(connStrBuilder);
        try
        {
            var conn = await source.OpenConnectionAsync(cancellationToken);
            var helper = new DbHelper(source, conn);
            source = null;

            return helper;
        }
        finally
        {
            if (source is { } s)
            {
                await s.DisposeAsync();
            }
        }
    }

    private readonly NpgsqlDataSource _source;
    private readonly NpgsqlConnection _conn;
    private NpgsqlTransaction? _transaction;

    private DbHelper(
        NpgsqlDataSource source,
        NpgsqlConnection conn)
    {
        _source = source;
        _conn = conn;
    }

    public Task<SchemaInfo> GetSchemaInfo(string schemaName, CancellationToken cancellationToken)
        => SchemaInfo.GetAsync(_conn, schemaName, cancellationToken);

    public NpgsqlCommand CreateCommand(string commandText)
    {
        var cmd = _conn.CreateCommand();
        cmd.CommandText = commandText;
        return cmd;
    }

    public Task<TextReader> BeginTextExport(string copyToCommand, CancellationToken cancellationToken)
        => _conn.BeginTextExportAsync(copyToCommand, cancellationToken);

    public Task<TextWriter> BeginTextImport(string copyFromCommand, CancellationToken cancellationToken)
        => _conn.BeginTextImportAsync(copyFromCommand, cancellationToken);

    public async Task BeginTransaction(CancellationToken cancellationToken)
    {
        if (_transaction is not null)
        {
            ThrowHelper.ThrowInvalidOperationException("Transaction already exists.");
        }

        _transaction = await _conn.BeginTransactionAsync(cancellationToken);
    }

    public async Task Commit(CancellationToken cancellationToken)
    {
        if (_transaction is null)
        {
            ThrowHelper.ThrowInvalidOperationException("No transaction to commit.");
        }

        await _transaction.CommitAsync(cancellationToken);
        _transaction = null;
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    { 
        if (_transaction is { } t)
        {
            await t.DisposeAsync();
        }

        await _conn.DisposeAsync();
        await _source.DisposeAsync();
    }
}
