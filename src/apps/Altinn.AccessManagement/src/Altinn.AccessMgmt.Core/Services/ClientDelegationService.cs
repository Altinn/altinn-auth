using System.Collections.Immutable;
using System.Diagnostics;
using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessMgmt.Core.Utils;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.ProblemDetails;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;

namespace Altinn.AccessMgmt.Core.Services;

/// <inheritdoc/>
public class ClientDelegationService(AppDbContext db) : IClientDelegationService
{
    /// <inheritdoc/>
    public async Task<Result<List<MyClientDto>>> GetMyClients(Guid partyId, List<Guid> provider, CancellationToken cancellationToken = default)
    {
        provider ??= [];
        var query = await db.Assignments
            .AsNoTracking()
            .Where(a => a.ToId == partyId && a.RoleId == RoleConstants.Agent)
            .WhereIf(provider.Count > 0, a => provider.Contains(a.FromId))
            .GroupJoin(
                db.Delegations,
                a => a.Id,
                d => d.ToId,
                (a, d) => new { Provider = a.From, Delegations = d, }
            )
            .SelectMany(d => d.Delegations.DefaultIfEmpty(), (a, d) => new { a.Provider, Delegation = d, })
            .GroupJoin(
                db.Assignments,
                x => x.Delegation.FromId,
                a => a.Id,
                (x, a) => new { x.Provider, x.Delegation, FromAssignments = a, }
            )
            .SelectMany(x => x.FromAssignments.DefaultIfEmpty(), (x, a) => new { x.Provider, x.Delegation, a.Role, Client = a.From })
            .GroupJoin(
                db.DelegationPackages,
                x => x.Delegation.Id,
                dp => dp.DelegationId,
                (x, dp) => new
                {
                    x.Provider,
                    x.Client,
                    x.Role,
                    Packages = dp,
                }
            )
            .SelectMany(x => x.Packages.DefaultIfEmpty(), (x, dp) => new { x.Client, x.Provider, x.Role, dp.Package })
            .GroupBy(x => x.Provider.Id)
            .ToListAsync(cancellationToken);

        return query
            .Select(i =>
                new MyClientDto()
                {
                    Provider = DtoMapper.Convert(i.First().Provider),
                    Clients = i.Where(j => j.Client is { }).GroupBy(j => j.Client.Id).Select(j => new ClientDto()
                    {
                        Client = DtoMapper.Convert(j.First().Client),
                        Access = j.Select(r => new ClientDto.RoleAccessPackages
                        {
                            Role = DtoMapper.ConvertCompactRole(r.Role),
                            Packages = j.Where(p => p.Package is { }).Select(p => DtoMapper.ConvertCompactPackage(p.Package)).DistinctBy(p => p.Id).ToArray(),
                        }).ToList()
                    })
                }
            )
            .ToList();
    }

    public async Task<Result<List<AgentDto>>> GetMyProviders(Guid useruuid, CancellationToken cancellationToken = default)
    {
        var query = await db.Assignments
            .AsNoTracking()
            .Include(a => a.From)
            .Where(a => a.ToId == useruuid && a.RoleId == RoleConstants.Agent)
            .ToListAsync(cancellationToken);

        return query
            .Select(a => new AgentDto
            {
                Agent = DtoMapper.Convert(a.From),
                AgentAddedAt = a.Audit_ValidFrom,
                Access = [
                    new()
                    {
                        Role = DtoMapper.ConvertCompactRole(a.Role),
                        Packages = [],
                    }
                ]
            }).ToList();
    }

    public async Task<Result<ValidationProblemInstance>> DeleteMyProvider(Guid useruuid, Guid provider, CancellationToken cancellationToken = default)
    {
        return await RemoveAgent(provider, useruuid, true, cancellationToken);
    }

    public async Task<Result<List<DelegationDto>>> DeleteMyClient(Guid useruuid, Guid provider, Guid from, DelegationBatchInputDto payload, CancellationToken cancellationToken = default)
    {
        return await RemoveAgentDelegation(provider, from, useruuid, payload, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Result<List<ClientDto>>> GetClients(Guid partyId, List<string>? roles, CancellationToken cancellationToken = default)
    {
        roles ??= [];
        var roleFilter = new List<Guid>();
        foreach (var r in roles)
        {
            if (RoleConstants.TryGetByAll(r, out var role))
            {
                roleFilter.Add(role.Id);
            }
            else
            {
                roleFilter.Add(Guid.Empty);
            }
        }

        if (roleFilter.Count > 0 && roleFilter.All(p => p == Guid.Empty))
        {
            return new List<ClientDto>();
        }

        var query = await db.Assignments
            .AsNoTracking()
            .Where(a => a.ToId == partyId)
            .WhereIf(roleFilter.Count > 0, r => roleFilter.Contains(r.RoleId))
            .Include(a => a.From)
            .GroupJoin(
                db.AssignmentPackages,
                a => a.Id,
                ap => ap.AssignmentId,
                (a, aps) => new { Assignment = a, AssignmentPackages = aps }
            )
            .SelectMany(
                x => x.AssignmentPackages.DefaultIfEmpty(),
                (x, ap) => new { x.Assignment, AssignmentPackage = ap }
            )
            .GroupJoin(
                db.RolePackages,
                x => x.Assignment.RoleId,
                rp => rp.RoleId,
                (x, rps) => new { x.Assignment, x.AssignmentPackage, RolePackages = rps }
            )
            .SelectMany(
                x => x.RolePackages.DefaultIfEmpty(),
                (x, rp) => new
                {
                    x.Assignment.To,
                    x.Assignment.From,
                    x.Assignment.Role,
                    AssignmentPackage = x.AssignmentPackage.Package,
                    RolePackage = rp.Package
                }
            )
            .Where(x =>
                (x.AssignmentPackage == null || x.AssignmentPackage.IsDelegable) &&
                (x.RolePackage == null || x.RolePackage.IsDelegable))
            .GroupBy(x => x.From.Id)
            .ToListAsync(cancellationToken);

        return query
            .Select(access =>
            new ClientDto()
            {
                Client = DtoMapper.Convert(access.First().From),
                Access = access.GroupBy(r => r.Role.Id).Select(r => new ClientDto.RoleAccessPackages
                {
                    Role = DtoMapper.ConvertCompactRole(r.First().Role),
                    Packages = [
                        .. r.Where(p => p.AssignmentPackage is { }).Select(p => DtoMapper.ConvertCompactPackage(p.AssignmentPackage)).DistinctBy(p => p.Id),
                        .. r.Where(p => p.RolePackage is { }).Select(p => DtoMapper.ConvertCompactPackage(p.RolePackage)).DistinctBy(p => p.Id),
                    ],
                }).ToList(),
            }).ToList();
    }

    /// <inheritdoc/>
    public async Task<Result<List<AgentDto>>> GetAgents(Guid partyId, CancellationToken cancellationToken = default)
    {
        var query = await db.Assignments
            .Where(a => a.FromId == partyId && a.RoleId == RoleConstants.Agent)
            .Include(a => a.To)
            .ToListAsync(cancellationToken);

        return query.Select(a => new AgentDto()
        {
            Agent = DtoMapper.Convert(a.To),
            AgentAddedAt = a.Audit_ValidFrom,
            Access = [
                new()
                {
                    Role = DtoMapper.ConvertCompactRole(RoleConstants.Agent.Entity),
                    Packages = []
                }
            ]
        }).ToList();
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public async Task<ValidationProblemInstance?> RemoveAgent(Guid partyId, Guid toUuid, bool cascade, CancellationToken cancellationToken = default)
    {
        ValidationErrorBuilder errorBuilder = default;

        var existingAssignment = await db.Assignments
            .AsTracking()
            .Where(p => p.FromId == partyId && p.ToId == toUuid && p.RoleId == RoleConstants.Agent)
            .FirstOrDefaultAsync(cancellationToken);

        var existingDelegations = await db.Delegations
            .AsNoTracking()
            .Where(p => p.ToId == existingAssignment.Id)
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

            if (!cascade)
            {
                errorBuilder.Add(
                    ValidationErrors.DelegationHasActiveConnections,
                    "QUERY/cascade",
                    [
                        new($"{first.Delegation.Id}", $"Cannot remove delegation '{first.Delegation.Id}' because party '{toUuid}' still has active delegated packages <{pkgs}> from party '{fromId}'.")
                    ]
                );
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
        var query = await db.Delegations
            .AsNoTracking()
            .Include(d => d.From)
            .Include(d => d.To).ThenInclude(a => a.To)
            .Where(d => d.FacilitatorId == partyId && d.From.FromId == fromId)
            .Join(
                db.DelegationPackages,
                d => d.Id,
                dp => dp.DelegationId,
                (d, dp) => new { Delegation = d, DelegationPackage = dp })
            .Select(x => new
            {
                x.Delegation.To.To,
                x.Delegation.From.Role,
                x.DelegationPackage.Package,
            })
            .GroupBy(x => x.To.Id)
            .ToListAsync(cancellationToken);

        var result = query
            .Select(e =>
            new AgentDto()
            {
                Agent = DtoMapper.Convert(e.First().To),
                AgentAddedAt = e.First().To.Audit_ValidFrom,
                Access = e.GroupBy(r => r.Role.Id).Select(r => new AgentDto.AgentRoleAccessPackages
                {
                    Role = DtoMapper.ConvertCompactRole(r.First().Role),
                    Packages = r.Select(r => DtoMapper.ConvertCompactPackage(r.Package)).DistinctBy(p => p.Id).ToArray(),
                }).ToList(),
            }).ToList();

        return result;
    }

    /// <inheritdoc/>
    public async Task<Result<List<ClientDto>>> GetDelegatedAccessPackagesToAgentsViaPartyAsync(Guid partyId, Guid toId, CancellationToken cancellationToken = default)
    {
        var query = await db.Delegations
            .AsNoTracking()
            .Include(d => d.To)
            .Include(d => d.From).ThenInclude(a => a.From)
            .Where(d => d.FacilitatorId == partyId && d.To.ToId == toId)
            .Join(
                db.DelegationPackages,
                d => d.Id,
                dp => dp.DelegationId,
                (d, dp) => new { Delegation = d, DelegationPackage = dp })
            .Select(x => new
            {
                x.Delegation.From.From,
                x.Delegation.From.Role,
                x.DelegationPackage.Package
            })
            .GroupBy(x => x.From.Id)
            .ToListAsync(cancellationToken);

        var result = query
            .Select(e =>
            new ClientDto()
            {
                Client = DtoMapper.Convert(e.First().From),
                Access = e.GroupBy(r => r.Role.Id).Select(r => new ClientDto.RoleAccessPackages
                {
                    Role = DtoMapper.ConvertCompactRole(r.First().Role),
                    Packages = r.Select(r => DtoMapper.ConvertCompactPackage(r.Package)).DistinctBy(p => p.Id).ToArray(),
                }).ToList(),
            }).ToList();

        return result;
    }

    /// <inheritdoc/>
    public async Task<Result<List<DelegationDto>>> DelegateAccessPackageToAgent(
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
                var roleExist = RoleConstants.TryGetByAll(r.Role, out var role);
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
            }).ToList();

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
                    continue;
                }

                if (!inputPackage.Package.Entity.IsDelegable)
                {
                    errorBuilder.Add(ValidationErrors.PackageIsNotDelegable, $"BODY/values[{input.RoleIdx}]/packages[{inputPackage.PackageIdx}]", [new($"{inputPackage.Package.Entity.Urn}", $"Package is not delegable.")]);
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

        var agentAssignment = await db.Assignments
            .FirstOrDefaultAsync(a => a.FromId == partyId && a.ToId == toId && a.RoleId == RoleConstants.Agent.Id, cancellationToken: cancellationToken);

        if (agentAssignment is null)
        {
            errorBuilder.Add(ValidationErrors.MissingAssignment, $"QUERY/to", [new(RoleConstants.Agent.Entity.Urn, $"Role is not assigned to '{toId}' from '{partyId}'.")]);
        }

        if (errorBuilder.TryBuild(out var errorResult))
        {
            return errorResult;
        }

        var result = new List<DelegationDto>();
        foreach (var input in inputs)
        {
            var pkgIds = input.Packages.Select(p => p.Package).Select(p => p.Id).Distinct();
            var clientAssignment = await db.Assignments.AsNoTracking().FirstOrDefaultAsync(t => t.FromId == fromId && t.ToId == partyId && t.RoleId == input.Role.Id, cancellationToken);

            if (clientAssignment is null)
            {
                errorBuilder.Add(ValidationErrors.MissingAssignment, $"BODY/values[{input.RoleIdx}]/role", [new($"{input.Role.Entity.Urn}", $"Role is not assigned to '{partyId}' from '{fromId}'.")]);
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
            foreach (var pkg in input.Packages)
            {
                var rolePackageId = rolePackages.FirstOrDefault(r => r.PackageId == pkg.Package.Id)?.Id;
                var assignmentPackageId = assignmentPackages.FirstOrDefault(t => t.PackageId == pkg.Package.Id)?.Id;
                if (rolePackageId is null && assignmentPackageId is null)
                {
                    errorBuilder.Add(ValidationErrors.UserNotAuthorized, $"BODY/values[{input.RoleIdx}]/packages[{pkg.PackageIdx}]", [new($"{pkg.Package.Entity.Urn}", $"Can't delegate package from client '{fromId}' as they haven't been assigned to '{partyId}' through role '{input.Role.Entity.Urn}'.")]);
                    continue;
                }

                var delegationExist = await existingDelegationPackages.AnyAsync(
                    t =>
                    t.DelegationId == delegation.Id &&
                    t.PackageId == pkg.Package.Id &&
                    t.RolePackageId == rolePackageId &&
                    t.AssignmentPackageId == assignmentPackageId,
                    cancellationToken);

                if (!delegationExist)
                {
                    db.DelegationPackages.Add(new DelegationPackage()
                    {
                        PackageId = pkg.Package.Id,
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
                    PackageId = pkg.Package.Id,
                    Changed = !delegationExist,
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
                var roleExist = RoleConstants.TryGetByAll(r.Role, out var role);
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
            }).ToList();

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
                    continue;
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

        var agentAssignment = await db.Assignments
            .FirstOrDefaultAsync(a => a.FromId == partyId && a.ToId == toId && a.RoleId == RoleConstants.Agent.Id, cancellationToken: cancellationToken);

        if (agentAssignment is null)
        {
            errorBuilder.Add(ValidationErrors.MissingAssignment, $"QUERY/to", [new(RoleConstants.Agent.Entity.Urn, $"Role is not assigned to '{toId}' from '{partyId}'.")]);
        }

        if (errorBuilder.TryBuild(out var errorResult))
        {
            return errorResult;
        }

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
                continue;
            }

            // check ass must exist
            var rolePackages = await db.RolePackages.AsNoTracking().Where(t => t.RoleId == input.Role.Id && pkgIds.Contains(t.PackageId)).ToListAsync(cancellationToken);
            var assignmentPackages = await db.AssignmentPackages.AsNoTracking().Where(t => t.AssignmentId == clientAssignment.Id && pkgIds.Contains(t.PackageId)).ToListAsync(cancellationToken);

            var delegation = await db.Delegations.AsNoTracking().FirstOrDefaultAsync(t => t.FromId == clientAssignment.Id && t.ToId == agentAssignment.Id && t.FacilitatorId == partyId, cancellationToken);
            if (delegation is null)
            {
                result.AddRange(pkgIds.Select(pkg => new DelegationDto()
                {
                    FromId = fromId,
                    PackageId = pkg,
                    RoleId = input.Role,
                    ToId = toId,
                    ViaId = partyId,
                    Changed = false,
                }));

                continue;
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
                }

                result.Add(new()
                {
                    FromId = fromId,
                    ToId = toId,
                    ViaId = partyId,

                    RoleId = input.Role,
                    PackageId = pkgId,
                    Changed = toRemove is { }
                });
            }
        }

        var uniqueRoleIds = inputs.Select(p => p.Role.Id).Distinct();
        var delegations = await db.Delegations
            .Where(d => d.FacilitatorId == partyId)
            .Include(d => d.From)
            .Where(d => d.From.FromId == fromId && uniqueRoleIds.Contains(d.From.RoleId))
            .GroupJoin(
                db.DelegationPackages,
                d => d.Id,
                dp => dp.DelegationId,
                (d, dp) => new
                {
                    Packages = dp,
                    AnyPackages = dp.Any(),
                    DelegationId = d.Id,
                }
            )
            .ToListAsync(cancellationToken);

        foreach (var delegation in delegations)
        {
            var scheduledDeletedPackages = result.Where(r => r.Changed).Select(p => p.PackageId).ToHashSet();
            var currentPackages = delegation.Packages.Select(p => p.PackageId).ToHashSet() ?? [];
            var shouldDeleteDelegation = scheduledDeletedPackages.SetEquals(currentPackages);

            if (shouldDeleteDelegation)
            {
                var deleteDelegation = await db.Delegations
                    .AsTracking()
                    .FirstOrDefaultAsync(d => d.Id == delegation.DelegationId, cancellationToken);

                db.Delegations.Remove(deleteDelegation);
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        return result;
    }
}

/// <summary>
/// Client Delegation Service
/// </summary>
public interface IClientDelegationService
{
    #region My
    Task<Result<List<MyClientDto>>> GetMyClients(Guid partyId, List<Guid> provider, CancellationToken cancellationToken = default);

    Task<Result<List<AgentDto>>> GetMyProviders(Guid useruuid, CancellationToken cancellationToken = default);

    Task<Result<ValidationProblemInstance?>> DeleteMyProvider(Guid useruuid, Guid provider, CancellationToken cancellationToken = default);

    Task<Result<List<DelegationDto>>> DeleteMyClient(Guid useruuid, Guid provider, Guid from, DelegationBatchInputDto payload, CancellationToken cancellationToken = default);

    #endregion

    #region Provider
    Task<Result<List<ClientDto>>> GetClients(Guid partyId, List<string>? roles, CancellationToken cancellationToken = default);

    Task<Result<List<AgentDto>>> GetAgents(Guid partyId, CancellationToken cancellationToken = default);

    Task<Result<AssignmentDto>> AddAgent(Guid partyId, Guid toUuid, CancellationToken cancellationToken = default);

    Task<ValidationProblemInstance?> RemoveAgent(Guid partyId, Guid toUuid, bool cascade, CancellationToken cancellationToken = default);

    Task<Result<List<AgentDto>>> GetDelegatedAccessPackagesFromClientsViaParty(Guid partyId, Guid fromId, CancellationToken cancellationToken = default);

    Task<Result<List<ClientDto>>> GetDelegatedAccessPackagesToAgentsViaPartyAsync(Guid partyId, Guid toId, CancellationToken cancellationToken = default);

    Task<Result<List<DelegationDto>>> DelegateAccessPackageToAgent(Guid partyId, Guid fromId, Guid toId, DelegationBatchInputDto payload, CancellationToken cancellationToken = default);

    Task<Result<List<DelegationDto>>> RemoveAgentDelegation(Guid partyId, Guid fromId, Guid toId, DelegationBatchInputDto payload, CancellationToken cancellationToken = default);
    #endregion
}
