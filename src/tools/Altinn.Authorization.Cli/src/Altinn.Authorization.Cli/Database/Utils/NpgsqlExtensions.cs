using System.Diagnostics.CodeAnalysis;
using Npgsql;

namespace Altinn.Authorization.Cli.Database.Utils;

/// <summary>
/// Extension methods for Npgsql.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class NpgsqlExtensions
{
    /// <summary>
    /// Gets a nullable field value from the reader.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="reader">The reader.</param>
    /// <param name="ordinal">The column ordinal.</param>
    /// <param name="defaultValue">The default value.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>The field value, or <see langword="null"/>.</returns>
    public static async Task<T?> GetNullableFieldValueAsync<T>(this NpgsqlDataReader reader, int ordinal, T? defaultValue, CancellationToken cancellationToken = default)
    {
        if (await reader.IsDBNullAsync(ordinal, cancellationToken))
        {
            return defaultValue;
        }

        return await reader.GetFieldValueAsync<T>(ordinal, cancellationToken);
    }

    /// <summary>
    /// Gets a nullable field value from the reader.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="reader">The reader.</param>
    /// <param name="ordinal">The column ordinal.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>The field value, or <see langword="null"/>.</returns>
    public static Task<T?> GetNullableFieldValueAsync<T>(this NpgsqlDataReader reader, int ordinal, CancellationToken cancellationToken = default)
        => GetNullableFieldValueAsync<T>(reader, ordinal, default, cancellationToken);
}
