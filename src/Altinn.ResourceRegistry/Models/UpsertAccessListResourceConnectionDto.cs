#nullable enable

namespace Altinn.ResourceRegistry.Models;

/// <summary>
/// Model for creating or updateing an access list resource connection.
/// </summary>
/// <param name="Actions">The allowed actions.</param>
public record UpsertAccessListResourceConnectionDto(
    IReadOnlyList<string> Actions);
