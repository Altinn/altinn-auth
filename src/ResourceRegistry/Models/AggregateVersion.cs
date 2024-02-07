#nullable enable

using System.Text.Json.Serialization;

namespace Altinn.ResourceRegistry.Models;

/// <summary>
/// An aggregate version.
/// </summary>
/// <param name="Version">The version number.</param>
public record AggregateVersion(
    [property: JsonPropertyName("version")]
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)]
    ulong Version)
{
    /// <summary>
    /// Create a new <see cref="AggregateVersion"/> from a <see langword="ulong"/>.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <returns>The new <see cref="AggregateVersion"/>.</returns>
    public static AggregateVersion From(ulong version)
        => new(version);
}
