#nullable enable

namespace Altinn.ResourceRegistry.Models;

/// <summary>
/// Model used for creating or updating an access list.
/// </summary>
/// <param name="Name">The party registry name.</param>
/// <param name="Description">The (optional) party registry description.</param>
public record CreateAccessListModel(
    string Name,
    string? Description);
