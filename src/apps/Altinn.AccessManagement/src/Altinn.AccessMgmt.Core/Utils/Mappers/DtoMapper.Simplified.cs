using Altinn.Authorization.Api.Contracts.AccessManagement;

namespace Altinn.AccessMgmt.Core.Utils;

/// <summary>
/// Mapper extension for simplified connections
/// </summary>
public partial class DtoMapper
{
    /// <summary>
    /// Converts a CompactEntityDto to SimplifiedPartyDto, excluding sensitive personal information
    /// </summary>
    /// <param name="entity">The entity to convert</param>
    /// <returns>A simplified party DTO without PersonIdentifier/SSN, or null if entity is null</returns>
    /// <remarks>This method explicitly excludes PersonIdentifier to comply with data privacy requirements</remarks>
    public static SimplifiedPartyDto? ToSimplifiedParty(CompactEntityDto? entity)
    {
        if (entity is null)
        {
            return null;
        }

        return new SimplifiedPartyDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Type = entity.Type,
            Variant = entity.Variant,
            OrganizationIdentifier = entity.OrganizationIdentifier,
            IsDeleted = entity.IsDeleted,
            DeletedAt = entity.DeletedAt
        };
    }

    /// <summary>
    /// Converts a ConnectionDto to SimplifiedConnectionDto
    /// </summary>
    /// <param name="connection">The connection to convert</param>
    /// <returns>A simplified connection DTO, or null if connection is null</returns>
    public static SimplifiedConnectionDto? ToSimplifiedConnection(ConnectionDto? connection)
    {
        if (connection is null)
        {
            return null;
        }

        return new SimplifiedConnectionDto
        {
            Party = ToSimplifiedParty(connection.Party),
            Connections = connection.Connections?.Select(ToSimplifiedConnection).ToList() ?? []
        };
    }

    /// <summary>
    /// Converts a collection of ConnectionDto to simplified connections
    /// </summary>
    /// <param name="connections">The connections to convert</param>
    /// <returns>A collection of simplified connection DTOs</returns>
    public static IEnumerable<SimplifiedConnectionDto> ToSimplifiedConnections(IEnumerable<ConnectionDto>? connections)
    {
        return connections?.Select(ToSimplifiedConnection) ?? Enumerable.Empty<SimplifiedConnectionDto>();
    }
}
