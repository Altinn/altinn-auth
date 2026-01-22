using System.Collections.Immutable;
using System.Diagnostics;
using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessMgmt.Core.Utils;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Queries.Connection;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.ProblemDetails;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.Core.Services;

/// <inheritdoc/>
public class ClientDelegationService(
    AppDbContext db,
    ConnectionQuery connectionQuery) : IClientDelegationService
{
    /// <inheritdoc/>
    public async Task<Result<List<AgentDto>>> GetAgentsForPartyAsync(Guid partyId, CancellationToken cancellationToken = default)
    {
        var connections = await connectionQuery.GetConnectionsToOthersAsync(
            new ConnectionQueryFilter
            {
                ToIds = [],
                FromIds = [partyId],
                RoleIds = [RoleConstants.Agent],
                IncludeResource = true,
                OnlyUniqueResults = true,
                IncludeSubConnections = false,
                IncludePackages = true,
                EnrichEntities = true,
            },
            ct: cancellationToken);

        var result = DtoMapper.ConvertToAgentDto(connections);

        return result;
    }

    /// <inheritdoc/>
    public async Task<Result<List<ClientDto>>> GetClientsForPartyAsync(Guid partyId, CancellationToken cancellationToken = default)
    {
        var connections = await connectionQuery.GetConnectionsFromOthersAsync(
            new ConnectionQueryFilter
            {
                ToIds = [partyId],
                FromIds = [],
                OnlyUniqueResults = true,
                IncludeSubConnections = false,
                IncludeResource = true,
                IncludePackages = true,
                EnrichEntities = true,
            },
            ct: cancellationToken);

        var result = DtoMapper.ConvertToClientDto(connections);

        return result;
    }

    /// <inheritdoc/>
    public async Task<Result<AssignmentDto>> AddAgentForParty(Guid partyId, Guid toUuid, CancellationToken cancellationToken = default)
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

    /// <inheritdoc/>
    public async Task<ValidationProblemInstance?> RemoveAgent(Guid partyId, Guid toUuid, bool cascade, CancellationToken cancellationToken = default)
    {
        ValidationErrorBuilder errorBuilder = default;

        var existingAssignment = await db.Assignments.AsTracking()
            .Where(p => p.FromId == partyId && p.ToId == toUuid && p.RoleId == RoleConstants.Agent)
            .FirstOrDefaultAsync(cancellationToken);

        if (!cascade)
        {
            var existingDelegations = await db.Delegations.AsNoTracking()
                .Where(p => p.ToId == toUuid)
                .Include(p => p.To)
                .Join(db.DelegationPackages, d => d.Id, dp => dp.DelegationId, (d, dp) => new
                {
                    Delegation = d,
                    DelegationPackage = dp,
                })
                .GroupBy(d => d.Delegation.Id)
                .ToListAsync(cancellationToken);

            foreach (var existingDelegation in existingDelegations)
            {
                var first = existingDelegation.FirstOrDefault();
                if (first is null)
                {
                    continue;
                }

                var pkgs = string.Join(", ", existingDelegation.Select(p => p.DelegationPackage.PackageId));
                var fromId = first.Delegation.FromId;
                var delegationId = first.Delegation.Id;

                if (fromId is { })
                {
                    errorBuilder.Add(
                        ValidationErrors.DelegationHasActiveConnections,
                        "QUERY/cascade",
                        [
                            new($"{delegationId}",$"Cannot remove delegation because party {toUuid} still has active delegated packages ({pkgs}) from party {fromId}.")
                        ]
                    );
                }
            }
        }

        if (errorBuilder.TryBuild(out var problem))
        {
            return problem;
        }

        db.Assignments.Remove(existingAssignment);
        await db.SaveChangesAsync(cancellationToken);
        return null;
    }

    /// <inheritdoc/>
    public async Task<Result<List<AgentDto>>> GetDelegatedAccessPackagesFromClientsViaParty(Guid partyId, Guid fromId, CancellationToken cancellationToken = default)
    {
        var connections = await connectionQuery.GetConnectionsToOthersAsync(
            new()
            {
                FromIds = [fromId],
                ViaIds = [partyId],
                ViaRoleIds = [RoleConstants.Agent],
                OnlyUniqueResults = true,
                IncludeSubConnections = false,
                EnrichEntities = true,
                IncludePackages = true
            },
            true,
            cancellationToken);

        var result = DtoMapper.ConvertToAgentDto(connections);

        return result;
    }

    /// <inheritdoc/>
    public async Task<Result<List<ClientDto>>> GetDelegatedAccessPackagesToAgentsViaPartyAsync(Guid partyId, Guid toId, CancellationToken cancellationToken = default)
    {
        var connections = await connectionQuery.GetConnectionsFromOthersAsync(
            new()
            {
                ViaIds = [partyId],
                ViaRoleIds = [RoleConstants.Agent],
                ToIds = [toId],
                OnlyUniqueResults = true,
                IncludeSubConnections = false,
                EnrichEntities = true,
                IncludePackages = true,
            },
            true,
            cancellationToken);

        var result = DtoMapper.ConvertToClientDto(connections);

        return result;
    }

    /// <inheritdoc/>
    public async Task<Result<List<DelegationDto>>> AddDelegationForAgentAsync(
        Guid partyId,
        Guid fromId,
        Guid toId,
        DelegationBatchInputDto payload,
        CancellationToken cancellationToken = default)
    {
        ValidationErrorBuilder errorBuilder = default;
        var inputs = payload.Values
            .Select((r, idx) =>
            {
                var roleExist = RoleConstants.TryGetByCode(r.Role, out var role);
                var pkgs = r.Packages.Select((p, idx) =>
                {
                    var packageExist = PackageConstants.TryGetByUrn(p, out var package);
                    return new
                    {
                        PackageExist = packageExist,
                        Package = package,
                        InputPackage = p,
                        PackageIdx = idx,
                    };
                });

                return new
                {
                    RoleIdx = idx,
                    InputRole = r.Role,
                    InputPackages = pkgs,
                    InputOk = roleExist && pkgs.All(p => p.PackageExist),
                    Role = role,
                    Packages = pkgs,
                };
            });

        foreach (var input in inputs)
        {
            if (input.Role is null)
            {
                errorBuilder.Add(ValidationErrors.InvalidRole, $"BODY/values[{input.RoleIdx}]/role", [new($"{input.Role}", "role do not exist.")]);
            }

            foreach (var inputPackage in input.InputPackages)
            {
                if (!inputPackage.PackageExist)
                {
                    errorBuilder.Add(ValidationErrors.InvalidPackage, $"BODY/values[{input.RoleIdx}]/packages[{inputPackage.PackageIdx}]", [new($"{inputPackage.InputPackage}", "package do not exist.")]);
                }
            }
        }

        var entities = await db.Entities.Where(e => e.Id == partyId || e.Id == fromId || e.Id == toId).ToDictionaryAsync(e => e.Id, cancellationToken);
        if (!entities.ContainsKey(partyId))
        {
            throw new UnreachableException();
        }

        if (!entities.ContainsKey(fromId))
        {
            errorBuilder.Add(ValidationErrors.EntityNotExists, $"QUERY/from", [new($"{fromId}", "entity do not exist.")]);
        }

        if (!entities.ContainsKey(toId))
        {
            errorBuilder.Add(ValidationErrors.EntityNotExists, $"QUERY/to", [new($"{toId}", "entity do not exist.")]);
        }

        if (errorBuilder.TryBuild(out var errorResult))
        {
            return errorResult;
        }

        var agentAssignment = await db.Assignments
            .FirstOrDefaultAsync(a => a.FromId == partyId && a.ToId == toId && a.RoleId == RoleConstants.Agent.Id, cancellationToken: cancellationToken);

        if (agentAssignment is null)
        {
            return new List<DelegationDto>();
        }

        var result = new List<DelegationDto>();
        foreach (var input in inputs)
        {
            var pkgIds = input.Packages.Select(p => p.Package).Select(p => p.Id).Distinct();
            var clientAssignment = await db.Assignments.AsNoTracking().FirstOrDefaultAsync(t => t.FromId == fromId && t.ToId == partyId && t.RoleId == input.Role.Id, cancellationToken);
            
            if (clientAssignment is null)
            {
                errorBuilder.Add(ValidationErrors.AssignmentHasActiveConnections, $"BODY/values[{input.RoleIdx}]", [new($"{input.Role.Entity.Urn}", $"Role is not assigned to '{partyId}' from '{fromId}'.")]);
                continue;
            }

            // check ass must exist
            var rolePackages = await db.RolePackages.AsNoTracking().Where(t => t.RoleId == input.Role.Id && pkgIds.Contains(t.PackageId)).ToListAsync(cancellationToken);
            var assignmentPackages = await db.AssignmentPackages.AsNoTracking().Where(t => t.AssignmentId == clientAssignment.Id && pkgIds.Contains(t.PackageId)).ToListAsync(cancellationToken);

            var delegation = await db.Delegations.AsNoTracking().FirstOrDefaultAsync(t => t.FromId == clientAssignment.Id && t.ToId == agentAssignment.Id && t.FacilitatorId == partyId, cancellationToken);
            if (delegation is null)
            {
                delegation = new Delegation()
                {
                    FromId = clientAssignment.Id,
                    ToId = agentAssignment.Id,
                    FacilitatorId = partyId
                };

                db.Delegations.Add(delegation);
            }

            var existingDelegationPackages = db.DelegationPackages.Where(t => t.DelegationId == delegation.Id);
            foreach (var pkgId in pkgIds)
            {
                var rolePackageId = rolePackages.FirstOrDefault(r => r.PackageId == pkgId)?.Id;
                var assignmentPackageId = assignmentPackages.FirstOrDefault(t => t.PackageId == pkgId)?.Id;

                if (!existingDelegationPackages.Any(
                    t =>
                    t.DelegationId == delegation.Id &&
                    t.PackageId == pkgId &&
                    t.RolePackageId == rolePackageId &&
                    t.AssignmentPackageId == assignmentPackageId))
                {
                    db.DelegationPackages.Add(new DelegationPackage()
                    {
                        PackageId = pkgId,
                        DelegationId = delegation.Id,
                        RolePackageId = rolePackageId,
                        AssignmentPackageId = assignmentPackageId,
                    });
                }

                result.Add(new()
                {
                    FromId = fromId,
                    ToId = toId,
                    ViaId = partyId,
                    RoleId = input.Role,
                    PackageId = pkgId,
                });
            }
        }

        if (errorBuilder.TryBuild(out errorResult))
        {
            return errorResult;
        }

        await db.SaveChangesAsync(cancellationToken);

        return result;
    }

    /// <inheritdoc/>
    public async Task<Result<List<DelegationDto>>> RemoveAgentDelegation(
        Guid partyId,
        Guid fromId,
        Guid toId,
        DelegationBatchInputDto payload,
        CancellationToken cancellationToken = default)
    {
        ValidationErrorBuilder errorBuilder = default;
        var inputs = payload.Values
            .Select((r, idx) =>
            {
                var roleExist = RoleConstants.TryGetByCode(r.Role, out var role);

                var pkgs = new List<(bool PackageExist, Package Package, string InputPackage, int PackageIdx)>();
                var pkg = r.Packages.Select((p, idx) =>
                {
                    var packageExist = PackageConstants.TryGetByUrn(p, out var package);
                    return new
                    {
                        PackageIdx = idx,
                        InputPackage = p,
                        Package = package,
                        PackageExist = packageExist,
                    };
                });

                return new
                {
                    RoleIdx = idx,
                    InputRole = r.Role,
                    InputPackages = pkgs,
                    InputOk = roleExist && pkgs.All(p => p.PackageExist),
                    Role = role,
                    Packages = pkgs,
                };
            });

        foreach (var input in inputs)
        {
            if (input.Role is null)
            {
                errorBuilder.Add(ValidationErrors.InvalidRole, $"BODY/values[{input.RoleIdx}]/role", [new($"{input.Role}", "role do not exist.")]);
            }

            foreach (var inputPackage in input.InputPackages)
            {
                if (!inputPackage.PackageExist)
                {
                    errorBuilder.Add(ValidationErrors.InvalidPackage, $"BODY/values[{input.RoleIdx}]/packages[{inputPackage.PackageIdx}]", [new($"{inputPackage.InputPackage}", "package do not exist.")]);
                }
            }
        }

        var entities = await db.Entities.Where(e => e.Id == partyId || e.Id == fromId || e.Id == toId).ToDictionaryAsync(e => e.Id, cancellationToken);
        if (!entities.ContainsKey(partyId))
        {
            throw new UnreachableException();
        }

        var agentAssignment = await db.Assignments
            .FirstOrDefaultAsync(a => a.FromId == partyId && a.ToId == toId && a.RoleId == RoleConstants.Agent, cancellationToken: cancellationToken);
        if (agentAssignment is null)
        {
            return new List<DelegationDto>();
        }

        var result = new List<DelegationDto>();
        foreach (var input in inputs)
        {
            var pkgIds = input.Packages.Select(p => p.Package).Select(p => p.Id).Distinct();
            var clientAssignment = await db.Assignments.AsNoTracking().FirstOrDefaultAsync(t => t.FromId == fromId && t.ToId == partyId && t.RoleId == input.Role.Id, cancellationToken);

            // check ass must exist
            var rolePackages = await db.RolePackages.AsNoTracking().Where(t => t.RoleId == input.Role.Id && pkgIds.Contains(t.PackageId)).ToListAsync(cancellationToken);
            var assignmentPackages = await db.AssignmentPackages.AsNoTracking().Where(t => t.AssignmentId == clientAssignment.Id && pkgIds.Contains(t.PackageId)).ToListAsync(cancellationToken);

            var delegation = await db.Delegations.AsNoTracking().FirstOrDefaultAsync(t => t.FromId == clientAssignment.Id && t.ToId == agentAssignment.Id && t.FacilitatorId == partyId, cancellationToken);
            if (delegation is null)
            {
                delegation = new Delegation()
                {
                    FromId = clientAssignment.Id,
                    ToId = agentAssignment.Id,
                    FacilitatorId = partyId
                };

                db.Delegations.Add(delegation);
            }

            var existingDelegationPackages = await db.DelegationPackages.AsTracking().Where(t => t.DelegationId == delegation.Id).ToListAsync(cancellationToken);
            foreach (var pkgId in pkgIds)
            {
                var rolePackageId = rolePackages.FirstOrDefault(r => r.PackageId == pkgId)?.Id;
                var assignmentPackageId = assignmentPackages.FirstOrDefault(t => t.PackageId == pkgId)?.Id;

                var toRemove = existingDelegationPackages.FirstOrDefault(
                    t =>
                    t.DelegationId == delegation.Id &&
                    t.PackageId == pkgId &&
                    t.RolePackageId == rolePackageId &&
                    t.AssignmentPackageId == assignmentPackageId);

                if (toRemove is { })
                {
                    db.DelegationPackages.Remove(toRemove);

                    result.Add(new()
                    {
                        FromId = fromId,
                        ToId = toId,
                        ViaId = partyId,
                        RoleId = input.Role,
                        PackageId = pkgId,
                    });
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        return result;
    }
}

/// <summary>
/// Delegation Service
/// </summary>
public interface IClientDelegationService
{
    Task<Result<List<ClientDto>>> GetClientsForPartyAsync(Guid partyId, CancellationToken cancellationToken = default);

    Task<Result<List<AgentDto>>> GetAgentsForPartyAsync(Guid partyId, CancellationToken cancellationToken = default);

    Task<Result<AssignmentDto>> AddAgentForParty(Guid partyId, Guid toUuid, CancellationToken cancellationToken = default);

    Task<ValidationProblemInstance?> RemoveAgent(Guid partyId, Guid toUuid, bool cascade, CancellationToken cancellationToken = default);

    Task<Result<List<AgentDto>>> GetDelegatedAccessPackagesFromClientsViaParty(Guid partyId, Guid fromId, CancellationToken cancellationToken = default);

    Task<Result<List<ClientDto>>> GetDelegatedAccessPackagesToAgentsViaPartyAsync(Guid partyId, Guid toId, CancellationToken cancellationToken = default);

    Task<Result<List<DelegationDto>>> AddDelegationForAgentAsync(Guid partyId, Guid fromId, Guid toId, DelegationBatchInputDto payload, CancellationToken cancellationToken = default);

    Task<Result<List<DelegationDto>>> RemoveAgentDelegation(Guid partyId, Guid fromId, Guid toId, DelegationBatchInputDto payload, CancellationToken cancellationToken = default);
}
