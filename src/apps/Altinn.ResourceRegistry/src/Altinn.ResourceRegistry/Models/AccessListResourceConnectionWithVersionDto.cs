#nullable enable

using System.Text.Json.Serialization;
using Altinn.ResourceRegistry.Core.AccessLists;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.Utils;

namespace Altinn.ResourceRegistry.Models;

/// <summary>
/// A <see cref="AccessListResourceConnectionDto"/> with version information.
/// </summary>
public record AccessListResourceConnectionWithVersionDto(
    string ResourceIdentifier,
    IReadOnlyCollection<string>? ActionFilters,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    [property: JsonIgnore]
    HttpDateTimeHeaderValue VersionModifiedAt,
    [property: JsonIgnore]
    AggregateVersion VersionTag)
    : AccessListResourceConnectionDto(ResourceIdentifier, ActionFilters, CreatedAt, UpdatedAt)
    , IConvertibleFrom<AccessListResourceConnectionWithVersionDto, AccessListData<AccessListResourceConnection>>
    , ITaggedEntity<AggregateVersion>
{
    /// <inheritdoc/>
    public static AccessListResourceConnectionWithVersionDto From(AccessListData<AccessListResourceConnection> value)
        => new(
            value.Value.ResourceIdentifier,
            value.Value.Actions,
            value.Value.Created,
            value.Value.Modified,
            new(value.UpdatedAt),
            new(value.Version));

    /// <inheritdoc/>
    void ITaggedEntity<AggregateVersion>.GetHeaderValues(out AggregateVersion version, out HttpDateTimeHeaderValue modifiedAt)
    {
        version = VersionTag;
        modifiedAt = VersionModifiedAt;
    }
}
