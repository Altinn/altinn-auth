using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Core.Enums.ResourceRegistry;
using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Models.AccessList;
using Altinn.AccessManagement.Core.Models.Party;
using Altinn.AccessManagement.Core.Models.Register;
using Altinn.AccessManagement.Core.Models.ResourceRegistry;
using Altinn.AccessManagement.Core.Models.Rights;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessMgmt.Core.Appsettings;
using Altinn.AccessMgmt.Core.Constants.Translation;
using Altinn.AccessMgmt.Core.Extensions;
using Altinn.AccessMgmt.Core.Notifications;
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
using Altinn.AccessMgmt.PersistenceEF.Queries.Connection.Models;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Altinn.Authorization.ABAC.Xacml;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.Api.Contracts.AccessManagement.Enums;
using Altinn.Authorization.ProblemDetails;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Altinn.AccessMgmt.Core.Services;

/// <inheritdoc />
public partial class ConnectionService(
    AppDbContext dbContext,
    ConnectionQuery connectionQuery,
    IAuditAccessor auditAccessor,
    IOptions<CoreAppsettings> appsettings,
    IAltinn2RightsClient altinn2Client,
    IAMPartyService partyService,
    IContextRetrievalService contextRetrievalService,
    IAccessListsAuthorizationClient accessListsAuthorizationClient,
    IPolicyRetrievalPoint policyRetrievalPoint,
    IRoleService roleService,
    ITranslationService translationService,
    ISingleRightsService singleRightsService) : IConnectionService
{
    public async Task<Result<IEnumerable<ConnectionDto>>> Get(Guid party, Guid? fromId, Guid? toId, bool includeClientDelegations = true, bool includeAgentConnections = true, Action<ConnectionOptions> configureConnections = null, CancellationToken cancellationToken = default)
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
                IncludeDelegation = includeClientDelegations,
                IncludePackages = false,
                IncludeResources = false,
                EnrichPackageResources = false,
                ExcludeDeleted = false,
                ExcludeRoleIds = includeAgentConnections ? null : [RoleConstants.Agent.Id],
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

    /// <inheritdoc/>
    public async Task<Result<AssignmentDto>> AddRightholder(Guid fromId, Guid toId, Action<ConnectionOptions> configureConnections = null, CancellationToken cancellationToken = default)
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
        await RightholderAddedNotification.Upsert(
            dbContext,
            from.Id,
            to.Id,
            appsettings?.Value?.Notifications?.RightholderAddedNotifyInSeconds ?? RightholderAddedNotification.DefaultNotifyInSeconds,
            cancellationToken
        );

        await dbContext.SaveChangesAsync(cancellationToken);

        if (from.PartyId.HasValue && to.PartyId.HasValue)
        {
            await altinn2Client.ClearReporteeRights(from.PartyId.Value, to.PartyId.Value, to.UserId.HasValue ? to.UserId.Value : 0, cancellationToken: cancellationToken);
        }

        return DtoMapper.Convert(assignment);
    }

    /// <inheritdoc />
    public async Task<ValidationProblemInstance> CheckAssignmentForConnectedRefernces(Guid assignmentId, CancellationToken cancellationToken = default)
    {
        // WARNING! The list of connected entities to check for active connections must be in sync with the connections that are actually deleted in the cascade delete in the database,
        // otherwise we might end up deleting data that was not evaluted for cascading delete. Idealy this would have been only one method but given the problem returned is difrent from
        // that used in AssignmentService we need to duplicate the checks here, as it is already published and would be a breaking change.
        // Changes here must also be done in AssignmentService.CheckCascadingAssignmentRevoke(Guid assignmentId, CancellationToken cancellationToken).
        var assignedPackages = await dbContext.AssignmentPackages
            .AsNoTracking()
            .Where(p => p.AssignmentId == assignmentId)
            .ToListAsync(cancellationToken);

        var resources = await dbContext.AssignmentResources
            .AsNoTracking()
            .Where(p => p.AssignmentId == assignmentId)
            .ToListAsync(cancellationToken);

        var instances = await dbContext.AssignmentInstances
            .AsNoTracking()
            .Where(p => p.AssignmentId == assignmentId)
            .ToListAsync(cancellationToken);

        var delegationsFrom = await dbContext.Delegations
            .AsNoTracking()
            .Where(p => p.FromId == assignmentId)
            .ToListAsync(cancellationToken);

        var delegationsTo = await dbContext.Delegations
            .AsNoTracking()
            .Where(p => p.ToId == assignmentId)
            .ToListAsync(cancellationToken);

        var problem = ValidationComposer.Validate(
            AssignmentPackageValidation.HasAssignedPackages(assignedPackages),
            AssignmentResourceValidation.HasAssignedResources(resources),
            AssignmentInstanceValidation.HasAssignedInstances(instances),
            DelegationValidation.HasDelegationsAssigned(delegationsFrom),
            DelegationValidation.HasDelegationsAssigned(delegationsTo)
        );

        return problem;
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
            problem = await CheckAssignmentForConnectedRefernces(existingAssignment.Id, cancellationToken);

            if (problem is { })
            {
                return problem;
            }
        }

        dbContext.Remove(existingAssignment);
        await RightholderRemovedNotification.Upsert(
            dbContext,
            fromId,
            toId,
            appsettings?.Value?.Notifications?.RightholderRemovedNotifyInSeconds ?? RightholderRemovedNotification.DefaultNotifyInSeconds,
            cancellationToken
        );

        await dbContext.SaveChangesAsync(cancellationToken);

        if (from.PartyId.HasValue && to.PartyId.HasValue)
        {
            await altinn2Client.ClearReporteeRights(from.PartyId.Value, to.PartyId.Value, to.UserId.HasValue ? to.UserId.Value : 0, cancellationToken: cancellationToken);
        }

        return null;
    }

    #region Resources

    public async Task<Result<IEnumerable<ResourcePermissionDto>>> GetResources(Guid party, Guid? fromId, Guid? toId, Guid? resourceId, Action<ConnectionOptions> configureConnections = null, CancellationToken cancellationToken = default)
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

        var direction = party == fromId
            ? ConnectionQueryDirection.ToOthers
            : ConnectionQueryDirection.FromOthers;

        var resources = await connectionQuery.GetConnectionsAsync(
            new ConnectionQueryFilter()
            {
                RoleIds = [RoleConstants.Rightholder.Id],
                ResourceIds = resourceId.HasValue ? [resourceId.Value] : null,
                FromIds = from != null ? [from.Id] : null,
                ToIds = to != null ? [to.Id] : null,
                IncludeResources = true,
                IncludeKeyRole = true,
                IncludeMainUnitConnections = true,
                IncludeSubConnections = true,
                IncludePackages = false,
                EnrichPackageResources = false,
                IncludeDelegation = false,
            },
            direction,
            true,
            cancellationToken
        );

        return DtoMapper.ConvertResources(resources);
    }

    public async Task<Result<IEnumerable<InstancePermissionDto>>> GetResourceInstances(Guid party, Guid? fromId, Guid? toId, Guid? resourceId, string instanceId, Action<ConnectionOptions> configureConnections = null, CancellationToken cancellationToken = default)
    {
        var options = new ConnectionOptions(configureConnections);
        var (from, to) = await GetFromAndToEntities(fromId, toId, cancellationToken);
        var problem = ValidateReadOpInput(fromId, from, toId, to, options);
        if (problem is { })
        {
            if (problem.ErrorCode == Problems.PartyNotFound.ErrorCode)
            {
                return new List<InstancePermissionDto>();
            }

            return problem;
        }

        var direction = party == fromId
            ? ConnectionQueryDirection.ToOthers
            : ConnectionQueryDirection.FromOthers;

        var instances = await connectionQuery.GetConnectionsAsync(
            new ConnectionQueryFilter()
            {
                RoleIds = [RoleConstants.Rightholder.Id],
                ResourceIds = resourceId.HasValue ? [resourceId.Value] : null,
                InstanceIds = !string.IsNullOrEmpty(instanceId) ? [instanceId] : null,
                FromIds = from != null ? [from.Id] : null,
                ToIds = to != null ? [to.Id] : null,
                IncludeInstances = true,
                IncludeResources = false,
                IncludeKeyRole = true,
                IncludeMainUnitConnections = false,
                IncludeSubConnections = false,
                IncludePackages = false,
                EnrichPackageResources = false,
                IncludeDelegation = false,
            },
            direction,
            true,
            cancellationToken
        );

        return await MapConnectionsToInstancePermissions(instances, cancellationToken);
    }

    private async Task<List<InstancePermissionDto>> MapConnectionsToInstancePermissions(IEnumerable<ConnectionQueryExtendedRecord> connections, CancellationToken cancellationToken)
    {
        var result = new List<InstancePermissionDto>();

        // Group connections by resource and instance
        var grouped = connections
            .Where(c => c.Instances != null && c.Instances.Any())
            .SelectMany(c => c.Instances, (connection, instance) => new { Connection = connection, Instance = instance })
            .GroupBy(x => new { x.Instance.ResourceId, x.Instance.InstanceId });

        foreach (var group in grouped)
        {
            var firstInstance = group.First().Instance;

            var dto = new InstancePermissionDto
            {
                Resource = new ResourceDto { Id = firstInstance.ResourceId, RefId = firstInstance.ResourceRefId, Name = firstInstance.ResourceName },
                Instance = new InstanceDto
                {
                    RefId = firstInstance.InstanceId,
                    Type = null // TODO: InstanceType support to be added later
                },
                Permissions = group.Select(x => DtoMapper.ConvertToPermission(x.Connection)).ToList()
            };

            result.Add(dto);
        }

        return result;
    }

    public async Task<Result<bool>> UpdateResource(Entity from, Entity to, Resource resourceObj, IEnumerable<string> rightKeys, Entity by, Action<ConnectionOptions> configureConnection = null, CancellationToken cancellationToken = default)
    {
        var canDelegate = await ResourceDelegationCheck(by.Id, from.Id, resourceObj?.RefId, configureConnection, cancellationToken: cancellationToken);
        if (canDelegate.IsProblem)
        {
            return canDelegate.Problem;
        }

        var keys = rightKeys.ToList();
        foreach (var ruleKey in keys)
        {
            if (!canDelegate.Value.Rights.Any(a => a.Right.Key == ruleKey && a.Result))
            {
                return Problems.NotAuthorizedForDelegationRequest;
            }
        }

        List<Rule> result = await singleRightsService.TryWriteDelegationPolicyRules(from, to, resourceObj, keys, by, ignoreExistingPolicy: true, cancellationToken: cancellationToken);

        if (!result.All(r => r.CreatedSuccessfully))
        {
            return Problems.DelegationPolicyRuleWriteFailed;
        }

        return true;
    }

    public async Task<ValidationProblemInstance> RemoveResource(Guid fromId, Guid toId, string resource, Action<ConnectionOptions> configureConnection = null, CancellationToken cancellationToken = default)
    {
        var resourceObj = await dbContext.Resources.AsNoTracking().FirstOrDefaultAsync(t => t.RefId == resource, cancellationToken);
        if (resourceObj == null)
        {
            return ValidationComposer.Validate(
                ResourceValidation.ResourceExists(resourceObj, resource)
            );
        }

        return await RemoveResource(fromId, toId, resourceObj.Id, configureConnection, cancellationToken);
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

        var newVersion = await singleRightsService.ClearPolicyRules(existingAssignmentResources.PolicyPath, existingAssignmentResources.PolicyVersion, cancellationToken);
        existingAssignmentResources.PolicyVersion = newVersion;
        await AccessRemovedNotification.Upsert(
            dbContext,
            fromId,
            toId,
            resourceId,
            null,
            appsettings?.Value?.Notifications?.AccessRemovedNotifyInSeconds ?? AccessRemovedNotification.DefaultNotifyInSeconds,
            cancellationToken
        );

        dbContext.Remove(existingAssignmentResources);
        await dbContext.SaveChangesAsync(cancellationToken);

        return null;
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
            IncludeResources = false,
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
        await AccessRemovedNotification.Upsert(
            dbContext,
            fromId,
            toId,
            null,
            packageId,
            appsettings?.Value?.Notifications?.AccessRemovedNotifyInSeconds ?? AccessRemovedNotification.DefaultNotifyInSeconds,
            cancellationToken
        );

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

        var connection = await Get(fromId, fromId, toId, configureConnections: configureConnection, cancellationToken: cancellationToken);
        if (!connection.IsSuccess || !connection.Value.Any())
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
            PackageValidation.PackageIsAssignableTo(check.Value.Select(p => p.Package.Urn), to.Type, queryParamName),
            PackageValidation.PackageIsAssignableFrom(check.Value.Select(p => p.Package.Urn), from.Type, queryParamName)
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
        await AccessAddedNotification.Upsert(
            dbContext,
            fromId,
            toId,
            null,
            packageId,
            appsettings?.Value?.Notifications?.AccessAddedNotifyInSeconds ?? AccessAddedNotification.DefaultNotifyInSeconds,
            cancellationToken
        );

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

        var result = assignablePackages.GroupBy(p => p.Package.Id).Select(group =>
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

        // If packageIds were provided, ensure that all requested packages are represented in the result, otherwise return problem details.
        if (packageIds != null)
        {
            var missingPackageIds = packageIds.Except(result.Select(r => r.Package.Id)).ToList();
            if (missingPackageIds.Any())
            {
                return Problems.DelegationCheckFailureInvalidPackage;
            }
        }

        return result;
    }

    public async Task<Result<IEnumerable<AccessPackageDto.AccessPackageDtoCheck>>> CheckPackageForResource(Guid party, Guid authenticatedUserUuid, IEnumerable<Guid> packageIds = null, Action<ConnectionOptions> configureConnection = null, CancellationToken cancellationToken = default)
    {
        var assignablePackages = await dbContext.GetAssignableAccessPackages(
            party,
            authenticatedUserUuid,
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
            IncludeResources = false,
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
    public async Task<Result<ResourceCheckDto>> ResourceDelegationCheck(Guid authenticatedUserUuid, Guid party, string resource, Action<ConnectionOptions> configureConnection = null, string languageCode = "nb", bool ignoreDelegableFlag = false, bool allowMaskinportenSchema = false, CancellationToken cancellationToken = default)
    {
        // Get fromParty
        MinimalParty fromParty = await partyService.GetByUid(party, cancellationToken);

        ResourceDto resourceDto;
        XacmlPolicy policy;
        bool isMaskinPortenSchemaResource = false;

        try
        {
            // Fetch resource
            resourceDto = await FetchResource(resource, cancellationToken);

            // Fetch policy for the resource
            policy = await GetPolicy(resource, cancellationToken);
        }
        catch (ValidationException)
        {
            return Problems.InvalidResource;
        }

        if (resourceDto.Type.Name.Equals("MaskinportenSchema", StringComparison.InvariantCultureIgnoreCase))
        {
            isMaskinPortenSchemaResource = true;
        }

        // When allowMaskinportenSchema is true, we don't want to deny delegation for MaskinportenSchema resources
        // isMaskinPortenSchema is used later to add denial reasons, so we only set it when we want to deny
        bool isMaskinPortenSchema = isMaskinPortenSchemaResource && !allowMaskinportenSchema;

        // Fetch Resourcemetadata
        ServiceResource resourceMetadata = await contextRetrievalService.GetResource(resource, cancellationToken);

        if (resourceMetadata is null)
        {
            return Problems.InvalidResource;
        }

        List<RightDto> rightKeys = await contextRetrievalService.GetResourcePolicyV2(resource, languageCode, cancellationToken);

        if (rightKeys is null)
        {
            return Problems.MissingMetadata;
        }

        ResourceAccessListMode accessListMode = resourceMetadata.AccessListMode;
        bool isResourceDelegable = ignoreDelegableFlag || resourceMetadata.Delegable || (allowMaskinportenSchema && isMaskinPortenSchemaResource);

        // Decompose policy into resource/tasks
        List<Models.Right> rights = DelegationCheckHelper.DecomposePolicy(policy, resource);

        // Fetch packages
        var packages = await CheckPackageForResource(party, authenticatedUserUuid, null, ConfigureConnections, cancellationToken);

        bool isMainAdminForFrom = await IsMainAdmin(party, authenticatedUserUuid, cancellationToken);

        var roles = await RoleDelegationCheck(party, authenticatedUserUuid, isMainAdminForFrom, cancellationToken);

        // Fetch resource rights
        var resources = await GetResourceRights(party, authenticatedUserUuid, resourceDto.Id, null, cancellationToken);

        ProcessTheAccessToTheRightKeys(rights, packages.Value, roles.Value, resources);

        // Map to result
        IEnumerable<RightCheckDto> checkRights = await MapFromInternalToExternalRights(rights, resource, accessListMode, fromParty, rightKeys, isResourceDelegable, isMaskinPortenSchema, cancellationToken);

        // build reult with reason based on roles, packages, resource rights and users delegable
        ResourceCheckDto resourceCheckDto = new ResourceCheckDto
        {
            Resource = resourceDto,
            Rights = checkRights
        };

        return resourceCheckDto;
    }

    /// <inheritdoc/>
    public async Task<Result<InstanceCheckDto>> InstanceDelegationCheck(Guid authenticatedUserUuid, Guid party, string resource, string instanceId, Action<ConnectionOptions> configureConnection = null, string languageCode = "nb", CancellationToken cancellationToken = default)
    {
        // Get fromParty
        MinimalParty fromParty = await partyService.GetByUid(party, cancellationToken);

        ResourceDto resourceDto;
        XacmlPolicy policy;
        bool isMaskinPortenSchema = false;

        try
        {
            resourceDto = await FetchResource(resource, cancellationToken);
            policy = await GetPolicy(resource, cancellationToken);
        }
        catch (ValidationException)
        {
            return Problems.InvalidResource;
        }

        if (resourceDto.Type.Name.Equals("MaskinportenSchema", StringComparison.InvariantCultureIgnoreCase))
        {
            isMaskinPortenSchema = true;
        }

        ServiceResource resourceMetadata = await contextRetrievalService.GetResource(resource, cancellationToken);
        if (resourceMetadata is null)
        {
            return Problems.InvalidResource;
        }

        List<RightDto> rightKeys = await contextRetrievalService.GetResourcePolicyV2(resource, languageCode, cancellationToken);
        if (rightKeys is null)
        {
            return Problems.MissingMetadata;
        }

        ResourceAccessListMode accessListMode = resourceMetadata.AccessListMode;
        bool isResourceDelegable = resourceMetadata.Delegable;

        List<Models.Right> rights = DelegationCheckHelper.DecomposePolicy(policy, resource);

        var packages = await CheckPackageForResource(party, authenticatedUserUuid, null, configureConnection, cancellationToken);

        bool isMainAdminForFrom = await IsMainAdmin(party, authenticatedUserUuid, cancellationToken);

        var roles = await RoleDelegationCheck(party, authenticatedUserUuid, isMainAdminForFrom, cancellationToken);
        var resources = await GetResourceRights(party, authenticatedUserUuid, resourceDto.Id, null, cancellationToken);
        var instances = await GetInstanceRights(party, authenticatedUserUuid, resourceDto.Id, instanceId, RoleConstants.Rightholder, cancellationToken);

        ProcessTheAccessToTheRightKeys(rights, packages.Value, roles.Value, resources, instances);

        // Map to result
        IEnumerable<RightCheckDto> checkRights = await MapFromInternalToExternalRights(rights, resource, accessListMode, fromParty, rightKeys, isResourceDelegable, isMaskinPortenSchema, cancellationToken);

        // build result with resource, instance and rights
        InstanceCheckDto instanceCheckDto = new InstanceCheckDto
        {
            Resource = resourceDto,
            Instance = new InstanceDto
            {
                RefId = instanceId,
                Type = null
            },
            Rights = checkRights
        };

        return instanceCheckDto;
    }

    private async Task<bool> IsMainAdmin(Guid fromId, Guid authenticatedUserUuid, CancellationToken cancellationToken)
    {
        var connectionsToFromParty = await connectionQuery.GetConnectionsAsync(
            new ConnectionQueryFilter()
            {
                FromIds = [fromId],
                ToIds = [authenticatedUserUuid],
                IncludeKeyRole = true,
                IncludeMainUnitConnections = true,
                IncludeSubConnections = true,
                IncludePackages = true,
                IncludeDelegation = false,
                EnrichEntities = false,
            },
            ConnectionQueryDirection.FromOthers,
            true,
            cancellationToken
        );
        return connectionsToFromParty.Any(c => c.RoleId == RoleConstants.MainAdministratorA2.Id || c.Packages.Any(p => p.Id == PackageConstants.MainAdministrator.Id));
    }

    private async Task<RightCheckDto> MapFromInternalToExternalRight(Models.Right right, string resource, ResourceAccessListMode accessListMode, MinimalParty fromParty, List<RightDto> rightKeys, bool isResourceDelegable, bool isMaskinPortenSchema, CancellationToken cancellationToken)
    {
        RightDto rightKey = rightKeys.FirstOrDefault(r => r.Key == right.Key);
        if (rightKey is null)
        {
            rightKey = new RightDto
            {
                Key = right.Key
            };
        }

        if (DelegationCheckHelper.IsAccessListModeEnabledAndApplicable(accessListMode, fromParty.PartyType))
        {
            if (rightKey.Action is not null)
            {
                AccessListAuthorizationRequest accessListAuthorizationRequest = new AccessListAuthorizationRequest
                {
                    Subject = PartyUrn.PartyUuid.Create(fromParty.PartyUuid),
                    Resource = ResourceIdUrn.ResourceId.Create(ResourceIdentifier.CreateUnchecked(resource)),
                    Action = ActionUrn.ActionId.Create(ActionIdentifier.Parse(rightKey.Action.Urn()))
                };

                AccessListAuthorizationResponse accessListAuthorizationResponse = await accessListsAuthorizationClient.AuthorizePartyForAccessList(accessListAuthorizationRequest, cancellationToken);
                AccessListAuthorizationResult accessListAuthorizationResult = accessListAuthorizationResponse.Result;
                if (accessListAuthorizationResult != AccessListAuthorizationResult.Authorized)
                {
                    right.AccessListDenied = true;
                }
            }
            else
            {
                right.AccessListDenied = true;
            }
        }

        RightCheckDto currentAction = new RightCheckDto
        {
            Right = rightKey,
            Result = false
        };

        List<RightCheckDto.Permision> permisions = [];
        if (right.PackageAllowAccess.Count == 0 && right.RoleAllowAccess.Count == 0 && right.ResourceAllowAccess.Count == 0)
        {
            currentAction.Result = false;

            permisions.AddRange(right.PackageDenyAccess);
            permisions.AddRange(right.RoleDenyAccess);

            permisions.Add(new RightCheckDto.Permision
            {
                Description = "Missing-Resource",
                PermisionKey = DelegationCheckReasonCode.MissingDelegationAccess
            });
        }
        else
        {
            currentAction.Result = true;

            ProcessPackageAllowAccessReasons(right.PackageAllowAccess, permisions);
            ProcessRoleAllowAccessReasons(right.RoleAllowAccess, permisions);
            ProcessResourceAllowAccessReasons(right.ResourceAllowAccess, permisions);
        }

        if (!isResourceDelegable)
        {
            currentAction.Result = false;
            RightCheckDto.Permision permision = new RightCheckDto.Permision
            {
                Description = $"Resource-NotDelegable",
                PermisionKey = DelegationCheckReasonCode.ResourceNotDelegable,
            };
            permisions.Add(permision);
        }

        if (isMaskinPortenSchema)
        {
            currentAction.Result = false;
            RightCheckDto.Permision permision = new RightCheckDto.Permision
            {
                Description = $"Resource-MaskinportenSchema",
                PermisionKey = DelegationCheckReasonCode.ResourceIsMaskinPortenSchema,
            };
            permisions.Add(permision);
        }

        if (right.AccessListDenied == true)
        {
            currentAction.Result = false;
            RightCheckDto.Permision permision = new RightCheckDto.Permision
            {
                Description = "Resource-AccessList-Enabled-NotListed",
                PermisionKey = DelegationCheckReasonCode.AccessListValidationFail,
            };
            permisions.Add(permision);
        }

        HashSet<DelegationCheckReasonCode> reasonKeys = [];

        foreach (var permision in permisions)
        {
            reasonKeys.Add(permision.PermisionKey);
        }

        currentAction.ReasonCodes = reasonKeys;

        return currentAction;
    }

    private void ProcessResourceAllowAccessReasons(List<RightPermission> resourceRightsAllowAccess, List<RightCheckDto.Permision> permisions)
    {
        if (resourceRightsAllowAccess.Count > 0)
        {
            foreach (var resourceRightAllowAccess in resourceRightsAllowAccess)
            {
                foreach (var resourceRightPermission in resourceRightAllowAccess.Permissions)
                {
                    RightCheckDto.Permision permision = new RightCheckDto.Permision
                    {
                        Description = "Access-Resource",
                        PermisionKey = DelegationCheckReasonCode.DelegationAccess,
                        FromName = resourceRightPermission.From?.Name,
                        FromId = resourceRightPermission.From?.Id,
                        ToName = resourceRightPermission.To?.Name,
                        ToId = resourceRightPermission.To?.Id,
                        RoleId = resourceRightPermission.Role?.Id,
                        RoleUrn = resourceRightPermission.Role?.Urn,

                        ViaId = resourceRightPermission.Via?.Id,
                        ViaName = resourceRightPermission.Via?.Name,
                        ViaRoleId = resourceRightPermission.ViaRole?.Id,
                        ViaRoleUrn = resourceRightPermission.ViaRole?.Urn
                    };

                    permisions.Add(permision);
                }
            }
        }
    }

    /// <inheritdoc />
    public async Task<Result<bool>> AddResource(Entity from, Entity to, Resource resourceObj, RightKeyListDto rightKeys, Entity by, Action<ConnectionOptions> configureConnection = null, CancellationToken cancellationToken = default)
    {
        if (rightKeys?.DirectRightKeys is null || !rightKeys.DirectRightKeys.Any())
        {
            return Problems.MissingRightKey;
        }

        var canDelegate = await ResourceDelegationCheck(by.Id, from.Id, resourceObj?.RefId, configureConnection, cancellationToken: cancellationToken);
        if (canDelegate.IsProblem)
        {
            return canDelegate.Problem;
        }

        var keys = rightKeys.DirectRightKeys.ToList();
        foreach (var rightKey in keys)
        {
            if (!canDelegate.Value.Rights.Any(a => a.Right.Key == rightKey && a.Result))
            {
                return Problems.NotAuthorizedForDelegationRequest;
            }
        }

        var connection = await Get(from.Id, from.Id, to.Id, configureConnections: configureConnection, cancellationToken: cancellationToken);
        if (!connection.IsSuccess || connection.Value.Count() == 0)
        {
            return Problems.MissingConnection;
        }

        List<Rule> result = await singleRightsService.TryWriteDelegationPolicyRules(from, to, resourceObj, keys, by, ignoreExistingPolicy: false, cancellationToken: cancellationToken);

        await AccessAddedNotification.Upsert(
            dbContext,
            from.Id,
            to.Id,
            resourceObj.Id,
            null,
            appsettings?.Value?.Notifications?.AccessAddedNotifyInSeconds ?? AccessAddedNotification.DefaultNotifyInSeconds,
            cancellationToken
        );

        if (!result.All(r => r.CreatedSuccessfully))
        {
            return Problems.DelegationPolicyRuleWriteFailed;
        }

        return true;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> AddInstance(Entity from, Entity to, Resource resourceObj, string instanceId, RightKeyListDto rightKeys, Entity by, Action<ConnectionOptions> configureConnection = null, CancellationToken cancellationToken = default)
    {
        var canDelegate = await InstanceDelegationCheck(by.Id, from.Id, resourceObj?.RefId, instanceId, configureConnection, cancellationToken: cancellationToken);
        if (canDelegate.IsProblem)
        {
            return canDelegate.Problem;
        }

        var keys = rightKeys.DirectRightKeys.ToList();
        foreach (var rightKey in keys)
        {
            if (!canDelegate.Value.Rights.Any(a => a.Right.Key == rightKey && a.Result))
            {
                return Problems.NotAuthorizedForDelegationRequest;
            }
        }

        var connection = await Get(from.Id, from.Id, to.Id, configureConnections: configureConnection, cancellationToken: cancellationToken);
        if (!connection.IsSuccess || !connection.Value.Any())
        {
            return Problems.MissingConnection;
        }

        List<InstanceRule> result = await singleRightsService.TryWriteInstanceDelegationPolicyRules(from, to, resourceObj, instanceId, keys, by, ignoreExistingPolicy: false, cancellationToken: cancellationToken);

        if (!result.All(r => r.CreatedSuccessfully))
        {
            return Problems.DelegationPolicyRuleWriteFailed;
        }

        return true;
    }

    public async Task<Result<bool>> UpdateInstance(Entity from, Entity to, Resource resourceObj, string instanceId, IEnumerable<string> rightKeys, Entity by, Action<ConnectionOptions> configureConnection = null, CancellationToken cancellationToken = default)
    {
        var canDelegate = await InstanceDelegationCheck(by.Id, from.Id, resourceObj?.RefId, instanceId, ConfigureConnections, cancellationToken: cancellationToken);
        if (canDelegate.IsProblem)
        {
            return canDelegate.Problem;
        }

        var keys = rightKeys.ToList();
        foreach (var rightKey in keys)
        {
            if (!canDelegate.Value.Rights.Any(a => a.Right.Key == rightKey && a.Result))
            {
                return Problems.NotAuthorizedForDelegationRequest;
            }
        }

        List<InstanceRule> result = await singleRightsService.TryWriteInstanceDelegationPolicyRules(from, to, resourceObj, instanceId, keys, by, ignoreExistingPolicy: true, cancellationToken: cancellationToken);

        if (!result.All(r => r.CreatedSuccessfully))
        {
            return Problems.DelegationPolicyRuleWriteFailed;
        }

        return true;
    }

    public async Task<ValidationProblemInstance> RemoveInstance(Guid fromId, Guid toId, string resource, string instanceId, Action<ConnectionOptions> configureConnection = null, CancellationToken cancellationToken = default)
    {
        var resourceObj = await dbContext.Resources.AsNoTracking().FirstOrDefaultAsync(t => t.RefId == resource, cancellationToken);
        if (resourceObj == null)
        {
            return ValidationComposer.Validate(
                ResourceValidation.ResourceExists(resourceObj, resource)
            );
        }

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

        var existingAssignmentInstance = await dbContext.AssignmentInstances
            .AsTracking()
            .Where(a => a.AssignmentId == assignment.Id)
            .Where(a => a.ResourceId == resourceObj.Id)
            .Where(a => a.InstanceId == instanceId)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingAssignmentInstance is null)
        {
            return null;
        }

        var newVersion = await singleRightsService.ClearPolicyRules(existingAssignmentInstance.PolicyPath, existingAssignmentInstance.PolicyVersion, cancellationToken);
        existingAssignmentInstance.PolicyVersion = newVersion;

        dbContext.Remove(existingAssignmentInstance);
        await dbContext.SaveChangesAsync(cancellationToken);

        return null;
    }

    private void ProcessRoleAllowAccessReasons(List<RoleDtoCheck> rolesAllowAccess, List<RightCheckDto.Permision> permisions)
    {
        if (rolesAllowAccess.Count > 0)
        {
            foreach (var roleAllowAccess in rolesAllowAccess)
            {
                foreach (var roleReason in roleAllowAccess.Reasons)
                {
                    RightCheckDto.Permision permison = new RightCheckDto.Permision
                    {
                        Description = roleReason.Description,
                        PermisionKey = DelegationCheckReasonCode.RoleAccess,
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

                    permisions.Add(permison);
                }
            }
        }
    }

    private void ProcessPackageAllowAccessReasons(List<AccessPackageDto.AccessPackageDtoCheck> packagesAllowAccess, List<RightCheckDto.Permision> reasons)
    {
        if (packagesAllowAccess.Count > 0)
        {
            foreach (var packageAllowAccess in packagesAllowAccess)
            {
                foreach (var packageReason in packageAllowAccess.Reasons)
                {
                    RightCheckDto.Permision permision = new RightCheckDto.Permision
                    {
                        Description = packageReason.Description,
                        PermisionKey = DelegationCheckReasonCode.PackageAccess,
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

                    reasons.Add(permision);
                }
            }
        }
    }

    private async Task<IEnumerable<RightCheckDto>> MapFromInternalToExternalRights(List<Models.Right> rights, string resource, ResourceAccessListMode accessListMode, MinimalParty fromParty, List<RightDto> rightKeys, bool isResourceDelegable, bool isMaskinportenSchema, CancellationToken cancellationToken = default)
    {
        List<RightCheckDto> result = [];

        foreach (var right in rights)
        {
            result.Add(await MapFromInternalToExternalRight(right, resource, accessListMode, fromParty, rightKeys, isResourceDelegable, isMaskinportenSchema, cancellationToken));
        }

        return result;
    }

    private void ProcessTheAccessToTheRightKeys(List<Models.Right> rights, IEnumerable<AccessPackageDto.AccessPackageDtoCheck> packages, IEnumerable<RoleDtoCheck> roles, List<ResourceRightDto> resources, List<InstanceRightDto> instances = null)
    {
        foreach (var right in rights)
        {
            foreach (var accessorUrn in right.AccessorUrns)
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
                        right.PackageAllowAccess.Add(package);
                    }
                    else
                    {
                        RightCheckDto.Permision permision = new RightCheckDto.Permision
                        {
                            Description = $"Missing-Package",
                            PermisionKey = DelegationCheckReasonCode.MissingPackageAccess,
                            PackageId = package.Package.Id,
                            PackageUrn = package.Package.Urn
                        };

                        right.PackageDenyAccess.Add(permision);
                    }
                }

                if (role != null)
                {
                    if (role.Result)
                    {
                        right.RoleAllowAccess.Add(role);
                    }
                    else
                    {
                        RightCheckDto.Permision permision = new RightCheckDto.Permision
                        {
                            Description = $"Missing-Role",
                            PermisionKey = DelegationCheckReasonCode.MissingRoleAccess,
                            RoleId = role.Role.Id,
                            RoleUrn = role.Role.Urn,
                        };

                        right.RoleDenyAccess.Add(permision);
                    }
                }
            }

            foreach (var resource in resources)
            {
                if (resource.Rights.Any(r => r.Right.Key == right.Key))
                {
                    right.ResourceAllowAccess.Add(resource.Rights.First(r => r.Right.Key == right.Key));
                }
            }

            // Process instance-specific rights if provided
            if (instances != null)
            {
                foreach (var instance in instances)
                {
                    if (instance.Rights.Any(r => r.Right.Key == right.Key))
                    {
                        right.ResourceAllowAccess.Add(instance.Rights.First(r => r.Right.Key == right.Key));
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

    private async Task<ResourceDto> FetchResource(string resource, CancellationToken cancellationToken)
    {
        Resource resourceObj = await dbContext.Resources.AsNoTracking().SingleOrDefaultAsync(r => r.RefId == resource, cancellationToken);

        if (resourceObj is null)
        {
            throw new ValidationException($"Resource with id '{resource}' not found");
        }

        Provider provider = await dbContext.Providers.AsNoTracking().SingleOrDefaultAsync(p => p.Id == resourceObj.ProviderId, cancellationToken);
        ProviderTypeConstants.TryGetById(provider.TypeId, out var providerType);
        PersistenceEF.Models.ResourceType resourceType = await dbContext.ResourceTypes.SingleOrDefaultAsync(rt => rt.Id == resourceObj.TypeId, cancellationToken);

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
            Id = resourceObj.Id,
            Name = resourceObj.Name,
            Description = resourceObj.Description,
            Provider = providerDto,
            ProviderId = provider.Id,
            RefId = resourceObj.RefId,
            TypeId = resourceObj.TypeId,
            Type = new ResourceTypeDto { Id = resourceType.Id, Name = resourceType.Name }
        };

        return resourceDto;
    }

    private async Task<XacmlPolicy> GetPolicy(string resource, CancellationToken cancellationToken = default)
    {
        XacmlPolicy policy = null;

        if (string.IsNullOrEmpty(resource))
        {
            throw new ValidationException($"Resource cannot be null or empty");
        }

        bool isApp = DelegationCheckHelper.IsAppResource(resource, out string org, out string app);

        if (isApp)
        {
            policy = await policyRetrievalPoint.GetPolicyAsync(org, app, cancellationToken);
        }
        else
        {
            policy = await policyRetrievalPoint.GetPolicyAsync(resource, cancellationToken);
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
    public async Task<IEnumerable<ResourcePermissionDto>> GetResourcePermissionsFromOthers(
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
            return packages.DistinctBy(t => t.Resource.Id).Select(permission => new ResourcePermissionDto()
            {
                Resource = DtoMapper.Convert(permission.Resource),
                Permissions = packages.Where(t => t.Resource.Id == permission.Resource.Id).Select(DtoMapper.ConvertToPermission)
            });
        }

        return [];
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ResourcePermissionDto>> GetResourcePermissionsToOthers(
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
            return packages.DistinctBy(t => t.Resource.Id).Select(permission => new ResourcePermissionDto()
            {
                Resource = DtoMapper.Convert(permission.Resource),
                Permissions = packages.Where(t => t.Resource.Id == permission.Resource.Id).Select(DtoMapper.ConvertToPermission)
            });
        }

        return [];
    }

    /// <inheritdoc />
    public async Task<ResourceRightDto> GetResourceRightsToOthers(Guid partyId, Guid toId, Guid resourceId, Action<ConnectionOptions> configureConnection = null, CancellationToken cancellationToken = default)
    {
        var result = await GetResourceRights(
           fromId: partyId,
           toId: toId,
           resourceId: resourceId,
           roleId: RoleConstants.Rightholder,
           cancellationToken: cancellationToken
           );

        return result.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<ResourceRightDto> GetResourceRightsFromOthers(Guid partyId, Guid fromId, Guid resourceId, Action<ConnectionOptions> configureConnection = null, CancellationToken cancellationToken = default)
    {
        var result = await GetResourceRights(
            fromId: fromId,
            toId: partyId,
            resourceId: resourceId,
            roleId: RoleConstants.Rightholder,
            cancellationToken: cancellationToken
            );

        return result.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<InstanceRightDto> GetInstanceRightsToOthers(Guid partyId, Guid toId, Guid resourceId, string instanceId, Action<ConnectionOptions> configureConnection = null, CancellationToken cancellationToken = default)
    {
        var result = await GetInstanceRights(
           fromId: partyId,
           toId: toId,
           resourceId: resourceId,
           instanceId: instanceId,
           roleId: RoleConstants.Rightholder,
           cancellationToken: cancellationToken
           );

        return result.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<InstanceRightDto> GetInstanceRightsFromOthers(Guid partyId, Guid fromId, Guid resourceId, string instanceId, Action<ConnectionOptions> configureConnection = null, CancellationToken cancellationToken = default)
    {
        var result = await GetInstanceRights(
            fromId: fromId,
            toId: partyId,
            resourceId: resourceId,
            instanceId: instanceId,
            roleId: RoleConstants.Rightholder,
            cancellationToken: cancellationToken
            );

        return result.FirstOrDefault();
    }

    private async Task<List<ResourceRightDto>> GetResourceRights(Guid? fromId, Guid? toId, Guid? resourceId, Guid? roleId, CancellationToken cancellationToken = default)
    {
        if (!fromId.HasValue && !toId.HasValue)
        {
            throw new ArgumentException("You need to specify from or to");
        }

        #region Data

        var baseQuery = dbContext.AssignmentResources.AsNoTracking()
            .WhereIf(fromId.HasValue, t => t.Assignment.FromId == fromId.Value)
            .WhereIf(toId.HasValue, t => t.Assignment.ToId == toId.Value)
            .WhereIf(roleId.HasValue, t => t.Assignment.RoleId == roleId.Value)
            .WhereIf(resourceId.HasValue, t => t.ResourceId == resourceId.Value);

        // Direct
        var direct = dbContext.AssignmentResources.AsNoTracking()
            .WhereIf(fromId.HasValue, t => t.Assignment.FromId == fromId.Value)
            .WhereIf(toId.HasValue, t => t.Assignment.ToId == toId.Value)
            .WhereIf(roleId.HasValue, t => t.Assignment.RoleId == roleId.Value)
            .WhereIf(resourceId.HasValue, t => t.ResourceId == resourceId.Value)
            .Select(t => new AssignmentResourceQueryResult()
            {
                Resource = t.Resource,
                From = t.Assignment.From,
                To = t.Assignment.To,
                Role = t.Assignment.Role,
                PolicyPath = t.PolicyPath,
                PolicyVersion = t.PolicyVersion,
                Reason = AccessReasonFlag.Direct
            });

        // Hierarchy (Parent/Child)
        var childResult = dbContext.AssignmentResources.AsNoTracking()
            .WhereIf(roleId.HasValue, t => t.Assignment.RoleId == roleId.Value)
            .WhereIf(resourceId.HasValue, t => t.ResourceId == resourceId.Value)
            .Join(
                dbContext.Entities.AsNoTracking(),
                ar => ar.Assignment.FromId,
                c => c.ParentId,
                (ar, c) => new AssignmentResourceQueryResult
                {
                    Resource = ar.Resource,
                    From = c,
                    To = ar.Assignment.To,
                    Role = ar.Assignment.Role,
                    Via = null, // c.Parent
                    ViaRole = null,
                    PolicyPath = ar.PolicyPath,
                    PolicyVersion = ar.PolicyVersion
                })
            .WhereIf(fromId.HasValue, t => t.From.Id == fromId.Value)
            .WhereIf(toId.HasValue, t => t.To.Id == toId.Value)
            .Select(t => new AssignmentResourceQueryResult()
            {
                Resource = t.Resource,
                From = t.From,
                To = t.To,
                Role = t.Role,
                PolicyPath = t.PolicyPath,
                PolicyVersion = t.PolicyVersion,
                Reason = AccessReasonFlag.Parent
            });

        // KeyRole
        var keyRoleResult = dbContext.AssignmentResources.AsNoTracking()
           .WhereIf(roleId.HasValue, t => t.Assignment.RoleId == roleId.Value)
           .WhereIf(resourceId.HasValue, t => t.ResourceId == resourceId.Value)
           .Join(
               dbContext.Assignments.AsNoTracking().Where(t => t.Role.IsKeyRole),
               ar => ar.Assignment.ToId,
               c => c.FromId,
               (ar, c) => new AssignmentResourceQueryResult
               {
                   Resource = ar.Resource,
                   From = ar.Assignment.From,
                   To = c.To,
                   Role = ar.Assignment.Role,
                   Via = c.From,
                   ViaRole = c.Role,
                   PolicyPath = ar.PolicyPath,
                   PolicyVersion = ar.PolicyVersion
               })
           .WhereIf(fromId.HasValue, t => t.From.Id == fromId.Value)
           .WhereIf(toId.HasValue, t => t.To.Id == toId.Value)
           .Select(t => new AssignmentResourceQueryResult()
           {
               Resource = t.Resource,
               From = t.From,
               To = t.To,
               Role = t.Role,
               PolicyPath = t.PolicyPath,
               PolicyVersion = t.PolicyVersion,
               Reason = AccessReasonFlag.KeyRole
           });

        // KeyRole + Heirarchy
        var keyRoleSubUnit = dbContext.AssignmentResources.AsNoTracking()
            .WhereIf(roleId.HasValue, t => t.Assignment.RoleId == roleId.Value)
            .WhereIf(resourceId.HasValue, t => t.ResourceId == resourceId.Value)
            .Join(
                dbContext.Entities.AsNoTracking(),
                ar => ar.Assignment.FromId,
                e => e.ParentId,
                (ar, fromChild) => new { ar, fromChild }
            )
            .Join(
                dbContext.Assignments.AsNoTracking().Where(a => a.Role.IsKeyRole),
                x => x.ar.Assignment.ToId,
                kr => kr.FromId,
                (x, kr) => new AssignmentResourceQueryResult
                {
                    Resource = x.ar.Resource,
                    From = x.fromChild,
                    To = kr.To,
                    Role = x.ar.Assignment.Role,
                    Via = kr.From,
                    ViaRole = kr.Role,
                    PolicyPath = x.ar.PolicyPath,
                    PolicyVersion = x.ar.PolicyVersion
                }
            )
            .WhereIf(fromId.HasValue, t => t.From.Id == fromId.Value)
            .WhereIf(toId.HasValue, t => t.To.Id == toId.Value)
            .Select(t => new AssignmentResourceQueryResult()
            {
                Resource = t.Resource,
                From = t.From,
                To = t.To,
                Role = t.Role,
                PolicyPath = t.PolicyPath,
                PolicyVersion = t.PolicyVersion,
                Reason = AccessReasonFlag.Parent | AccessReasonFlag.KeyRole
            });

        var query = direct
            .Union(childResult)
            .Union(keyRoleResult)
            .Union(keyRoleSubUnit);

        var res = await query.ToListAsync();

        #endregion

        var result = new List<ResourceRightDto>();

        foreach (var resource in res.Select(t => t.Resource).DistinctBy(t => t.Id))
        {
            var internalResource = res.First().Resource;
            var rightKeys = await contextRetrievalService.GetResourcePolicyV2(internalResource.RefId, cancellationToken: cancellationToken);

            var resourceRight = new ResourceRightDto()
            {
                Resource = DtoMapper.Convert(internalResource),
                Rights = new List<RightPermission>()
            };

            bool isApp = DelegationCheckHelper.IsAppResource(resource.RefId, out string org, out string app);
            var resourcePolicy = isApp ?
                await policyRetrievalPoint.GetPolicyAsync(org, app, cancellationToken) :
                await policyRetrievalPoint.GetPolicyAsync(resource.RefId, cancellationToken);

            var policyRights = resourcePolicy.Rules.SelectMany(t => DelegationCheckHelper.CalculateRightKeys(t, resource.RefId));

            foreach (var assignmentResource in res)
            {
                var policy = await policyRetrievalPoint.GetPolicyVersionAsync(assignmentResource.PolicyPath, assignmentResource.PolicyVersion, cancellationToken);
                var availableRights = policy.Rules.SelectMany(t => DelegationCheckHelper.CalculateRightKeys(t, assignmentResource.Resource.RefId));
                var validRights = policyRights.Intersect(availableRights); // Only valid actions

                foreach (var rightKey in validRights)
                {
                    var right = resourceRight.Rights.FirstOrDefault(t => t.Right.Key == rightKey);

                    if (right == null)
                    {
                        var rightKeyMetadata = rightKeys.FirstOrDefault(r => r.Key == rightKey);
                        right = new RightPermission()
                        {
                            Right = new RightDto
                            {
                                Key = rightKey,
                                Resource = rightKeyMetadata?.Resource,
                                Action = rightKeyMetadata?.Action,
                            },
                            Reason = assignmentResource.Reason,
                            Permissions = new List<PermissionDto>(),
                        };
                        resourceRight.Rights.Add(right);
                    }

                    if (!right.Permissions.Any(p =>
                        p.From.Id == assignmentResource.From.Id &&
                        p.To.Id == assignmentResource.To.Id &&
                        p.Role.Id == assignmentResource.Role.Id &&
                        p.Via?.Id == assignmentResource.Via?.Id &&
                        p.ViaRole?.Id == assignmentResource.ViaRole?.Id &&
                        p.Reason == assignmentResource.Reason))
                    {
                        right.Permissions.Add(new PermissionDto()
                        {
                            From = DtoMapper.Convert(assignmentResource.From),
                            To = DtoMapper.Convert(assignmentResource.To),
                            Role = DtoMapper.ConvertCompactRole(assignmentResource.Role),
                            Via = DtoMapper.Convert(assignmentResource.Via),
                            ViaRole = DtoMapper.ConvertCompactRole(assignmentResource.ViaRole),
                            Reason = assignmentResource.Reason
                        });
                    }
                }
            }

            result.Add(resourceRight);
        }

        return result;
    }

    private async Task<List<InstanceRightDto>> GetInstanceRights(Guid? fromId, Guid? toId, Guid? resourceId, string? instanceId, Guid? roleId, CancellationToken cancellationToken = default)
    {
        if (!fromId.HasValue && !toId.HasValue)
        {
            throw new ArgumentException("You need to specify from or to");
        }

        #region Data

        var baseQuery = dbContext.AssignmentInstances.AsNoTracking()
            .Include(t => t.Assignment)
            .ThenInclude(t => t.From)
            .Include(t => t.Assignment)
            .ThenInclude(t => t.To)
            .Include(t => t.Assignment)
            .ThenInclude(t => t.Role)
            .Include(t => t.Resource)
            .WhereIf(fromId.HasValue, t => t.Assignment.FromId == fromId.Value)
            .WhereIf(toId.HasValue, t => t.Assignment.ToId == toId.Value)
            .WhereIf(roleId.HasValue, t => t.Assignment.RoleId == roleId.Value)
            .WhereIf(resourceId.HasValue, t => t.ResourceId == resourceId.Value)
            .WhereIf(!string.IsNullOrEmpty(instanceId), t => t.InstanceId == instanceId);

        // Direct
        var direct = baseQuery
            .Select(t => new AssignmentInstanceQueryResult()
            {
                Resource = t.Resource,
                From = t.Assignment.From,
                To = t.Assignment.To,
                Role = t.Assignment.Role,
                InstanceId = t.InstanceId,
                PolicyPath = t.PolicyPath,
                PolicyVersion = t.PolicyVersion,
                Reason = AccessReasonFlag.Direct
            });

        // KeyRole
        var keyRoleResult = dbContext.AssignmentInstances.AsNoTracking()
            .Include(t => t.Assignment)
            .ThenInclude(t => t.From)
            .Include(t => t.Assignment)
            .ThenInclude(t => t.To)
            .Include(t => t.Assignment)
            .ThenInclude(t => t.Role)
            .Include(t => t.Resource)
           .WhereIf(roleId.HasValue, t => t.Assignment.RoleId == roleId.Value)
           .WhereIf(resourceId.HasValue, t => t.ResourceId == resourceId.Value)
           .WhereIf(!string.IsNullOrEmpty(instanceId), t => t.InstanceId == instanceId)
           .Join(
               dbContext.Assignments.AsNoTracking()
                   .Include(a => a.From)
                   .Include(a => a.To)
                   .Include(a => a.Role)
                   .Where(t => t.Role.IsKeyRole),
               ai => ai.Assignment.ToId,
               c => c.FromId,
               (ai, c) => new AssignmentInstanceQueryResult
               {
                   Resource = ai.Resource,
                   From = ai.Assignment.From,
                   To = c.To,
                   Role = ai.Assignment.Role,
                   Via = c.From,
                   ViaRole = c.Role,
                   InstanceId = ai.InstanceId,
                   PolicyPath = ai.PolicyPath,
                   PolicyVersion = ai.PolicyVersion
               })
           .WhereIf(fromId.HasValue, t => t.From.Id == fromId.Value)
           .WhereIf(toId.HasValue, t => t.To.Id == toId.Value)
           .Select(t => new AssignmentInstanceQueryResult()
           {
               Resource = t.Resource,
               From = t.From,
               To = t.To,
               Role = t.Role,
               InstanceId = t.InstanceId,
               PolicyPath = t.PolicyPath,
               PolicyVersion = t.PolicyVersion,
               Reason = AccessReasonFlag.KeyRole
           });
        var query = direct
            .Union(keyRoleResult);

        var res = await query.ToListAsync(cancellationToken);

        #endregion

        var result = new List<InstanceRightDto>();

        foreach (var resource in res.Select(t => t.Resource).DistinctBy(t => t.Id))
        {
            var internalResource = res.First().Resource;
            var rightKeys = await contextRetrievalService.GetResourcePolicyV2(internalResource.RefId, cancellationToken: cancellationToken);

            var instanceRight = new InstanceRightDto()
            {
                Resource = DtoMapper.Convert(internalResource),
                Instance = !string.IsNullOrEmpty(instanceId)
                    ? new InstanceDto { RefId = instanceId }
                    : null,
                Rights = new List<RightPermission>()
            };

            bool isApp = DelegationCheckHelper.IsAppResource(resource.RefId, out string org, out string app);
            var resourcePolicy = isApp ?
                await policyRetrievalPoint.GetPolicyAsync(org, app, cancellationToken) :
                await policyRetrievalPoint.GetPolicyAsync(resource.RefId, cancellationToken);

            var policyRights = resourcePolicy.Rules.SelectMany(t => DelegationCheckHelper.CalculateRightKeys(t, resource.RefId));

            foreach (var assignmentInstance in res)
            {
                var policy = await policyRetrievalPoint.GetPolicyVersionAsync(assignmentInstance.PolicyPath, assignmentInstance.PolicyVersion, cancellationToken);
                var availableRights = policy.Rules.SelectMany(t => DelegationCheckHelper.CalculateRightKeys(t, assignmentInstance.Resource.RefId));
                var validRights = policyRights.Intersect(availableRights);

                foreach (var rightKey in validRights)
                {
                    var right = instanceRight.Rights.FirstOrDefault(t => t.Right.Key == rightKey);

                    if (right == null)
                    {
                        var rightKeyMetadata = rightKeys.FirstOrDefault(r => r.Key == rightKey);
                        right = new RightPermission()
                        {
                            Right = new RightDto
                            {
                                Key = rightKey,
                                Resource = rightKeyMetadata?.Resource,
                                Action = rightKeyMetadata?.Action,
                            },
                            Reason = assignmentInstance.Reason,
                            Permissions = new List<PermissionDto>(),
                        };
                        instanceRight.Rights.Add(right);
                    }

                    if (!right.Permissions.Any(p =>
                        p.From.Id == assignmentInstance.From.Id &&
                        p.To.Id == assignmentInstance.To.Id &&
                        p.Role.Id == assignmentInstance.Role.Id &&
                        p.Via?.Id == assignmentInstance.Via?.Id &&
                        p.ViaRole?.Id == assignmentInstance.ViaRole?.Id &&
                        p.Reason == assignmentInstance.Reason))
                    {
                        right.Permissions.Add(new PermissionDto()
                        {
                            From = DtoMapper.Convert(assignmentInstance.From),
                            To = DtoMapper.Convert(assignmentInstance.To),
                            Role = DtoMapper.ConvertCompactRole(assignmentInstance.Role),
                            Via = DtoMapper.Convert(assignmentInstance.Via),
                            ViaRole = DtoMapper.ConvertCompactRole(assignmentInstance.ViaRole),
                            Reason = assignmentInstance.Reason
                        });
                    }
                }
            }

            result.Add(instanceRight);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SystemUserClientConnectionDto>> GetConnectionsToAgent(Guid viaId, Guid toId, Guid? fromId = null, CancellationToken cancellationToken = default)
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
            .WhereIf(fromId.HasValue, t => t.FromId == fromId)
            .ToListAsync(cancellationToken);

        return GetConnectionsAsSystemUserClientConnectionDto(result);
    }

    /// <inheritdoc />
    public async Task<Result<bool>> RemoveRoleAssignment(
        Guid fromId,
        Guid toId,
        string roleCode,
        Action<ConnectionOptions> configureConnections = null,
        CancellationToken cancellationToken = default)
    {
        var options = new ConnectionOptions(configureConnections);
        var (from, to) = await GetFromAndToEntities(fromId, toId, cancellationToken);
        var problem = ValidateWriteOpInput(from, to, options);
        if (problem is { })
        {
            return problem;
        }

        // Validate roleId if proveder is not Altinn 2 then the assignmnet is not alowed to be removed
        var role = await dbContext.Roles
            .AsNoTracking()
            .Where(r => r.Code == roleCode.ToLowerInvariant())
            .FirstOrDefaultAsync(cancellationToken);

        if (role == null)
        {
            return Problems.InvalidRoleCode;
        }

        if (role.ProviderId != ProviderConstants.Altinn2.Id)
        {
            return Problems.RoleAssignmentNotRevocable;
        }

        // Fetch assignment
        var existingAssignment = await dbContext.Assignments
            .AsNoTracking()
            .Where(e => e.FromId == from.Id)
            .Where(e => e.ToId == to.Id)
            .Where(e => e.RoleId == role.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingAssignment is null)
        {
            return false;
        }

        // Remove and save revoked assignment
        dbContext.Remove(existingAssignment);

        var result = await dbContext.SaveChangesAsync(cancellationToken);
        return result > 0;
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

internal class AssignmentResourceQueryResult
{
    internal Resource Resource { get; set; }

    internal Entity From { get; set; }

    internal Entity To { get; set; }

    internal Entity Via { get; set; }

    internal Role ViaRole { get; set; }

    internal Role Role { get; set; }

    internal string PolicyPath { get; set; }

    internal string PolicyVersion { get; set; }

    internal AccessReasonFlag Reason { get; set; }
}

internal class AssignmentInstanceQueryResult
{
    internal Resource Resource { get; set; }

    internal Entity From { get; set; }

    internal Entity To { get; set; }

    internal Entity Via { get; set; }

    internal Role ViaRole { get; set; }

    internal Role Role { get; set; }

    internal string InstanceId { get; set; }

    internal string PolicyPath { get; set; }

    internal string PolicyVersion { get; set; }

    internal AccessReasonFlag Reason { get; set; }
}
