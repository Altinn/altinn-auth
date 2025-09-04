#nullable enable

using Altinn.Authorization.Models.ResourceRegistry;

namespace Altinn.ResourceRegistry.Models;

/// <summary>
/// Represents public access list metadata.
/// </summary>
/// <param name="Urn">The Accesslist Urn</param>
/// <param name="Identifier">The access list identifier</param>
/// <param name="Name">The access list name</param>
/// <param name="Description">The access list description</param>
public record AccessListInfoDto(
    AccessListUrn Urn, 
    string Identifier,
    string Name,
    string Description
  );
