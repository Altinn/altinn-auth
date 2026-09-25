#nullable enable

using System.Text.Json.Serialization;

namespace Altinn.ResourceRegistry.Models;

/// <summary>
/// Model for creating or updating an access list resource connection.
/// </summary>
/// <param name="ActionFilters">The allowed actions - if <see langword="null"/> or empty, all actions will be allowed.</param>
public record UpsertAccessListResourceConnectionDto(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyList<string>? ActionFilters);
