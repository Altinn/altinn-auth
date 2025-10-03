using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.Core.Utils;
using Altinn.AccessMgmt.Core.Validation;
using Altinn.AccessMgmt.Persistence.Services.Models;
using Altinn.AccessMgmt.PersistenceEF.Audit;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Queries;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.ProblemDetails;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.Core.Services;

/// <inheritdoc />
public partial class ConnectionService : IConnectionService
{
    public AppDbContext DbContext { get; }

    public IAuditAccessor AuditAccessor { get; }

    public ConnectionService(AppDbContext dbContext, IAuditAccessor auditAccessor)
    {
        DbContext = dbContext;
        AuditAccessor = auditAccessor;
    }

    public async Task<Result<AssignmentDto>> AddAssignment(Guid fromId, Guid toId, Role role, Action<ConnectionOptions> configureOptions = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(role);
        var options = new ConnectionOptions(configureOptions);
        var entities = await DbContext.Entities
            .AsNoTracking()
            .Where(e => e.Id == fromId || e.Id == toId)
            .Include(e => e.Type)
            .ToListAsync(cancellationToken);

        var fromEntity = entities.FirstOrDefault(e => e.Id == fromId);
        var toEntity = entities.FirstOrDefault(e => e.Id == toId);

        var problem = ValidateEntities(fromEntity, toEntity, options);
        if (problem is { })
        {
            return problem;
        }

        var existingAssignment = await DbContext.Assignments
            .AsNoTracking()
            .Where(e => e.FromId == fromId)
            .Where(e => e.ToId == toId)
            .Where(e => e.RoleId == role.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingAssignment is { })
        {
            return DtoMapper.Convert(existingAssignment);
        }

        var assignment = new Assignment()
        {
            FromId = fromId,
            ToId = toId,
            RoleId = role.Id,
        };

        await DbContext.Assignments.AddAsync(assignment);
        await DbContext.SaveChangesAsync(cancellationToken);

        return DtoMapper.Convert(assignment);
    }

    public async Task<ValidationProblemInstance> RemoveAssignment(Guid fromId, Guid toId, Role role, bool cascade = false, Action<ConnectionOptions> configureConnectionOptions = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(role);
        
        var options = new ConnectionOptions(configureConnectionOptions);
        var entities = await DbContext.Entities
            .AsNoTracking()
            .Where(e => e.Id == fromId || e.Id == toId)
            .Include(e => e.Type)
            .ToListAsync(cancellationToken);

        var fromEntity = entities.FirstOrDefault(e => e.Id == fromId);
        var toEntity = entities.FirstOrDefault(e => e.Id == toId);

        var problem = ValidateEntities(fromEntity, toEntity, options);
        if (problem is { })
        {
            return problem;
        }

        var existingAssignment = await DbContext.Assignments
            .AsTracking()
            .Where(e => e.FromId == fromId)
            .Where(e => e.ToId == toId)
            .Where(e => e.RoleId == role.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingAssignment is null)
        {
            return null;
        }

        if (!cascade)
        {
            var assignedPackages = await DbContext.AssignmentPackages
                .AsNoTracking()
                .Where(p => p.AssignmentId == existingAssignment.Id)
                .ToListAsync(cancellationToken);

            var delegationsFrom = await DbContext.Delegations
                .AsNoTracking()
                .Where(p => p.FromId == existingAssignment.Id)
                .ToListAsync(cancellationToken);

            var delegationsTo = await DbContext.Delegations
                .AsNoTracking()
                .Where(p => p.ToId == toId)
                .ToListAsync(cancellationToken);

            problem = ValidationComposer.Validate(
                AssignementPackageValidation.HasAssignedPackages(assignedPackages),
                DelegationValidation.HasDelegationsAssigned(delegationsFrom),
                DelegationValidation.HasDelegationsAssigned(delegationsTo)
            );

            if (problem is { })
            {
                return problem;
            }
        }

        DbContext.Remove(existingAssignment);
        await DbContext.SaveChangesAsync(cancellationToken);
        return null;
    }

    public async Task<Result<AssignmentPackageDto>> AddPackage(Guid fromId, Guid toId, Role role, Guid packageId, Action<ConnectionOptions> configureConnectionOptions = null, CancellationToken cancellationToken = default)
    {
        return await AddPackage(fromId, toId, role, packageId, "packageId", configureConnectionOptions, cancellationToken);
    }

    public async Task<Result<AssignmentPackageDto>> AddPackage(Guid fromId, Guid toId, Role role, string packageUrn, Action<ConnectionOptions> configureConnectionOptions = null, CancellationToken cancellationToken = default)
    {
        packageUrn = (packageUrn.StartsWith("urn:", StringComparison.Ordinal) || packageUrn.StartsWith(':')) ? packageUrn : ":" + packageUrn;
        
        var package = await DbContext.Packages
            .AsNoTracking()
            .Where(p => p.Urn.EndsWith(packageUrn))
            .FirstOrDefaultAsync(cancellationToken);

        var problem = ValidationComposer.Validate(PackageValidation.PackageExists(package));
        if (problem is { })
        {
            return problem;
        }

        return await AddPackage(fromId, toId, role, package.Id, "package", configureConnectionOptions, cancellationToken);
    }

    public async Task<ValidationProblemInstance> RemovePackage(Guid fromId, Guid toId, Role role, string packageUrn, CancellationToken cancellationToken = default)
    {
        packageUrn = packageUrn.StartsWith("urn:", StringComparison.Ordinal) || packageUrn.StartsWith(':') ? packageUrn : ":" + packageUrn;
        if (PackageConstants.TryGetByUrn(packageUrn, out var result))
        {
            return await RemovePackage(fromId, toId, role, result, cancellationToken);
        }

        return ValidationComposer.Validate(PackageValidation.PackageUrnLookup([], [packageUrn]));
    }

    public async Task<ValidationProblemInstance> RemovePackage(Guid fromId, Guid toId, Role role, Guid packageId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(role);
        
        var assignment = await DbContext.Assignments
            .AsNoTracking()
            .Where(a => a.FromId == fromId)
            .Where(a => a.ToId == toId)
            .Where(a => a.RoleId == role.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (assignment is null)
        {
            return null;
        }

        var existingAssignmentPackages = await DbContext.AssignmentPackages
            .AsTracking()
            .Where(a => a.AssignmentId == assignment.Id)
            .Where(a => a.PackageId == packageId)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingAssignmentPackages is null)
        {
            return null;
        }

        DbContext.Remove(assignment);
        await DbContext.SaveChangesAsync(cancellationToken);

        return null;
    }

    private async Task<Result<AssignmentPackageDto>> AddPackage(Guid fromId, Guid toId, Role role, Guid packageId, string queryParamName, Action<ConnectionOptions> configureConnectionOptions = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(role);
        
        var options = new ConnectionOptions(configureConnectionOptions);
        var entities = await DbContext.Entities
            .AsNoTracking()
            .Where(e => e.Id == fromId || e.Id == toId)
            .Include(e => e.Type)
            .ToListAsync(cancellationToken);

        var fromEntity = entities.FirstOrDefault(e => e.Id == fromId);
        var toEntity = entities.FirstOrDefault(e => e.Id == toId);

        var problem = ValidateEntities(fromEntity, toEntity, options);
        if (problem is { })
        {
            return problem;
        }

        var assignment = await DbContext.Assignments
                .AsNoTracking()
                .Where(a => a.FromId == fromId)
                .Where(a => a.ToId == toId)
                .Where(a => a.RoleId == role.Id)
                .FirstOrDefaultAsync(cancellationToken);

        if (assignment is null)
        {
            return Problems.MissingRightHolder;
        }

        var check = await CheckPackage(fromId, packageIds: [packageId], cancellationToken);
        if (check.IsProblem)
        {
            return check.Problem;
        }

        problem = ValidationComposer.Validate(
            PackageValidation.AuthorizePackageAssignment(check.Value),
            PackageValidation.PackageIsAssignableToRecipient(check.Value.Select(p => p.Package.Urn), toEntity.Type, queryParamName)
        );

        if (problem is { })
        {
            return problem;
        }

        var existingAssignmentPackage = await DbContext.AssignmentPackages
            .AsNoTracking()
            .Where(a => a.AssignmentId == assignment.Id)
            .Where(a => a.PackageId == packageId)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingAssignmentPackage is { })
        {
            return DtoMapper.Convert(existingAssignmentPackage);
        }

        existingAssignmentPackage = new AssignmentPackage()
        {
            AssignmentId = assignment.Id,
            PackageId = packageId,
        };

        await DbContext.AssignmentPackages.AddAsync(existingAssignmentPackage);
        await DbContext.SaveChangesAsync(cancellationToken);

        return DtoMapper.Convert(existingAssignmentPackage);
    }

    public async Task<Result<IEnumerable<AccessPackageDto.Check>>> CheckPackage(Guid party, IEnumerable<Guid> packageIds = null, CancellationToken cancellationToken = default)
    {
        
        var assignablePackages = await DbContext.GetAssignableAccessPackages(
            party,
            AuditAccessor.AuditValues.ChangedBy, 
            packageIds,
            cancellationToken
        );

        return assignablePackages.GroupBy(p => p.Package.Id).Select(group =>
        {
            var firstPackage = group.First();
            return new AccessPackageDto.Check
            {
                Package = new AccessPackageDto
                {
                    Id = firstPackage.Package.Id,
                    Urn = firstPackage.Package.Urn,
                    AreaId = firstPackage.Package.AreaId
                },
                Result = group.Any(p => p.Result),
                Reasons = group.Select(p => new AccessPackageDto.Check.Reason
                {
                    Description = p.Reason.Description,
                    RoleId = p.Reason.RoleId,
                    RoleUrn = p.Reason.RoleUrn,
                    FromId = p.Reason.FromId,
                    FromName = p.Reason.FromName,
                    ToId = p.Reason.ToId,
                    ToName = p.Reason.ToName,
                    ViaId = p.Reason.ViaId,
                    ViaName = p.Reason.ViaName,
                    ViaRoleId = p.Reason.ViaRoleId,
                    ViaRoleUrn = p.Reason.ViaRoleUrn
                })
            };
        }).ToList();
    }

    public async Task<Result<IEnumerable<AccessPackageDto.Check>>> CheckPackage(Guid party, IEnumerable<string> packages, IEnumerable<Guid> packageIds = null, CancellationToken cancellationToken = default)
    {
        packages = packages.Select(p =>
        p.StartsWith("urn:", StringComparison.Ordinal)
            ? p
            : (p.StartsWith(':')
                ? $"urn:altinn:accesspackage{p}"
                : $"urn:altinn:accesspackage:{p}"));

        var packagesFound = packages.Select(p =>
        {
            if (PackageConstants.TryGetByUrn(p, out var package))
            {
                return package.Entity;
            }

            return null;
        }).Where(p => p is { });

        var problem = ValidationComposer.Validate(PackageValidation.PackageUrnLookup(packagesFound, packages));
        if (problem is { })
        {
            return problem;
        }

        return await CheckPackage(party, [.. packageIds, .. packagesFound.Select(p => p.Id)], cancellationToken);
    }

    private ValidationProblemInstance? ValidateEntities(Entity from, Entity to, ConnectionOptions options)
    {
        var entityExists = ValidationComposer.Validate(
            EntityValidation.FromExists(from),
            EntityValidation.ToExists(to)
        );

        if (entityExists is { })
        {
            return entityExists;
        }

        var entitiesIsOfRightType = ValidationComposer.Validate(
            EntityTypeValidation.FromIsOfType(from.TypeId, [.. options.SupportedFromEntityTypes]),
            EntityTypeValidation.ToIsOfType(to.TypeId, [.. options.SupportedToEntityTypes])
        );

        if (entitiesIsOfRightType is { })
        {
            return entitiesIsOfRightType;
        }

        return null;
    }
}

public sealed class ConnectionOptions
{
    internal ConnectionOptions(Action<ConnectionOptions> configureConnectionService)
    {
        if (configureConnectionService is { })
        {
            configureConnectionService(this);
        }
    }

    public IEnumerable<Guid> SupportedFromEntityTypes { get; set; } = [];

    public IEnumerable<Guid> SupportedToEntityTypes { get; set; } = [];
}

/// <inheritdoc />
public partial class ConnectionService
{
    /// <inheritdoc />
    public async Task<IEnumerable<ConnectionPackageDto>> GetConnectionsToOthers(Guid partyId, Guid? toId = null, Guid? roleId = null, Guid? packageId = null, Guid? resourceId = null, CancellationToken cancellationToken = default)
    {
        var result = await DbContext.Connections.AsNoTracking()
            .Include(t => t.To)
            .ThenInclude(t => t.Variant)
            .Include(t => t.To)
            .ThenInclude(t => t.Type)
            .Include(t => t.Role)
            .Include(t => t.Package)
            .Where(t => t.FromId == partyId)
            .WhereIf(toId.HasValue, t => t.ToId == toId.Value)
            .WhereIf(roleId.HasValue, t => t.RoleId == roleId.Value)
            .WhereIf(packageId.HasValue, t => t.PackageId == packageId.Value)
            .WhereIf(resourceId.HasValue, t => t.ResourceId == resourceId.Value)
            .ToListAsync(cancellationToken);

        return ExtractRelationPackageDtoToOthers(result, includeSubConnections: false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ConnectionDto>> GetConnectionsToOthers(Guid partyId, Guid? toId = null, Guid? roleId = null, CancellationToken cancellationToken = default)
    {
        var result = await DbContext.Connections.AsNoTracking()
            .Include(t => t.To)
            .ThenInclude(t => t.Variant)
            .Include(t => t.To)
            .ThenInclude(t => t.Type)
            .Include(t => t.Role)
            .Where(t => t.FromId == partyId)
            .WhereIf(toId.HasValue, t => t.ToId == toId.Value)
            .WhereIf(roleId.HasValue, t => t.RoleId == roleId.Value)
            .ToListAsync(cancellationToken);

        return ExtractRelationDtoToOthers(result, includeSubConnections: false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ConnectionPackageDto>> GetConnectionsFromOthers(Guid partyId, Guid? fromId = null, Guid? roleId = null, Guid? packageId = null, Guid? resourceId = null, CancellationToken cancellationToken = default)
    {
        var result = await DbContext.Connections.AsNoTracking()
            .Include(t => t.From)
            .ThenInclude(t => t.Variant)
            .Include(t => t.From)
            .ThenInclude(t => t.Type)
            .Include(t => t.Role)
            .Include(t => t.Package)
            .Where(t => t.ToId == partyId)
            .WhereIf(fromId.HasValue, t => t.FromId == fromId.Value)
            .WhereIf(roleId.HasValue, t => t.RoleId == roleId.Value)
            .WhereIf(packageId.HasValue, t => t.PackageId == packageId.Value)
            .WhereIf(resourceId.HasValue, t => t.ResourceId == resourceId.Value)
            .ToListAsync(cancellationToken);

        return ExtractRelationPackageDtoFromOthers(result, includeSubConnections: false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ConnectionDto>> GetConnectionsFromOthers(Guid partyId, Guid? fromId = null, Guid? roleId = null, CancellationToken cancellationToken = default)
    {
        var result = await DbContext.Connections.AsNoTracking()
            .Include(c => c.From)
            .ThenInclude(c => c.Type)
            .Include(c => c.From)
            .ThenInclude(c => c.Variant)
            .Include(c => c.Role)
            .Where(t => t.ToId == partyId)
            .WhereIf(fromId.HasValue, t => t.FromId == fromId.Value)
            .WhereIf(roleId.HasValue, t => t.RoleId == roleId.Value)
            .ToListAsync(cancellationToken);

        return ExtractRelationDtoFromOthers(result, includeSubConnections: false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<PackagePermissionDto>> GetPackagePermissionsFromOthers(Guid partyId, Guid? fromId = null, Guid? packageId = null, CancellationToken cancellationToken = default)
    {
        var result = await DbContext.Connections
            .AsNoTracking()
            .Include(t => t.Package)
            .Where(t => t.ToId == partyId)
            .WhereIf(fromId.HasValue, t => t.FromId == fromId.Value)
            .WhereIf(packageId.HasValue, t => t.PackageId == packageId.Value)
            .ToListAsync(cancellationToken);

        if (result is { } && result.Any() && result.Where(r => r.Package is { }) is var packages)
        {
            return packages.DistinctBy(t => t.Package.Id).Select(permission => new PackagePermissionDto()
            {
                Package = DtoMapper.ConvertCompactPackage(permission.Package),
                Permissions = packages.Where(t => t.Package.Id == permission.Package.Id).Select(DtoMapper.ConvertToPermission)
            });
        }

        return [];
    }

    /// <inheritdoc />
    public async Task<IEnumerable<PackagePermissionDto>> GetPackagePermissionsToOthers(Guid partyId, Guid? toId = null, Guid? packageId = null, CancellationToken cancellationToken = default)
    {
        var result = await DbContext.Connections
            .AsNoTracking()
            .Include(t => t.Package)
            .Include(t => t.From)
            .Include(t => t.To)
            .Include(t => t.Via)
            .Include(t => t.ViaRole)
            .Include(t => t.Role)
            .Where(t => t.FromId == partyId)
            .WhereIf(toId.HasValue, t => t.ToId == toId.Value)
            .WhereIf(packageId.HasValue, t => t.PackageId == packageId.Value)
            .ToListAsync(cancellationToken);

        if (result is { } && result.Any() && result.Where(r => r.Package is { }) is var packages)
        {
            return packages.DistinctBy(t => t.Package.Id).Select(permission => new PackagePermissionDto()
            {
                Package = DtoMapper.ConvertCompactPackage(permission.Package),
                Permissions = packages.Where(t => t.Package.Id == permission.Package.Id).Select(DtoMapper.ConvertToPermission)
            });
        }

        return [];
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ResourcePermission>> GetResourcePermissionsFromOthers(Guid partyId, Guid? fromId = null, Guid? packageId = null, Guid? resourceId = null, CancellationToken cancellationToken = default)
    {
        var result = await DbContext.Connections
            .AsNoTracking()
            .Include(t => t.Package)
            .Include(t => t.Role)
            .Include(t => t.Resource)
            .Where(t => t.ToId == partyId)
            .WhereIf(fromId.HasValue, t => t.FromId == fromId.Value)
            .WhereIf(packageId.HasValue, t => t.PackageId == packageId.Value)
            .WhereIf(resourceId.HasValue, t => t.ResourceId == resourceId.Value)
            .ToListAsync(cancellationToken);

        if (result is { } && result.Any() && result.Where(r => r.Package is { }) is var packages)
        {
            return packages.DistinctBy(t => t.Resource.Id).Select(permission => new ResourcePermission()
            {
                Resource = permission.Resource,
                Permissions = packages.Where(t => t.Resource.Id == permission.Resource.Id).Select(DtoMapper.ConvertToPermission)
            });
        }

        return [];
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ResourcePermission>> GetResourcePermissionsToOthers(Guid partyId, Guid? toId = null, Guid? packageId = null, Guid? resourceId = null, CancellationToken cancellationToken = default)
    {
        var result = await DbContext.Connections
            .AsNoTracking()
            .Include(t => t.Package)
            .Include(t => t.Resource)
            .Include(t => t.Role)
            .Where(t => t.FromId == partyId)
            .WhereIf(toId.HasValue, t => t.ToId == toId.Value)
            .WhereIf(packageId.HasValue, t => t.PackageId == packageId.Value)
            .WhereIf(resourceId.HasValue, t => t.ResourceId == resourceId.Value)
            .ToListAsync(cancellationToken);

        if (result is { } && result.Any() && result.Where(r => r.Package is { }) is var packages)
        {
            return packages.DistinctBy(t => t.Resource.Id).Select(permission => new ResourcePermission()
            {
                Resource = permission.Resource,
                Permissions = packages.Where(t => t.Resource.Id == permission.Resource.Id).Select(DtoMapper.ConvertToPermission)
            });
        }

        return [];
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SystemUserClientConnectionDto>> GetConnectionsToAgent(Guid viaId, Guid toId, CancellationToken cancellationToken = default)
    {
        var result = await DbContext.Connections.AsNoTracking()
            .Include(t => t.Delegation)
            .Include(t => t.From)
            .ThenInclude(t => t.Type)
            .Include(t => t.From)
            .ThenInclude(t => t.Variant)
            .Include(t => t.Role)
            .Include(t => t.To)
            .ThenInclude(t => t.Type)
            .Include(t => t.To)
            .ThenInclude(t => t.Variant)
            .Include(t => t.Via)
            .ThenInclude(t => t.Type)
            .Include(t => t.Via)
            .ThenInclude(t => t.Variant)
            .Include(t => t.ViaRole)
            .Where(t => t.ToId == toId)
            .Where(t => t.ViaId == viaId)
            .ToListAsync(cancellationToken);

        return GetConnectionsAsSystemUserClientConnectionDto(result);
    }

    #region Mappers
    private IEnumerable<ConnectionPackageDto> ExtractRelationPackageDtoToOthers(IEnumerable<Connection> res, bool includeSubConnections = false)
    {
        return res.Where(t => t.Reason == "Direct").DistinctBy(t => t.ToId).Select(relation => new ConnectionPackageDto()
        {
            Party = DtoMapper.Convert(relation.To),
            Roles = res.Where(t => t.ToId == relation.ToId).Select(t => DtoMapper.ConvertCompactRole(t.Role)).DistinctBy(t => t.Id).ToList(),
            Packages = res.Where(t => t.ToId == relation.ToId && t.Package != null).Select(t => DtoMapper.ConvertCompactPackage(t.Package)).DistinctBy(t => t.Id).ToList(),
            Connections = includeSubConnections ? ExtractSubRelationDtoToOthers(res, relation.ToId).ToList() : new()
        });
    }

    private IEnumerable<ConnectionPackageDto> ExtractSubRelationPackageDtoFromOthers(IEnumerable<Connection> res, Guid party)
    {
        return res.Where(t => t.Reason != "Direct" && t.ViaId == party).DistinctBy(t => t.FromId).Select(relation => new ConnectionPackageDto()
        {
            Party = DtoMapper.Convert(relation.From),
            Roles = res.Where(t => t.FromId == relation.FromId).Select(t => DtoMapper.ConvertCompactRole(t.Role)).DistinctBy(t => t.Id).ToList(),
            Packages = res.Where(t => t.FromId == relation.FromId && t.Package != null).Select(t => DtoMapper.ConvertCompactPackage(t.Package)).DistinctBy(t => t.Id).ToList(),
            Connections = new()
        });
    }

    private IEnumerable<ConnectionDto> ExtractSubRelationDtoFromOthers(IEnumerable<Connection> res, Guid party)
    {
        return res.Where(t => t.Reason != "Direct" && t.ViaId == party).DistinctBy(t => t.FromId).Select(relation => new ConnectionDto()
        {
            Party = DtoMapper.Convert(relation.From),
            Roles = res.Where(t => t.FromId == relation.FromId).Select(t => DtoMapper.ConvertCompactRole(t.Role)).DistinctBy(t => t.Id).ToList(),
            Connections = new()
        });
    }

    private IEnumerable<ConnectionDto> ExtractRelationDtoToOthers(IEnumerable<Connection> res, bool includeSubConnections = false)
    {
        return res.Where(t => t.Reason == "Direct").DistinctBy(t => t.ToId).Select(relation => new ConnectionDto()
        {
            Party = DtoMapper.Convert(relation.To),
            Roles = res.Where(t => t.ToId == relation.ToId).Select(t => DtoMapper.ConvertCompactRole(t.Role)).DistinctBy(t => t.Id).ToList(),
            Connections = includeSubConnections ? ExtractSubRelationDtoToOthers(res, relation.ToId).ToList() : new()
        });
    }

    private IEnumerable<ConnectionDto> ExtractSubRelationDtoToOthers(IEnumerable<Connection> res, Guid party)
    {
        return res.Where(t => t.Reason != "Direct" && t.ViaId == party).DistinctBy(t => t.To.Id).Select(relation => new ConnectionDto()
        {
            Party = DtoMapper.Convert(relation.To),
            Roles = res.Where(t => t.ToId == relation.To.Id).Select(t => DtoMapper.ConvertCompactRole(t.Role)).DistinctBy(t => t.Id).ToList(),
            Connections = new()
        });
    }

    private IEnumerable<ConnectionPackageDto> ExtractSubRelationPackageDtoToOthers(IEnumerable<Connection> res, Guid party)
    {
        return res.Where(t => t.Reason != "Direct" && t.ViaId == party).DistinctBy(t => t.ToId).Select(relation => new ConnectionPackageDto()
        {
            Party = DtoMapper.Convert(relation.To),
            Roles = res.Where(t => t.ToId == relation.ToId).Select(t => DtoMapper.ConvertCompactRole(t.Role)).DistinctBy(t => t.Id).ToList(),
            Packages = res.Where(t => t.ToId == relation.ToId && t.Package != null).Select(t => DtoMapper.ConvertCompactPackage(t.Package)).DistinctBy(t => t.Id).ToList(),
            Connections = new()
        });
    }

    private IEnumerable<ConnectionDto> ExtractRelationDtoFromOthers(IEnumerable<Connection> res, bool includeSubConnections = false)
    {
        return res.DistinctBy(t => t.FromId).Select(relation => new ConnectionDto()
        {
            Party = DtoMapper.Convert(relation.From),
            Roles = res.Where(t => t.FromId == relation.FromId).Select(t => DtoMapper.ConvertCompactRole(t.Role)).DistinctBy(t => t.Id).ToList(),
            Connections = includeSubConnections ? ExtractSubRelationDtoFromOthers(res, relation.FromId).ToList() : new()
        });
    }

    private IEnumerable<ConnectionPackageDto> ExtractRelationPackageDtoFromOthers(IEnumerable<Connection> res, bool includeSubConnections = false)
    {
        return res.DistinctBy(t => t.FromId).Select(relation => new ConnectionPackageDto()
        {
            Party = DtoMapper.Convert(relation.From),
            Roles = res.Where(t => t.FromId == relation.FromId).Select(t => DtoMapper.ConvertCompactRole(t.Role)).DistinctBy(t => t.Id).ToList(),
            Packages = res.Where(t => t.FromId == relation.FromId && t.Package != null).Select(t => DtoMapper.ConvertCompactPackage(t.Package)).DistinctBy(t => t.Id).ToList(),
            Connections = includeSubConnections ? ExtractSubRelationDtoFromOthers(res, relation.FromId).ToList() : new()
        });
    }

    private IEnumerable<SystemUserClientConnectionDto> GetConnectionsAsSystemUserClientConnectionDto(IEnumerable<Connection> res)
    {
        return res.DistinctBy(t => t.DelegationId).Select(connection => new SystemUserClientConnectionDto()
        {
            Id = connection.DelegationId.Value,
            Delegation = connection.DelegationId.HasValue ? new SystemUserClientConnectionDto.ClientDelegation()
            {
                Id = connection.DelegationId.Value,
                FromId = connection.FromId,
                ToId = connection.ToId,
                FacilitatorId = connection.ViaId ?? Guid.Empty
            } : null,
            From = new SystemUserClientConnectionDto.Client()
            {
                Id = connection.From.Id,
                TypeId = connection.From.TypeId,
                VariantId = connection.From.VariantId,
                Name = connection.From.Name,
                RefId = connection.From.RefId,
                ParentId = connection.From.ParentId.HasValue ? connection.From.ParentId.Value : null
            },
            Role = new SystemUserClientConnectionDto.AgentRole()
            {
                Id = connection.Role.Id,
                Name = connection.Role.Name,
                Code = connection.Role.Code,
                Description = connection.Role.Description,
                IsKeyRole = connection.Role.IsKeyRole,
                Urn = connection.Role.Urn
            },
            To = new SystemUserClientConnectionDto.Agent()
            {
                Id = connection.To.Id,
                TypeId = connection.To.TypeId,
                VariantId = connection.To.VariantId,
                Name = connection.To.Name,
                RefId = connection.To.RefId,
                ParentId = connection.To.ParentId.HasValue ? connection.To.ParentId.Value : null
            },
            Facilitator = connection.ViaId.HasValue ? new SystemUserClientConnectionDto.ServiceProvider()
            {
                Id = connection.Via.Id,
                TypeId = connection.Via.TypeId,
                VariantId = connection.Via.VariantId,
                Name = connection.Via.Name,
                RefId = connection.Via.RefId,
                ParentId = connection.Via.ParentId.HasValue ? connection.Via.ParentId.Value : null
            } : null,
            FacilitatorRole = connection.ViaRoleId.HasValue ? new SystemUserClientConnectionDto.ServiceProviderRole()
            {
                Id = connection.ViaRole.Id,
                Name = connection.ViaRole.Name,
                Code = connection.ViaRole.Code,
                Description = connection.ViaRole.Description,
                IsKeyRole = connection.ViaRole.IsKeyRole,
                Urn = connection.ViaRole.Urn
            } : null
        });
    }
    #endregion
}
