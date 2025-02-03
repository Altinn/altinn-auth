using CommunityToolkit.Diagnostics;
using Npgsql;
using System.Collections;
using System.Collections.Immutable;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Altinn.Authorization.Cli.Database.Metadata;

/// <summary>
/// Represents a graph of tables.
/// </summary>
[ExcludeFromCodeCoverage]
[DebuggerDisplay("Count = {Count,nq}")]
public sealed class TableGraph
    : IReadOnlyList<TableGraph.Node>
{
    private readonly ImmutableArray<Node> _nodes;

    private TableGraph(ImmutableArray<Node> nodes)
    {
        Guard.IsNotDefault(nodes);

        _nodes = nodes;
    }

    /// <inheritdoc/>
    public int Count => _nodes.Length;

    /// <inheritdoc/>
    public Node this[int index] => _nodes[index];

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator()"/>
    public ImmutableArray<Node>.Enumerator GetEnumerator()
        => _nodes.GetEnumerator();

    IEnumerator<Node> IEnumerable<Node>.GetEnumerator()
        => ((IEnumerable<Node>)_nodes).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => ((IEnumerable)_nodes).GetEnumerator();

    /// <summary>
    /// Represents a node in a table graph.
    /// </summary>
    public sealed class Node
        : TableInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Node"/> class.
        /// </summary>
        /// <param name="schema">The schema name.</param>
        /// <param name="name">The table name.</param>
        /// <param name="estimatedRowCount">The estimated row count.</param>
        /// <param name="columnNames">The column names.</param>
        /// <param name="foreignKeyTables">The list of foreign key tables.</param>
        public Node(string schema, string name, long estimatedRowCount, IEnumerable<string> columnNames, IEnumerable<Node> foreignKeyTables)
            : base(schema, name, columnNames, estimatedRowCount)
        {
            ForeignKeyTables = foreignKeyTables.ToImmutableArray();
        }

        /// <summary>
        /// Gets tables the current table references with foreign keys.
        /// </summary>
        public ImmutableArray<Node> ForeignKeyTables { get; }

        /// <summary>
        /// Gets the estimated number of rows in the table.
        /// </summary>
        public new long EstimatedTableRows => base.EstimatedTableRows!.Value;
    }

    /// <summary>
    /// Builds a table graph for a schema.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="schemaName">The schema name.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="TableGraph"/>.</returns>
    public static async Task<TableGraph> GetAsync(
        NpgsqlConnection connection,
        string schemaName,
        CancellationToken cancellationToken = default)
    {
        const string QUERY =
            /*strpsql*/"""
            WITH "tables" AS (
            	SELECT t.table_name
            	FROM information_schema."tables" t
            	WHERE t.table_schema = @schema
            ), "table_deps" AS (
            	SELECT DISTINCT
            		kcu.table_name "from",
            		rel_tco.table_name "to"
            	FROM
            		information_schema.table_constraints tco
            	JOIN information_schema.key_column_usage kcu
            		ON tco.constraint_schema = kcu.constraint_schema
            		AND tco.constraint_name = kcu.constraint_name
            	JOIN information_schema.referential_constraints rco 
            		ON tco.constraint_schema = rco.constraint_schema
            		AND tco.constraint_name = rco.constraint_name
            	JOIN information_schema.table_constraints rel_tco 
            		ON rco.unique_constraint_schema = rel_tco.constraint_schema 
            		AND rco.unique_constraint_name = rel_tco.constraint_name
            	WHERE
            			tco.constraint_type = 'FOREIGN KEY'
            		AND kcu.table_schema = @schema
            		AND rel_tco.table_schema = @schema
            ), "est_rows" AS (
                SELECT
                    relname "table_name",
                    relnamespace::regnamespace::text "schema",
                    reltuples::bigint estimated_rows
                FROM pg_catalog.pg_class
            ), "columns" AS (
                SELECT
                    c.table_name,
                    c.column_name,
                    c.ordinal_position
                FROM information_schema.columns c
                WHERE c.table_schema = @schema
            )
            SELECT 
                t.table_name "name",
                er.estimated_rows "est_rows",
                array_agg(c.column_name ORDER BY c.ordinal_position) "columns",
                array_remove(array_agg(DISTINCT td."to"), NULL) "deps"
            FROM "tables" t
            JOIN "est_rows" er ON t.table_name = er.table_name and er."schema" = @schema
            LEFT JOIN "table_deps" td ON td."from" = t.table_name
            LEFT JOIN "columns" c ON c.table_name = t.table_name
            GROUP BY t.table_name, er.estimated_rows
            """;

        Guard.IsNotNull(connection);
        Guard.IsNotNullOrWhiteSpace(schemaName);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = QUERY;

        cmd.Parameters.AddWithValue("schema", schemaName);

        await cmd.PrepareAsync(cancellationToken);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        var results = new List<(string Name, long EstimatedRows, string[] Columns, string[] Deps)>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var name = reader.GetString("name");
            var rows = reader.GetInt64("est_rows");
            var cols = reader.GetFieldValue<string[]>("columns");
            var deps = reader.GetFieldValue<string[]>("deps");
            results.Add((name, rows, cols, deps));
        }

        // sort by deps, fewest first
        results.Sort((a, b) => a.Deps.Length.CompareTo(b.Deps.Length));

        var lookup = new Dictionary<string, Node>();
        var builder = ImmutableArray.CreateBuilder<Node>(results.Count);

        while (builder.Count < results.Count)
        {
            var added = false;
            foreach (var (table, estRows, cols, deps) in results) 
            {
                ref var nodeRef = ref CollectionsMarshal.GetValueRefOrAddDefault(lookup, table, out var exists);
                if (exists)
                {
                    // already added
                    continue;
                }

                if (deps.All(d => lookup.ContainsKey(d)))
                {
                    var node = new Node(
                        schemaName,
                        table,
                        estRows,
                        cols,
                        deps.Select(d => lookup[d]));

                    builder.Add(node);
                    nodeRef = node;
                    added = true;
                }
            }

            if (!added)
            {
                ThrowHelper.ThrowInvalidOperationException($"Failed to build table graph for schema \"{schemaName}\".");
            }
        }

        return new TableGraph(builder.MoveToImmutable());
    }
}
