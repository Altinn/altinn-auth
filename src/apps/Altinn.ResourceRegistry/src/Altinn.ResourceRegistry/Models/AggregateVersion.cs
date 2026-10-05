#nullable enable

using System.Text.Json.Serialization;
using Altinn.ResourceRegistry.Core.Utils;

namespace Altinn.ResourceRegistry.Models;

/// <summary>
/// An aggregate version.
/// </summary>
/// <param name="Version">The version number.</param>
public record AggregateVersion(
    [property: JsonPropertyName("version")]
    [property: JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)]
    ulong Version)
    : IConvertibleFrom<AggregateVersion, ulong>
{
    /// <inheritdoc/>
    public static AggregateVersion From(ulong version)
        => new(version);
}
