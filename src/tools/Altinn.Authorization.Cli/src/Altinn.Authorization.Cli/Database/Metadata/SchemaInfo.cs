using CommunityToolkit.Diagnostics;
using Npgsql;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Altinn.Authorization.Cli.Database.Metadata;

/// <summary>
/// Represents information ablut a schema.
/// </summary>
[ExcludeFromCodeCoverage]
[DebuggerDisplay("Schema {Name}")]
public sealed class SchemaInfo
{
    private SchemaInfo(
        string name,
        TableGraph tables,
        ImmutableArray<SequenceInfo> sequences)
    {
        Name = name;
        Tables = tables;
        Sequences = sequences;
    }

    /// <summary>
    /// Gets the schema name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the tables in the schema.
    /// </summary>
    public TableGraph Tables { get; }

    /// <summary>
    /// Gets the sequences in the schema.
    /// </summary>
    public ImmutableArray<SequenceInfo> Sequences { get; }

    /// <summary>
    /// Gets information about a database schema.
    /// </summary>
    /// <param name="connection">The <see cref="NpgsqlConnection"/>.</param>
    /// <param name="schema">The schema name.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>Information about the schema named <paramref name="schema"/>.</returns>
    public static async Task<SchemaInfo> GetAsync(
        NpgsqlConnection connection,
        string schema,
        CancellationToken cancellationToken = default)
    {
        Guard.IsNotNull(connection);
        Guard.IsNotNullOrWhiteSpace(schema);

        var tables = await TableGraph.GetAsync(connection, schema, cancellationToken);
        var sequences = await GetSequencesAsync(connection, schema, cancellationToken);

        return new SchemaInfo(schema, tables, sequences);
    }

    private static async Task<ImmutableArray<SequenceInfo>> GetSequencesAsync(
        NpgsqlConnection connection,
        string schema,
        CancellationToken cancellationToken = default)
    {
        const string QUERY =
            /*strpsql*/"""
            SELECT sequence_name FROM information_schema.sequences
            WHERE sequence_schema = @schema
            """;

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = QUERY;

        cmd.Parameters.AddWithValue("schema", schema);

        await cmd.PrepareAsync(cancellationToken);
        await using var batch = connection.CreateBatch();

        {
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var seqCmd = batch.CreateBatchCommand();
                batch.BatchCommands.Add(seqCmd);

                var name = reader.GetString(0);
                seqCmd.CommandText =
                    /*strpsql*/$"""
                    SELECT last_value, is_called, '{name}' name from
                    {schema}.{name}
                    """;
            }

            if (batch.BatchCommands.Count == 0)
            {
                return [];
            }
        }

        {
            var builder = ImmutableArray.CreateBuilder<SequenceInfo>(batch.BatchCommands.Count);
            await using var reader = await batch.ExecuteReaderAsync(cancellationToken);

            do
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    var last_value = reader.GetInt64(0);
                    var isCalled = reader.GetBoolean(1);
                    var name = reader.GetString(2);

                    builder.Add(new SequenceInfo(schema, name, last_value, isCalled));
                }
            } while (await reader.NextResultAsync(cancellationToken));

            return builder.DrainToImmutable();
        }
    }
}
