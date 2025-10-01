#nullable enable

using System.Diagnostics.CodeAnalysis;
using Npgsql;

namespace Altinn.AccessMgmt.PersistenceEF.Extensions;

/// <summary>
/// Helper extensions for Npgsql.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class NpgsqlExtensions
{
    /// <summary>
    /// Reads a column returning value or the specified  default value
    /// </summary>
    /// <typeparam name="T">The element type</typeparam>
    /// <param name="reader">The reader</param>
    /// <param name="name">The column name</param>
    /// <param name="defaultValue">Default value to return of column is null</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The column value or specified defaultValue</returns>
    public static async ValueTask<T> GetFieldValueOrDefaultAsync<T>(
        this NpgsqlDataReader reader,
        string name,
        T defaultValue,
        CancellationToken cancellationToken = default)
    {
        var ordinal = reader.GetOrdinal(name);
        if (await reader.IsDBNullAsync(ordinal, cancellationToken))
        {
            return defaultValue;
        }

        return await reader.GetFieldValueAsync<T>(ordinal, cancellationToken);
    }
}
