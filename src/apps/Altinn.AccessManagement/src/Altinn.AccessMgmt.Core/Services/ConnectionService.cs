using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text;
using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Core.Enums.ResourceRegistry;
using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessManagement.Core.Helpers;
using Altinn.AccessManagement.Core.Models.AccessList;
using Altinn.AccessManagement.Core.Models.Party;
using Altinn.AccessManagement.Core.Models.Register;
using Altinn.AccessManagement.Core.Models.ResourceRegistry;
using Altinn.AccessManagement.Core.Models.Rights;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessMgmt.Core.Constants.Translation;
using Altinn.AccessMgmt.Core.Extensions;
using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.Core.Utils;
using Altinn.AccessMgmt.Core.Utils.Helper;
using Altinn.AccessMgmt.Core.Validation;
using Altinn.AccessMgmt.Persistence.Services.Models;
using Altinn.AccessMgmt.PersistenceEF.Audit;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Queries;
using Altinn.AccessMgmt.PersistenceEF.Queries.Connection;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Altinn.Authorization.ABAC.Xacml;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.Api.Contracts.AccessManagement.Enums;
using Altinn.Authorization.ProblemDetails;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.Core.Services;

/// <inheritdoc />
public partial class ConnectionService(
    AppDbContext dbContext,
    ConnectionQuery connectionQuery,
    IAuditAccessor auditAccessor,
    IAltinn2RightsClient altinn2Client,
    IAMPartyService partyService,
    IContextRetrievalService contextRetrievalService,
    IAccessListsAuthorizationClient accessListsAuthorizationClient,
    IPolicyRetrievalPoint policyRetrievalPoint,
    IRoleService roleService,
    ITranslationService translationService) : IConnectionService
{
    public async Task<Result<IEnumerable<ConnectionDto>>> Get(Guid party, Guid? fromId, Guid? toId, Action<ConnectionOptions> configureConnections = null, CancellationToken cancellationToken = default)
    {
        var options = new ConnectionOptions(configureConnections);
        var (from, to) = await GetFromAndToEntities(fromId, toId, cancellationToken);
        var problem = ValidateReadOpInput(fromId, from, toId, to, options);
        if (problem is { })
        {
            if (problem.ErrorCode == Problems.PartyNotFound.ErrorCode)
            {
                return new List<ConnectionDto>();
            }

            return problem;
        }

        var direction = party == fromId
            ? ConnectionQueryDirection.ToOthers
            : ConnectionQueryDirection.FromOthers;

        var connections = await connectionQuery.GetConnectionsAsync(
            new ConnectionQueryFilter()
            {
                FromIds = fromId.HasValue ? [fromId.Value] : null,
                ToIds = toId.HasValue ? [toId.Value] : null,
                EnrichEntities = true,
                IncludeSubConnections = true,
                IncludeKeyRole = true,
                IncludeMainUnitConnections = true,
                IncludeDelegation = true,
                IncludePackages = true,
                IncludeResource = false,
                EnrichPackageResources = false,
                ExcludeDeleted = false,
                OnlyUniqueResults = false
            },
            direction,
            true,
            cancellationToken
        );

        return direction == ConnectionQueryDirection.FromOthers
            ? DtoMapper.ConvertFromOthers(connections, getSingle: fromId.HasValue)
            : DtoMapper.ConvertToOthers(connections, getSingle: toId.HasValue);
    }

    public async Task<Result<AssignmentDto>> AddAssignment(Guid fromId, Guid toId, Action<ConnectionOptions> configureConnections = null, CancellationToken cancellationToken = default)
    {
        var options = new ConnectionOptions(configureConnections);
        var (from, to) = await GetFromAndToEntities(fromId, toId, cancellationToken);
        var problem = ValidateWriteOpInput(from, to, options);
        if (problem is { })
        {
            return problem;
        }

        var existingAssignment = await dbContext.Assignments
            .AsNoTracking()
            .Where(e => e.FromId == from.Id)
            .Where(e => e.ToId == to.Id)
            .Where(e => e.RoleId == RoleConstants.Rightholder)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingAssignment is { })
        {
            return DtoMapper.Convert(existingAssignment);
        }

        var assignment = new Assignment()
        {
            FromId = fromId,
            ToId = toId,
            RoleId = RoleConstants.Rightholder,
        };

        await dbContext.Assignments.AddAsync(assignment, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return DtoMapper.Convert(assignment);
    }

    public async Task<ValidationProblemInstance> RemoveAssignment(Guid fromId, Guid toId, bool cascade = false, Action<ConnectionOptions> configureConnections = null, CancellationToken cancellationToken = default)
    {
        var options = new ConnectionOptions(configureConnections);
        var (from, to) = await GetFromAndToEntities(fromId, toId, cancellationToken);
        var problem = ValidateWriteOpInput(from, to, options);
        if (problem is { })
        {
            return problem;
        }

        var existingAssignment = await dbContext.Assignments
            .AsTracking()
            .Where(e => e.FromId == fromId)
            .Where(e => e.ToId == toId)
            .Where(e => e.RoleId == RoleConstants.Rightholder)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingAssignment is null)
        {
            return null;
        }

        if (!cascade)
        {
            var assignedPackages = await dbContext.AssignmentPackages
                .AsNoTracking()
                .Where(p => p.AssignmentId == existingAssignment.Id)
                .ToListAsync(cancellationToken);

            var delegationsFrom = await dbContext.Delegations
                .AsNoTracking()
                .Where(p => p.FromId == existingAssignment.Id)
                .ToListAsync(cancellationToken);

            var delegationsTo = await dbContext.Delegations
                .AsNoTracking()
                .Where(p => p.ToId == toId)
                .ToListAsync(cancellationToken);

            problem = ValidationComposer.Validate(
                AssignmentPackageValidation.HasAssignedPackages(assignedPackages),
                DelegationValidation.HasDelegationsAssigned(delegationsFrom),
                DelegationValidation.HasDelegationsAssigned(delegationsTo)
            );

            if (problem is { })
            {
                return problem;
            }
        }

        dbContext.Remove(existingAssignment);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (from.PartyId.HasValue && to.PartyId.HasValue)
        {
            await altinn2Client.ClearReporteeRights(from.PartyId.Value, to.PartyId.Value, to.UserId.HasValue ? to.UserId.Value : 0, cancellationToken: cancellationToken);
        }

        return null;
    }

    #region Resources

    public async Task<Result<IEnumerable<ResourcePermissionDto>>> GetResources(Guid party, Guid? fromId, Guid? toId, Action<ConnectionOptions> configureConnections = null, CancellationToken cancellationToken = default)
    {
        var options = new ConnectionOptions(configureConnections);
        var (from, to) = await GetFromAndToEntities(fromId, toId, cancellationToken);
        var problem = ValidateReadOpInput(fromId, from, toId, to, options);
        if (problem is { })
        {
            if (problem.ErrorCode == Problems.PartyNotFound.ErrorCode)
            {
                return new List<ResourcePermissionDto>();
            }

            return problem;
        }

        var resources = await connectionQuery.GetConnectionsFromOthersAsync(
            new ConnectionQueryFilter()
            {
                FromIds = [from.Id],
                ToIds = [to.Id],
                EnrichPackageResources = false,
                IncludeDelegation = false,
                IncludeKeyRole = true,
                IncludeMainUnitConnections = true,
                IncludePackages = false,
                IncludeResource = true,
                IncludeSubConnections = true,
            });

        return DtoMapper.ConvertResources(resources);
    }

    public async Task<Result<AssignmentResourceDto>> AddResource(Guid fromId, Guid toId, string resourceId, int delegationChangeId, string policyPath, string policyVersion, Action<ConnectionOptions> configureConnection = null, CancellationToken cancellationToken = default)
    {
        var resource = await dbContext.Resources.AsNoTracking().FirstOrDefaultAsync(t => t.RefId == resourceId);
        return await AddResource(fromId, toId, resource.Id, delegationChangeId, policyPath, policyVersion, configureConnection, cancellationToken);
    }

    public async Task<Result<AssignmentResourceDto>> AddResource(Guid fromId, Guid toId, Guid resourceId, int delegationChangeId, string policyPath, string policyVersion, Action<ConnectionOptions> configureConnection = null, CancellationToken cancellationToken = default)
    {
        var options = new ConnectionOptions(configureConnection);
        var (from, to) = await GetFromAndToEntities(fromId, toId, cancellationToken);
        var problem = ValidateWriteOpInput(from, to, options);
        if (problem is { })
        {
            return problem;
        }

        var connection = await Get(fromId, fromId, toId, configureConnection, cancellationToken: cancellationToken);
        if (!connection.IsSuccess || connection.Value.Count() == 0)
        {
            return Problems.MissingConnection;
        }

        var check = await CheckResource(fromId, resourceIds: [resourceId], configureConnection, cancellationToken);
        if (check.IsProblem)
        {
            return check.Problem;
        }

        // Look for existing direct rightholder assignment
        var assignment = await dbContext.Assignments
            .Where(a => a.FromId == fromId)
            .Where(a => a.ToId == toId)
            .Where(a => a.RoleId == RoleConstants.Rightholder.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (assignment == null)
        {
            var hasConnection = await connectionQuery.HasConnection(fromId, toId);
            if (hasConnection.Result)
            {
                throw new Exception("No connection found between parties");
            }

            assignment = new Assignment()
            {
                FromId = fromId,
                ToId = toId,
                RoleId = RoleConstants.Rightholder.Id
            };

            await dbContext.Assignments.AddAsync(assignment, cancellationToken);
        }

        var assignmentResource = await dbContext.AssignmentResources.AsTracking()
                .Where(a => a.AssignmentId == assignment.Id)
                .Where(a => a.ResourceId == resourceId)
                .FirstOrDefaultAsync(cancellationToken);

        if (assignmentResource == null)
        {
            assignmentResource = new AssignmentResource()
            {
                AssignmentId = assignment.Id,
                ResourceId = resourceId,
                DelegationChangeId = delegationChangeId,
                PolicyPath = policyPath,
                PolicyVersion = policyVersion
            };
            await dbContext.AssignmentResources.AddAsync(assignmentResource, cancellationToken);
        }
        else
        {
            assignmentResource.PolicyPath = policyPath;
            assignmentResource.PolicyVersion = policyVersion;
            assignmentResource.DelegationChangeId = delegationChangeId;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (from.PartyId.HasValue && to.PartyId.HasValue)
        {
            await altinn2Client.ClearReporteeRights(from.PartyId.Value, to.PartyId.Value, to.UserId.HasValue ? to.UserId.Value : 0, cancellationToken: cancellationToken);
        }

        return DtoMapper.Convert(assignmentResource);
    }

    public async Task<ValidationProblemInstance> RemoveResource(Guid fromId, Guid toId, string resourceId, Action<ConnectionOptions> configureConnection = null, CancellationToken cancellationToken = default)
    {
        var resource = await dbContext.Resources.AsNoTracking().FirstOrDefaultAsync(t => t.RefId == resourceId);
        return await RemoveResource(fromId, toId, resource.Id, configureConnection, cancellationToken);
    }

    public async Task<ValidationProblemInstance> RemoveResource(Guid fromId, Guid toId, Guid resourceId, Action<ConnectionOptions> configureConnection = null, CancellationToken cancellationToken = default)
    {
        var options = new ConnectionOptions(configureConnection);
        var (from, to) = await GetFromAndToEntities(fromId, toId, cancellationToken);
        var problem = ValidateWriteOpInput(from, to, options);
        if (problem is { })
        {
            return problem;
        }

        var assignment = await dbContext.Assignments
            .AsNoTracking()
            .Include(a => a.From)
            .Include(a => a.To)
            .Where(a => a.FromId == fromId)
            .Where(a => a.ToId == toId)
            .Where(a => a.RoleId == RoleConstants.Rightholder)
            .FirstOrDefaultAsync(cancellationToken);

        if (assignment is null)
        {
            return null;
        }

        problem = ValidateWriteOpInput(assignment.From, assignment.To, options);
        if (problem is { })
        {
            return problem;
        }

        var existingAssignmentResources = await dbContext.AssignmentResources
            .AsTracking()
            .Where(a => a.AssignmentId == assignment.Id)
            .Where(a => a.ResourceId == resourceId)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingAssignmentResources is null)
        {
            return null;
        }

        dbContext.Remove(existingAssignmentResources);
        await dbContext.SaveChangesAsync(cancellationToken);

        return null;
    }

    public async Task<Result<Dictionary<Guid, bool>>> CheckResource(Guid party, IEnumerable<Guid> resourceIds = null, Action<ConnectionOptions> configureConnection = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<Dictionary<string, bool>>> CheckResource(Guid party, IEnumerable<string> resources, Action<ConnectionOptions> configureConnection = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    #endregion

    #region Packages

    public async Task<Result<IEnumerable<PackagePermissionDto>>> GetPackages(Guid party, Guid? fromId, Guid? toId, Action<ConnectionOptions> configureConnections = null, CancellationToken cancellationToken = default)
    {
        var options = new ConnectionOptions(configureConnections);
        var (from, to) = await GetFromAndToEntities(fromId, toId, cancellationToken);
        var problem = ValidateReadOpInput(fromId, from, toId, to, options);
        if (problem is { })
        {
            if (problem.ErrorCode == Problems.PartyNotFound.ErrorCode)
            {
                return new List<PackagePermissionDto>();
            }

            return problem;
        }

        var direction = party == fromId
            ? ConnectionQueryDirection.ToOthers
            : ConnectionQueryDirection.FromOthers;

        var connections = await connectionQuery.GetConnectionsAsync(
        new ConnectionQueryFilter()
        {
            FromIds = fromId.HasValue ? [fromId.Value] : null,
            ToIds = toId.HasValue ? [toId.Value] : null,
            EnrichEntities = true,
            IncludeSubConnections = true,
            IncludeKeyRole = true,
            IncludeMainUnitConnections = true,
            IncludeDelegation = true,
            IncludePackages = true,
            IncludeResource = false,
            EnrichPackageResources = false,
            ExcludeDeleted = false,
            OnlyUniqueResults = false
        },
        direction,
        true,
        cancellationToken
        );

        return DtoMapper.ConvertPackages(connections);
    }

    public async Task<Result<AssignmentPackageDto>> AddPackage(Guid fromId, Guid toId, Guid packageId, Action<ConnectionOptions> configureConnection = null, CancellationToken cancellationToken = default)
    {
        return await AddPackage(fromId, toId, packageId, "packageId", configureConnection, cancellationToken);
    }

    public async Task<Result<AssignmentPackageDto>> AddPackage(Guid fromId, Guid toId, string packageUrn, Action<ConnectionOptions> configureConnection = null, CancellationToken cancellationToken = default)
    {
        if (PackageConstants.TryGetByUrn(packageUrn, out var package))
        {
            return await AddPackage(fromId, toId, package.Id, "package", configureConnection, cancellationToken);
        }

        return ValidationComposer.Validate(PackageValidation.PackageExists(package, packageUrn));
    }

    public async Task<ValidationProblemInstance> RemovePackage(Guid fromId, Guid toId, string packageUrn, Action<ConnectionOptions> configureConnection = null, CancellationToken cancellationToken = default)
    {
        if (PackageConstants.TryGetByUrn(packageUrn, out var package))
        {
            return await RemovePackage(fromId, toId, package, configureConnection, cancellationToken);
        }

        return ValidationComposer.Validate(PackageValidation.PackageExists(package, packageUrn));
    }

    public async Task<ValidationProblemInstance> RemovePackage(Guid fromId, Guid toId, Guid packageId, Action<ConnectionOptions> configureConnection = null, CancellationToken cancellationToken = default)
    {
        var options = new ConnectionOptions(configureConnection);
        var (from, to) = await GetFromAndToEntities(fromId, toId, cancellationToken);
        var problem = ValidateWriteOpInput(from, to, options);
        if (problem is { })
        {
            return problem;
        }

        var assignment = await dbContext.Assignments
            .AsNoTracking()
            .Include(a => a.From)
            .Include(a => a.To)
            .Where(a => a.FromId == fromId)
            .Where(a => a.ToId == toId)
            .Where(a => a.RoleId == RoleConstants.Rightholder)
            .FirstOrDefaultAsync(cancellationToken);

        if (assignment is null)
        {
            return null;
        }

        problem = ValidateWriteOpInput(assignment.From, assignment.To, options);
        if (problem is { })
        {
            return problem;
        }

        var existingAssignmentPackages = await dbContext.AssignmentPackages
            .AsTracking()
            .Where(a => a.AssignmentId == assignment.Id)
            .Where(a => a.PackageId == packageId)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingAssignmentPackages is null)
        {
            return null;
        }

        dbContext.Remove(existingAssignmentPackages);
        await dbContext.SaveChangesAsync(cancellationToken);

        return null;
    }

    private async Task<Result<AssignmentPackageDto>> AddPackage(Guid fromId, Guid toId, Guid packageId, string queryParamName, Action<ConnectionOptions> configureConnection = null, CancellationToken cancellationToken = default)
    {
        var options = new ConnectionOptions(configureConnection);
        var (from, to) = await GetFromAndToEntities(fromId, toId, cancellationToken);
        var problem = ValidateWriteOpInput(from, to, options);
        if (problem is { })
        {
            return problem;
        }

        var connection = await Get(fromId, fromId, toId, configureConnection, cancellationToken: cancellationToken);
        if (!connection.IsSuccess || connection.Value.Count() == 0)
        {
            return Problems.MissingConnection;
        }

        var check = await CheckPackage(fromId, packageIds: [packageId], configureConnection, cancellationToken);
        if (check.IsProblem)
        {
            return check.Problem;
        }

        problem = ValidationComposer.Validate(
            PackageValidation.AuthorizePackageAssignment(check.Value),
            PackageValidation.PackageIsAssignableToRecipient(check.Value.Select(p => p.Package.Urn), to.Type, queryParamName)
        );

        if (problem is { })
        {
            return problem;
        }

        // Look for existing direct rightholder assignment
        var assignment = await dbContext.Assignments
            .AsNoTracking()
            .Where(a => a.FromId == fromId)
            .Where(a => a.ToId == toId)
            .Where(a => a.RoleId == RoleConstants.Rightholder.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (assignment == null)
        {
            assignment = new Assignment()
            {
                FromId = fromId,
                ToId = toId,
                RoleId = RoleConstants.Rightholder
            };

            await dbContext.Assignments.AddAsync(assignment, cancellationToken);
        }
        else
        {
            var existingAssignmentPackage = await dbContext.AssignmentPackages
                .AsNoTracking()
                .Where(a => a.AssignmentId == assignment.Id)
                .Where(a => a.PackageId == packageId)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingAssignmentPackage is { })
            {
                return DtoMapper.Convert(existingAssignmentPackage);
            }
        }

        var newAssignmentPackage = new AssignmentPackage()
        {
            AssignmentId = assignment.Id,
            PackageId = packageId,
        };

        await dbContext.AssignmentPackages.AddAsync(newAssignmentPackage, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (from.PartyId.HasValue && to.PartyId.HasValue)
        {
            await altinn2Client.ClearReporteeRights(from.PartyId.Value, to.PartyId.Value, to.UserId.HasValue ? to.UserId.Value : 0, cancellationToken: cancellationToken);
        }

        return DtoMapper.Convert(newAssignmentPackage);
    }

    public async Task<Result<IEnumerable<AccessPackageDto.AccessPackageDtoCheck>>> CheckPackage(Guid party, IEnumerable<Guid> packageIds = null, Action<ConnectionOptions> configureConnection = null, CancellationToken cancellationToken = default)
    {
        var assignablePackages = await dbContext.GetAssignableAccessPackages(
            party,
            auditAccessor.AuditValues.ChangedBy,
            packageIds,
            ct: cancellationToken
        );

        return assignablePackages.GroupBy(p => p.Package.Id).Select(group =>
        {
            var firstPackage = group.First();
            return new AccessPackageDto.AccessPackageDtoCheck
            {
                Package = new AccessPackageDto
                {
                    Id = firstPackage.Package.Id,
                    Urn = firstPackage.Package.Urn,
                    AreaId = firstPackage.Package.AreaId
                },
                Result = group.Any(p => p.Result),
                Reasons = group.Select(p => new AccessPackageDto.AccessPackageDtoCheck.Reason
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

    public async Task<Result<IEnumerable<AccessPackageDto.AccessPackageDtoCheck>>> CheckPackageForResource(Guid party, IEnumerable<Guid> packageIds = null, Action<ConnectionOptions> configureConnection = null, CancellationToken cancellationToken = default)
    {
        var assignablePackages = await dbContext.GetAssignableAccessPackages(
            party,
            auditAccessor.AuditValues.ChangedBy,
            packageIds,
            true,
            cancellationToken
        );

        return assignablePackages.GroupBy(p => p.Package.Id).Select(group =>
        {
            var firstPackage = group.First();
            return new AccessPackageDto.AccessPackageDtoCheck
            {
                Package = new AccessPackageDto
                {
                    Id = firstPackage.Package.Id,
                    Urn = firstPackage.Package.Urn,
                    AreaId = firstPackage.Package.AreaId
                },
                Result = group.Any(p => p.Result),
                Reasons = group.Select(p => new AccessPackageDto.AccessPackageDtoCheck.Reason
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

    public async Task<Result<IEnumerable<AccessPackageDto.AccessPackageDtoCheck>>> CheckPackage(Guid party, IEnumerable<string> packages, IEnumerable<Guid> packageIds = null, Action<ConnectionOptions> configureConnection = null, CancellationToken cancellationToken = default)
    {
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

        return await CheckPackage(party, [.. packageIds, .. packagesFound.Select(p => p.Id)], configureConnection, cancellationToken);
    }

    #endregion

    private async Task<(Entity From, Entity To)> GetFromAndToEntities(Guid? fromId, Guid? toId, CancellationToken cancellationToken)
    {
        if (fromId is null && toId is null)
        {
            throw new UnreachableException();
        }

        var entities = await dbContext.Entities
            .AsNoTracking()
            .Where(e => e.Id == fromId || e.Id == toId)
            .Include(e => e.Type)
            .ToListAsync(cancellationToken);

        var fromEntity = entities.FirstOrDefault(e => e.Id == fromId);
        var toEntity = entities.FirstOrDefault(e => e.Id == toId);

        return (fromEntity, toEntity);
    }

    private ValidationProblemInstance? ValidateWriteOpInput(Entity from, Entity to, ConnectionOptions options)
    {
        var problem = ValidationComposer.Validate(
            EntityValidation.FromExists(from),
            EntityValidation.ToExists(to)
        );

        if (problem is { })
        {
            return problem;
        }

        if (options.AllowedWriteFromEntityTypes.Any() && options.AllowedWriteToEntityTypes.Any())
        {
            problem = ValidationComposer.Validate(
                EntityTypeValidation.FromIsOfType(from.TypeId, [.. options.AllowedWriteFromEntityTypes]),
                EntityTypeValidation.ToIsOfType(to.TypeId, [.. options.AllowedWriteToEntityTypes])
            );
        }
        else if (options.AllowedWriteFromEntityTypes.Any())
        {
            problem = ValidationComposer.Validate(
                EntityTypeValidation.FromIsOfType(from.TypeId, [.. options.AllowedWriteFromEntityTypes])
            );
        }
        else if (options.AllowedWriteToEntityTypes.Any())
        {
            problem = ValidationComposer.Validate(
                EntityTypeValidation.ToIsOfType(to.TypeId, [.. options.AllowedWriteToEntityTypes])
            );
        }

        return problem;
    }

    private ProblemInstance ValidateReadOpInput(Guid? fromId, Entity? from, Guid? toId, Entity? to, ConnectionOptions options)
    {
        if (from is { })
        {
            var problem = ValidationComposer.Validate(
                EntityTypeValidation.FromIsOfType(from.TypeId, [.. options.AllowedReadFromEntityTypes])
            );

            if (problem is { })
            {
                return problem;
            }
        }

        if (to is { })
        {
            var problem = ValidationComposer.Validate(
                EntityTypeValidation.ToIsOfType(to.TypeId, [.. options.AllowedReadToEntityTypes])
            );

            if (problem is { })
            {
                return problem;
            }
        }

        if (to is null && from is null)
        {
            return Problems.ConnectionEntitiesDoNotExist;
        }

        if (toId.HasValue && to is null)
        {
            return Problems.PartyNotFound;
        }

        if (fromId.HasValue && from is null)
        {
            return Problems.PartyNotFound;
        }

        return null;
    }

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<RolePermissionDto>>> GetRoles(Guid party, Guid? fromId, Guid? toId, Action<ConnectionOptions> configureConnections, CancellationToken cancellationToken)
    {
        if (!fromId.HasValue && !toId.HasValue)
        {
            return Problems.PartyNotFound;
        }

        var direction = party == fromId
            ? ConnectionQueryDirection.ToOthers
            : ConnectionQueryDirection.FromOthers;

        var filter = new ConnectionQueryFilter()
        {
            FromIds = fromId.HasValue ? [fromId.Value] : null,
            ToIds = toId.HasValue ? [toId.Value] : null,
            EnrichEntities = true,
            IncludeSubConnections = true,
            IncludeKeyRole = true,
            IncludeMainUnitConnections = true,
            IncludeDelegation = false,
            IncludePackages = false,
            IncludeResource = false,
            EnrichPackageResources = false,
            ExcludeDeleted = false
        };

        var connections = await connectionQuery.GetConnectionsAsync(filter, direction, true, cancellationToken);
        return connections.GroupBy(r => r.RoleId).Select(connection =>
        {
            var role = connection.First().Role;
            return new RolePermissionDto
            {
                Role = DtoMapper.Convert(role),
                Permissions = connection.Select(connection => DtoMapper.ConvertToPermission(connection)),
            };
        }).ToList();
    }

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<RoleDtoCheck>>> RoleDelegationCheck(Guid party, Guid? toId = null, bool toIsMainAdminForFrom = false, CancellationToken cancellationToken = default)
    {
        toId = toId ?? auditAccessor.AuditValues.ChangedBy;

        var results = await dbContext.GetRolesForResourceDelegationCheck(
            fromId: party,
            toId: toId.Value,
            toIsMainAdminForFrom,
            ct: cancellationToken
        );

        var roles = await roleService.GetById(results.Select(r => r.Role.Id), cancellationToken);

        var translated = await roles.TranslateDeepAsync(
            translationService,
            TranslationConstants.DefaultLanguageCode,
            true);

        return results.GroupBy(p => p.Role.Id).Select(group =>
        {
            var firstRole = group.First();
            return new RoleDtoCheck
            {
                Role = translated.FirstOrDefault(r => r.Id == firstRole.Role.Id),
                Result = group.Any(p => p.Result),
                Reasons = group.Select(p => new RoleDtoCheck.Reason
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

    /// <inheritdoc />
    public async Task<Result<ResourceCheckDto>> ResourceDelegationCheck(Guid authenticatedUserUuid, Guid party, string resourceId, Action<ConnectionOptions> configureConnection = null, CancellationToken cancellationToken = default)
    {
        // Get fromParty
        MinimalParty fromParty = await partyService.GetByUid(party, cancellationToken);

        ResourceDto resource;
        XacmlPolicy policy;

        try
        {
            // Fetch resource
            resource = await FetchResource(resourceId, cancellationToken);

            // Fetch policy for the resource
            policy = await GetPolicy(resourceId, cancellationToken);
        }
        catch (ValidationException)
        {
            return Problems.InvallidResource;
        }

        // Fetch Resourcemetadata
        bool isApp = DelegationCheckHelper.IsAppResourceId(resourceId, out string org, out string app);
        ServiceResource resourceMetadata = await contextRetrievalService.GetResourceFromResourceList(resourceId, isApp ? org : null, isApp ? app : null);
        ResourceAccessListMode accessListMode = resourceMetadata.AccessListMode;
        bool isResourceDelegable = resourceMetadata.Delegable;
        
        // Decompose policy into resource/tasks
        List<ActionAccess> actionAccesses = DelegationCheckHelper.DecomposePolicy(policy, resourceId);

        // Fetch packages
        var packages = await CheckPackageForResource(party, null, ConfigureConnections, cancellationToken);

        bool isMainAdminForFrom = packages.Value.Any(p => p.Result == true && p.Package.Id == PackageConstants.MainAdministrator.Id);

        // Fetch roles
        var roles = await RoleDelegationCheck(party, authenticatedUserUuid, isMainAdminForFrom, cancellationToken);

        // Fetch resource rights
        //// var resources = (await GetResources(party, party, authenticatedUserUuid, ConfigureConnections, cancellationToken)).Value.Where(r => r.Resource.Id == resource.Id);

        ProcessTheAccessToTheAccessKeys(actionAccesses, packages.Value, roles.Value);

        ////AddDirectDelegationRights(actionAccesses, resources);

        // Map to result
        IEnumerable<ActionDto> actions = await MapFromInternalToExternalActions(actionAccesses, resourceId, accessListMode, fromParty, isResourceDelegable, cancellationToken);

        // build reult with reason based on roles, packages, resource rights and users delegable
        ResourceCheckDto resourceCheckDto = new ResourceCheckDto
        {
            Resource = resource,
            Actions = actions
        };

        return resourceCheckDto;
    }

    private string GetActionNameFromKey(string key, string resourceId)
    {
        string[] parts = key.Split("urn:", options: StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        StringBuilder sb = new StringBuilder();

        foreach (string part in parts)
        {
            string currentPart = part;
            if (currentPart.Substring(currentPart.Length - 1, 1) == ":")
            {
                currentPart = currentPart.Substring(0, currentPart.Length - 1);
            }

            int removeBefore = currentPart.LastIndexOf(':');
            if (removeBefore > -1)
            {
                currentPart = currentPart.Substring(currentPart.LastIndexOf(':') + 1);
            }

            if (currentPart.Equals(resourceId, StringComparison.InvariantCultureIgnoreCase))
            {
                continue;
            }

            sb.Append(UppercaseFirstLetter(currentPart));
            sb.Append(' ');
        }

        if (sb.Length > 0)
        {
            sb.Remove(sb.Length - 1, 1);
        }

        return sb.ToString();
    }

    private string UppercaseFirstLetter(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        return char.ToUpper(input[0]) + input.Substring(1);
    }

    private async Task<ActionDto> MapFromInternalToExternalAction(ActionAccess actionAccess, string resourceId, ResourceAccessListMode accessListMode, MinimalParty fromParty, bool isResourceDelegable, CancellationToken cancellationToken)
    {
        if (DelegationCheckHelper.IsAccessListModeEnabledAndApplicable(accessListMode, fromParty.PartyType))
        {
            string actionValue = actionAccess.ActionKey.Substring(actionAccess.ActionKey.LastIndexOf(":") + 1);
            AccessListAuthorizationRequest accessListAuthorizationRequest = new AccessListAuthorizationRequest
            {
                Subject = PartyUrn.PartyUuid.Create(fromParty.PartyUuid),
                Resource = ResourceIdUrn.ResourceId.Create(ResourceIdentifier.CreateUnchecked(resourceId)),
                Action = ActionUrn.ActionId.Create(ActionIdentifier.CreateUnchecked(actionValue))
            };

            AccessListAuthorizationResponse accessListAuthorizationResponse = await accessListsAuthorizationClient.AuthorizePartyForAccessList(accessListAuthorizationRequest, cancellationToken);
            AccessListAuthorizationResult accessListAuthorizationResult = accessListAuthorizationResponse.Result;
            if (accessListAuthorizationResult != AccessListAuthorizationResult.Authorized)
            {
                actionAccess.AccessListDenied = true;
            }
        }

        ActionDto currentAction = new ActionDto
        {
            ActionKey = actionAccess.ActionKey,
            ActionName = GetActionNameFromKey(actionAccess.ActionKey, resourceId),
            Result = false,
        };

        List<ActionDto.Reason> reasons = [];

        if (actionAccess.PackageAllowAccess.Count == 0 && actionAccess.RoleAllowAccess.Count == 0 && actionAccess.ResourceAllowAccess.Count == 0)
        {
            currentAction.Result = false;

            reasons.AddRange(actionAccess.PackageDenyAccess);
            reasons.AddRange(actionAccess.RoleDenyAccess);
            reasons.AddRange(actionAccess.ResourceDenyAccess);
        }
        else
        {
            currentAction.Result = true;

            ProcessPackageAllowAccessReasons(actionAccess.PackageAllowAccess, reasons);
            ProcessPackageAllowAccessReasons(actionAccess.RoleAllowAccess, reasons);
        }

        if (!isResourceDelegable)
        {
            currentAction.Result = false;
            ActionDto.Reason reason = new ActionDto.Reason
            {
                Description = $"Resource-NotDelegable",
                ReasonKey = DelegationCheckReasonCode.ResourceNotDelegable,
            };
            reasons.Add(reason);
        }

        if (actionAccess.AccessListDenied == true)
        {
            currentAction.Result = false;
            ActionDto.Reason reason = new ActionDto.Reason
            {
                Description = "Resource-AccessList-Enabled-NotListed",
                ReasonKey = DelegationCheckReasonCode.AccessListValidationFail,
            };
            reasons.Add(reason);
        }

        currentAction.Reasons = reasons;
        return currentAction;
    }

    private void ProcessPackageAllowAccessReasons(List<RoleDtoCheck> rolesAllowAccess, List<ActionDto.Reason> reasons)
    {
        if (rolesAllowAccess.Count > 0)
        {
            foreach (var roleAllowAccess in rolesAllowAccess)
            {
                foreach (var roleReason in roleAllowAccess.Reasons)
                {
                    ActionDto.Reason reason = new ActionDto.Reason
                    {
                        Description = roleReason.Description,
                        ReasonKey = DelegationCheckReasonCode.RoleAccess,
                        FromName = roleReason.FromName,
                        FromId = roleReason.FromId,
                        ToId = roleReason.ToId,
                        RoleId = roleReason.RoleId,
                        RoleUrn = roleReason.RoleUrn,
                        ToName = roleReason.ToName,
                        ViaId = roleReason.ViaId,
                        ViaName = roleReason.ViaName,
                        ViaRoleId = roleReason.ViaRoleId,
                        ViaRoleUrn = roleReason.ViaRoleUrn,
                    };

                    reasons.Add(reason);
                }
            }
        }
    }

    private void ProcessPackageAllowAccessReasons(List<AccessPackageDto.AccessPackageDtoCheck> packagesAllowAccess, List<ActionDto.Reason> reasons)
    {
        if (packagesAllowAccess.Count > 0)
        {
            foreach (var packageAllowAccess in packagesAllowAccess)
            {
                foreach (var packageReason in packageAllowAccess.Reasons)
                {
                    ActionDto.Reason reason = new ActionDto.Reason
                    {
                        Description = packageReason.Description,
                        ReasonKey = DelegationCheckReasonCode.PackageAccess,
                        FromName = packageReason.FromName,
                        PackageId = packageAllowAccess.Package.Id,
                        PackageUrn = packageAllowAccess.Package.Urn,
                        FromId = packageReason.FromId,
                        ToId = packageReason.ToId,
                        RoleId = packageReason.RoleId,
                        RoleUrn = packageReason.RoleUrn,
                        ToName = packageReason.ToName,
                        ViaId = packageReason.ViaId,
                        ViaName = packageReason.ViaName,
                        ViaRoleId = packageReason.ViaRoleId,
                        ViaRoleUrn = packageReason.ViaRoleUrn,
                    };

                    reasons.Add(reason);
                }
            }
        }
    }

    private async Task<IEnumerable<ActionDto>> MapFromInternalToExternalActions(List<ActionAccess> actionAccesses, string resourceId, ResourceAccessListMode accessListMode, MinimalParty fromParty, bool isResourceDelegable, CancellationToken cancellationToken = default)
    {
        List<ActionDto> actions = [];

        foreach (var actionAccess in actionAccesses)
        {
            actions.Add(await MapFromInternalToExternalAction(actionAccess, resourceId, accessListMode, fromParty, isResourceDelegable, cancellationToken));
        }

        return actions;
    }

    private void ProcessTheAccessToTheAccessKeys(List<ActionAccess> actionAccesses, IEnumerable<AccessPackageDto.AccessPackageDtoCheck> packages, IEnumerable<RoleDtoCheck> roles)
    {
        foreach (var actionAccess in actionAccesses)
        {
            foreach (var accessorUrn in actionAccess.AccessorUrns)
            {
                AccessPackageDto.AccessPackageDtoCheck package = packages.FirstOrDefault(p => p.Package.Urn.Equals(accessorUrn, StringComparison.InvariantCultureIgnoreCase));
                RoleDtoCheck role = roles.FirstOrDefault(r => r.Role.Urn.Equals(accessorUrn, StringComparison.InvariantCultureIgnoreCase));
                if (role == null && package == null)
                {
                    role = roles.FirstOrDefault(r => accessorUrn.Equals(r.Role.LegacyUrn, StringComparison.InvariantCultureIgnoreCase));
                }

                if (package != null)
                {
                    if (package.Result)
                    {
                        actionAccess.PackageAllowAccess.Add(package);
                    }
                    else
                    {
                        ActionDto.Reason reason = new ActionDto.Reason
                        {
                            Description = $"Missing-Package",
                            ReasonKey = DelegationCheckReasonCode.MissingPackageAccess,
                            PackageId = package.Package.Id,
                            PackageUrn = package.Package.Urn
                        };

                        actionAccess.PackageDenyAccess.Add(reason);
                    }
                }

                if (role != null)
                {
                    if (role.Result)
                    {
                        actionAccess.RoleAllowAccess.Add(role);
                    }
                    else
                    {
                        ActionDto.Reason reason = new ActionDto.Reason
                        {
                            Description = $"Missing-Role",
                            ReasonKey = DelegationCheckReasonCode.MissingRoleAccess,
                            RoleId = role.Role.Id,
                            RoleUrn = role.Role.Urn,                            
                        };

                        actionAccess.RoleDenyAccess.Add(reason);
                    }
                }
            }
        }
    }

    private Action<ConnectionOptions> ConfigureConnections { get; } = options =>
    {
        options.AllowedWriteFromEntityTypes = [EntityTypeConstants.Organization];
        options.AllowedWriteToEntityTypes = [EntityTypeConstants.Organization, EntityTypeConstants.Person];
        options.AllowedReadFromEntityTypes = [EntityTypeConstants.Organization, EntityTypeConstants.Person];
        options.AllowedReadToEntityTypes = [EntityTypeConstants.Organization, EntityTypeConstants.Person];
        options.FilterFromEntityTypes = [];
        options.FilterToEntityTypes = [];
    };

    private async Task<ResourceDto> FetchResource(string resourceId, CancellationToken cancellationToken)
    {
        Resource resource = await dbContext.Resources.AsNoTracking().SingleOrDefaultAsync(r => r.RefId == resourceId, cancellationToken);

        if (resource is null)
        {
            throw new ValidationException($"Resource with id '{resourceId}' not found");
        }

        Provider provider = await dbContext.Providers.AsNoTracking().SingleOrDefaultAsync(p => p.Id == resource.ProviderId, cancellationToken);
        ProviderTypeConstants.TryGetById(provider.TypeId, out var providerType);
        PersistenceEF.Models.ResourceType resourceType = await dbContext.ResourceTypes.SingleOrDefaultAsync(rt => rt.Id == resource.TypeId, cancellationToken);

        ProviderDto providerDto = new ProviderDto
        {
            Id = provider.Id,
            Code = provider.Code,
            LogoUrl = provider.LogoUrl,
            Name = provider.Name,
            RefId = provider.RefId,
            TypeId = provider.TypeId,
            Type = new ProviderTypeDto { Id = providerType.Id, Name = providerType.Entity.Name }
        };

        ResourceDto resourceDto = new ResourceDto
        {
            Id = resource.Id,
            Name = resource.Name,
            Description = resource.Description,
            Provider = providerDto,
            ProviderId = provider.Id,
            RefId = resource.RefId,
            TypeId = resource.TypeId,
            Type = new ResourceTypeDto { Id = resourceType.Id, Name = resourceType.Name }
        };

        return resourceDto;
    }

    private async Task<XacmlPolicy> GetPolicy(string resourceId, CancellationToken cancellationToken = default)
    {
        XacmlPolicy policy = null;

        if (string.IsNullOrEmpty(resourceId))
        {
            throw new ValidationException($"ResourceId cannot be null or empty");
        }

        bool isApp = DelegationCheckHelper.IsAppResourceId(resourceId, out string org, out string app);

        if (isApp)
        {
            policy = await policyRetrievalPoint.GetPolicyAsync(org, app, cancellationToken);
        }
        else
        {
            policy = await policyRetrievalPoint.GetPolicyAsync(resourceId, cancellationToken);
        }

        if (policy == null)
        {
            throw new ValidationException($"No valid policy found for the specified resource");
        }

        return policy;
    }
}

/// <summary>
/// Partial ConnectionService
/// </summary>
public partial class ConnectionService
{
    /// <inheritdoc />
    public async Task<IEnumerable<ConnectionDto>> GetConnectionsToOthers(
        Guid partyId,
        Guid? toId = null,
        Guid? roleId = null,
        Action<ConnectionOptions> configureConnections = null,
        CancellationToken cancellationToken = default)
    {
        var options = new ConnectionOptions(configureConnections);
        var result = await dbContext.Connections.AsNoTracking()
            .IncludeExtendedEntities()
            .Include(t => t.Role)
            .WhereIf(options.FilterFromEntityTypes.Any(), c => options.FilterFromEntityTypes.Contains(c.From.TypeId))
            .WhereIf(options.FilterToEntityTypes.Any(), c => options.FilterToEntityTypes.Contains(c.To.TypeId))
            .Where(t => t.FromId == partyId)
            .WhereIf(toId.HasValue, t => t.ToId == toId.Value)
            .WhereIf(roleId.HasValue, t => t.RoleId == roleId.Value)
            .ToListAsync(cancellationToken);

        return ExtractRelationDtoToOthers(result, includeSubConnections: false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ConnectionDto>> GetConnectionsFromOthers(
        Guid partyId,
        Guid? fromId = null,
        Guid? roleId = null,
        Action<ConnectionOptions> configureConnections = null,
        CancellationToken cancellationToken = default)
    {
        var options = new ConnectionOptions(configureConnections);
        var result = await dbContext.Connections.AsNoTracking()
            .IncludeExtendedEntities()
            .Include(c => c.Role)
            .Where(t => t.ToId == partyId)
            .WhereIf(options.FilterFromEntityTypes.Any(), c => options.FilterFromEntityTypes.Contains(c.From.TypeId))
            .WhereIf(options.FilterToEntityTypes.Any(), c => options.FilterToEntityTypes.Contains(c.To.TypeId))
            .WhereIf(fromId.HasValue, t => t.FromId == fromId.Value)
            .WhereIf(roleId.HasValue, t => t.RoleId == roleId.Value)
            .ToListAsync(cancellationToken);

        return ExtractRelationDtoFromOthers(result, includeSubConnections: false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<PackagePermissionDto>> GetPackagePermissionsFromOthers(
        Guid partyId,
        Guid? fromId = null,
        Guid? packageId = null,
        Action<ConnectionOptions> configureConnections = null,
        CancellationToken cancellationToken = default)
    {
        var options = new ConnectionOptions(configureConnections);
        var result = await dbContext.Connections
            .AsNoTracking()
            .IncludeExtendedEntities()
            .Include(t => t.Package)
            .Include(t => t.Via)
            .Include(t => t.ViaRole)
            .Include(t => t.Role)
            .Where(c => c.ToId == partyId)
            .WhereIf(options.FilterFromEntityTypes.Any(), c => options.FilterFromEntityTypes.Contains(c.From.TypeId))
            .WhereIf(options.FilterToEntityTypes.Any(), c => options.FilterToEntityTypes.Contains(c.To.TypeId))
            .WhereIf(fromId.HasValue, c => c.FromId == fromId.Value)
            .WhereIf(packageId.HasValue, c => c.PackageId == packageId.Value)
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
    public async Task<IEnumerable<PackagePermissionDto>> GetPackagePermissionsToOthers(
        Guid partyId,
        Guid? toId = null,
        Guid? packageId = null,
        Action<ConnectionOptions> configureConnections = null,
        CancellationToken cancellationToken = default)
    {
        var options = new ConnectionOptions(configureConnections);
        var result = await dbContext.Connections
            .AsNoTracking()
            .IncludeExtendedEntities()
            .Include(t => t.Package)
            .Include(t => t.Via)
            .Include(t => t.ViaRole)
            .Include(t => t.Role)
            .Where(t => t.FromId == partyId)
            .WhereIf(options.FilterFromEntityTypes.Any(), c => options.FilterFromEntityTypes.Contains(c.From.TypeId))
            .WhereIf(options.FilterToEntityTypes.Any(), c => options.FilterToEntityTypes.Contains(c.To.TypeId))
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
    public async Task<IEnumerable<ResourcePermission>> GetResourcePermissionsFromOthers(
        Guid partyId,
        Guid? fromId = null,
        Guid? packageId = null,
        Guid? resourceId = null,
        Action<ConnectionOptions> configureConnections = null,
        CancellationToken cancellationToken = default)
    {
        var options = new ConnectionOptions(configureConnections);
        var result = await dbContext.Connections
            .AsNoTracking()
            .IncludeExtendedEntities()
            .Include(t => t.Package)
            .Include(t => t.Role)
            .Include(t => t.Resource)
            .Where(t => t.ToId == partyId)
            .WhereIf(options.FilterFromEntityTypes.Any(), c => options.FilterFromEntityTypes.Contains(c.From.TypeId))
            .WhereIf(options.FilterToEntityTypes.Any(), c => options.FilterToEntityTypes.Contains(c.To.TypeId))
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
    public async Task<IEnumerable<ResourcePermission>> GetResourcePermissionsToOthers(
        Guid partyId,
        Guid? toId = null,
        Guid? packageId = null,
        Guid? resourceId = null,
        Action<ConnectionOptions> configureConnections = null,
        CancellationToken cancellationToken = default)
    {
        var options = new ConnectionOptions(configureConnections);
        var result = await dbContext.Connections
            .AsNoTracking()
            .IncludeExtendedEntities()
            .Include(t => t.Package)
            .Include(t => t.Resource)
            .Include(t => t.Role)
            .Where(t => t.FromId == partyId)
            .WhereIf(options.FilterFromEntityTypes.Any(), c => options.FilterFromEntityTypes.Contains(c.From.TypeId))
            .WhereIf(options.FilterToEntityTypes.Any(), c => options.FilterToEntityTypes.Contains(c.To.TypeId))
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
        var result = await dbContext.Connections
            .AsNoTracking()
            .IncludeExtendedEntities()
            .Include(t => t.Delegation)
            .Include(t => t.Role)
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

    private IEnumerable<ConnectionDto> ExtractRelationDtoFromOthers(IEnumerable<Connection> res, bool includeSubConnections = false)
    {
        return res.DistinctBy(t => t.FromId).Select(relation => new ConnectionDto()
        {
            Party = DtoMapper.Convert(relation.From),
            Roles = res.Where(t => t.FromId == relation.FromId).Select(t => DtoMapper.ConvertCompactRole(t.Role)).DistinctBy(t => t.Id).ToList(),
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
            }
            : null,
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
            }
            : null,
            FacilitatorRole = connection.ViaRoleId.HasValue ? new SystemUserClientConnectionDto.ServiceProviderRole()
            {
                Id = connection.ViaRole.Id,
                Name = connection.ViaRole.Name,
                Code = connection.ViaRole.Code,
                Description = connection.ViaRole.Description,
                IsKeyRole = connection.ViaRole.IsKeyRole,
                Urn = connection.ViaRole.Urn
            }
            : null
        });
    }
    #endregion
}

/// <summary>
/// Configures Logic for Connection Services
/// </summary>
public sealed class ConnectionOptions
{
    internal ConnectionOptions(Action<ConnectionOptions> configureConnectionService)
    {
        if (configureConnectionService is { })
        {
            configureConnectionService(this);
        }
    }

    public IEnumerable<Guid> AllowedWriteFromEntityTypes { get; set; } = [];

    public IEnumerable<Guid> AllowedWriteToEntityTypes { get; set; } = [];

    public IEnumerable<Guid> AllowedReadFromEntityTypes { get; set; } = [];

    public IEnumerable<Guid> AllowedReadToEntityTypes { get; set; } = [];

    public IEnumerable<Guid> FilterToEntityTypes { get; set; } = [];

    public IEnumerable<Guid> FilterFromEntityTypes { get; set; } = [];
}
