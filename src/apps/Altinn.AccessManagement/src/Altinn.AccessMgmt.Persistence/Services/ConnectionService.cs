using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Repositories;
using Altinn.AccessMgmt.Persistence.Repositories.Contracts;
using Altinn.AccessMgmt.Persistence.Services.Contracts;
using Altinn.AccessMgmt.Persistence.Services.Models;

namespace Altinn.AccessMgmt.Persistence.Services;

/// <summary>
/// Service for managing connections.
/// </summary>
public class ConnectionService(
    IConnectionRepository connectionRepository,
    IConnectionPackageRepository connectionPackageRepository,
    IConnectionResourceRepository connectionResourceRepository
    ) : IConnectionService
{
    private readonly IConnectionRepository connectionRepository = connectionRepository;
    private readonly IConnectionPackageRepository connectionPackageRepository = connectionPackageRepository;
    private readonly IConnectionResourceRepository connectionResourceRepository = connectionResourceRepository;

    /// <inheritdoc />
    public async Task<IEnumerable<ExtConnection>> GetGiven(Guid id)
    {
        var filter = connectionRepository.CreateFilterBuilder();
        filter.Equal(t => t.FromId, id);
        return await connectionRepository.GetExtended(filter);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ExtConnection>> GetRecived(Guid id)
    {
        var filter = connectionRepository.CreateFilterBuilder();
        filter.Equal(t => t.ToId, id);
        return await connectionRepository.GetExtended(filter);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ExtConnection>> GetFacilitated(Guid id)
    {
        var filter = connectionRepository.CreateFilterBuilder();
        filter.Equal(t => t.FacilitatorId, id);
        return await connectionRepository.GetExtended(filter);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ExtConnection>> GetSpecific(Guid fromId, Guid toId)
    {
        var filter = connectionRepository.CreateFilterBuilder();
        filter.Equal(t => t.FromId, fromId);
        filter.Equal(t => t.ToId, toId);
        return await connectionRepository.GetExtended(filter);
    }

    /// <inheritdoc />
    public async Task<ExtConnection> Get(Guid Id)
    {
        var filter = connectionRepository.CreateFilterBuilder();
        filter.Equal(t => t.Id, Id);
        var res = await connectionRepository.GetExtended(filter);
        return res.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Package>> GetPackages(Guid id)
    {
        return await connectionPackageRepository.GetB(id);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Resource>> GetResources(Guid id)
    {
        return await connectionResourceRepository.GetB(id);
    }
}

/// <summary>
/// Convert database models to dto models
/// </summary>
public static class ConnectionConverter
{
    /// <summary>
    /// Convert database model to response model
    /// </summary>
    public static CreateDelegationResponse ConvertToResponseModel(Connection connection)
    {
        return new CreateDelegationResponse()
        {
            DelegationId = connection.Id,
            FromEntityId = connection.FromId
        };
    }

    /// <summary>
    /// Convert database model to dto model
    /// </summary>
    public static ConnectionDto ConvertToDto(ExtConnection connection)
    {
        return new ConnectionDto()
        {
            Id = connection.Id,
            From = connection.From,
            To = connection.To,
            Facilitator = connection.Facilitator,
            Role = ConvertToDto(connection.Role),
            FacilitatorRole = ConvertToDto(connection.FacilitatorRole),
            Delegation = connection.Delegation
        };
    }

    /// <summary>
    /// Convert database model to dto model
    /// </summary>
    public static RoleDto ConvertToDto(Role role)
    {
        return new RoleDto()
        {
            Id = role.Id,
            Description = role.Description,
            Name = role.Name,
            Code = role.Code,
            Urn = role.Urn,
            IsKeyRole = role.IsKeyRole
        };
    }
}
