using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Repositories;
using Altinn.AccessMgmt.Persistence.Repositories.Contracts;
using Altinn.AccessMgmt.Persistence.Services.Contracts;

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
