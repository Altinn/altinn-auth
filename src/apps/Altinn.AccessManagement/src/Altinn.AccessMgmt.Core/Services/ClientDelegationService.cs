using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessMgmt.Core.Utils;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Queries.Connection;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.ProblemDetails;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Altinn.AccessMgmt.Core.Services;

public partial class ClientDelegationService(
    ILogger<ClientDelegationService> Logger,
    AppDbContext db,
    ConnectionQuery connectionQuery) : IClientDelegationService
{
    public async Task<Result<List<AgentDto>>> GetAgentsAsync(Guid partyId, CancellationToken cancellationToken = default)
    {
        var connections = await connectionQuery.GetConnectionsToOthersAsync(
            new ConnectionQueryFilter
            {
                ToIds = [],
                FromIds = [partyId],
                RoleIds = [RoleConstants.Agent],
                IncludeResource = true,
                IncludePackages = true,
                EnrichEntities = true,
            },
            ct: cancellationToken);

        var result = DtoMapper.ConvertToClientDelegationAgentDto(connections);

        return result;
    }

    public async Task<Result<List<ClientDto>>> GetClientsAsync(Guid partyId, CancellationToken cancellationToken = default)
    {
        var connections = await connectionQuery.GetConnectionsFromOthersAsync(
            new ConnectionQueryFilter
            {
                ToIds = [partyId],
                FromIds = [],
                IncludeResource = true,
                IncludePackages = true,
                EnrichEntities = true,
            },
            ct: cancellationToken);

        var result = DtoMapper.ConvertToClientDto(connections);

        return result;
    }

    public async Task<Result<AssignmentDto>> AddAgent(Guid partyId, Guid toUuid, CancellationToken cancellationToken = default)
    {
        var existingAssignment = await db.Assignments.AsNoTracking().Where(p => p.FromId == partyId && p.ToId == toUuid && p.RoleId == RoleConstants.Agent).FirstOrDefaultAsync(cancellationToken);
        if (existingAssignment is { })
        {
            return DtoMapper.Convert(existingAssignment);
        }

        var entity = await db.Entities.FirstOrDefaultAsync(e => e.Id == toUuid, cancellationToken);
        if (entity is null)
        {
            return Problems.EntityTypeNotFound;
        }

        if ((entity.TypeId != EntityTypeConstants.Person) && entity.TypeId != EntityTypeConstants.SystemUser && entity.VariantId == EntityVariantConstants.AgentSystem)
        {
            return Problems.UnsupportedEntityType;
        }

        var assignment = new Assignment
        {
            FromId = partyId,
            ToId = toUuid,
            RoleId = RoleConstants.Agent,
        };
        db.Assignments.Add(assignment);
        await db.SaveChangesAsync(cancellationToken);

        return DtoMapper.Convert(assignment);
    }

    public async Task RemoveAgent(Guid partyId, Guid toUuid, CancellationToken cancellationToken = default)
    {
        var existingAssignment = await db.Assignments.AsTracking().Where(p => p.FromId == partyId && p.ToId == toUuid && p.RoleId == RoleConstants.Agent).FirstOrDefaultAsync(cancellationToken);
        if (existingAssignment is null)
        {
            return;
        }

        db.Assignments.Remove(existingAssignment);
        await db.SaveChangesAsync(cancellationToken);
    }
}

public interface IClientDelegationService
{
    Task<Result<List<ClientDto>>> GetClientsAsync(Guid partyId, CancellationToken cancellationToken = default);

    Task<Result<List<AgentDto>>> GetAgentsAsync(Guid partyId, CancellationToken cancellationToken = default);

    Task<Result<AssignmentDto>> AddAgent(Guid partyId, Guid toUuid, CancellationToken cancellationToken = default);

    Task RemoveAgent(Guid partyId, Guid toUuid, CancellationToken cancellationToken = default);
}
