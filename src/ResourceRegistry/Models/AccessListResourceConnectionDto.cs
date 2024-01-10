#nullable enable

namespace Altinn.ResourceRegistry.Models;

/// <summary>
/// Model for creating an access list resource connection.
/// </summary>
/// <param name="ResourceIdentifier">The resource identifier.</param>
/// <param name="Actions">The allowed actions.</param>
/// <param name="CreatedAt">When the connection was created.</param>
/// <param name="UpdatedAt">When the connection was last updated.</param>
public record AccessListResourceConnectionDto(
    string ResourceIdentifier,
    IReadOnlyList<string> Actions,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
