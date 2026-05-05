using System.ComponentModel.DataAnnotations;
using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.Core.Utils;
using Altinn.AccessMgmt.Core.Validation;
using Altinn.AccessMgmt.PersistenceEF.Audit;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.ProblemDetails;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.Core.Services;

/// <summary>
/// Service for managing Maskinporten supplier relationships and scope delegations
/// </summary>
public class MaskinportenSupplierService(
    AppDbContext dbContext,
    IAuditAccessor auditAccessor,
    IConnectionService connectionService,
    ISingleRightsService singleRightsService,
    IEntityService entityService) : IMaskinportenSupplierService
{
    private const string MaskinportenSchemaTypeName = "MaskinportenSchema";

    /// <inheritdoc />
    public async Task<Result<AssignmentDto>> AddSupplier(Guid consumerId, Guid supplierId, CancellationToken cancellationToken = default)
    {
        var (validation, _) = await GetAndValidateOrganizations(consumerId, supplierId, cancellationToken);
        if (validation.IsProblem)
        {
            return validation.Problem;
        }

        // Check if assignment already exists
        var existingAssignment = await dbContext.Assignments
            .AsNoTracking()
            .Where(e => e.FromId == consumerId)
            .Where(e => e.ToId == supplierId)
            .Where(e => e.RoleId == RoleConstants.Supplier.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingAssignment is { })
        {
            return DtoMapper.Convert(existingAssignment);
        }

        // Create new supplier assignment
        var assignment = new Assignment()
        {
            FromId = consumerId,
            ToId = supplierId,
            RoleId = RoleConstants.Supplier.Id,
        };

        await dbContext.Assignments.AddAsync(assignment, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return DtoMapper.Convert(assignment);
    }

    /// <inheritdoc />
    public async Task<ValidationProblemInstance> RemoveSupplier(Guid consumerId, Guid supplierId, bool cascade = false, CancellationToken cancellationToken = default)
    {
        var (validation, _) = await GetAndValidateOrganizations(consumerId, supplierId, cancellationToken);
        if (validation.IsProblem)
        {
            return validation.Problem as ValidationProblemInstance;
        }

        var existingAssignment = await dbContext.Assignments
            .AsTracking()
            .Where(e => e.FromId == consumerId)
            .Where(e => e.ToId == supplierId)
            .Where(e => e.RoleId == RoleConstants.Supplier.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingAssignment is null)
        {
            return null;
        }

        // Get all delegated resources for this supplier assignment
        var assignedResources = await dbContext.AssignmentResources
            .AsTracking()
            .Where(p => p.AssignmentId == existingAssignment.Id)
            .ToListAsync(cancellationToken);

        if (assignedResources.Any())
        {
            if (!cascade)
            {
                // Resources exist and cascade not requested - fail with validation error
                return ValidationComposer.Validate(
                    ResourceValidation.HasAssignedResources(assignedResources)
                );
            }

            // Cascade: Clear all policy rules first (all-or-nothing)
            // Cannot use ConnectionService.RemoveResource because it looks for RoleConstants.Rightholder,
            // but supplier delegations use RoleConstants.Supplier
            var clearedVersions = new List<(AssignmentResource Resource, string NewVersion)>();

            foreach (var assignmentResource in assignedResources)
            {
                // Clear delegation policy rules for this resource
                var newVersion = await singleRightsService.ClearPolicyRules(
                    assignmentResource.PolicyPath,
                    assignmentResource.PolicyVersion,
                    cancellationToken);

                if (newVersion is null)
                {
                    // Policy clear failed - abort the entire cascade operation
                    // All previously cleared policies remain cleared (no rollback mechanism)
                    // But database records are NOT deleted, maintaining partial consistency
                    return ValidationComposer.Validate(
                        ResourceValidation.PolicyCascadeClearFailed(newVersion)
                    );
                }

                clearedVersions.Add((assignmentResource, newVersion));
            }

            // All policy clears succeeded - now safe to delete database records
            foreach (var (resource, newVersion) in clearedVersions)
            {
                // Update version to track the clear operation
                resource.PolicyVersion = newVersion;

                // Remove the assignment resource record
                dbContext.Remove(resource);
            }
        }

        // All resources revoked (or none existed) - safe to delete the assignment
        dbContext.Remove(existingAssignment);
        await dbContext.SaveChangesAsync(cancellationToken);

        return null;
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<ConnectionDto>>> GetSuppliers(Guid consumerId, Guid? supplierId = null, CancellationToken cancellationToken = default)
    {
        var consumer = await GetEntity(consumerId, cancellationToken);
        if (consumer.IsProblem)
        {
            return consumer.Problem;
        }

        if (!IsOrganization(consumer.Value))
        {
            return Problems.PartyNotFound;
        }

        // Direct EF query for supplier assignments
        var assignments = await dbContext.Assignments
            .AsNoTracking()
            .Include(a => a.To)
            .ThenInclude(e => e.Type)
            .Include(a => a.Role)
            .Where(a => a.FromId == consumerId)
            .Where(a => a.RoleId == RoleConstants.Supplier.Id)
            .WhereIf(supplierId.HasValue, a => a.ToId == supplierId.Value)
            .ToListAsync(cancellationToken);

        var suppliers = assignments.Select(a => new ConnectionDto
        {
            Party = DtoMapper.Convert(a.To),
            Roles = new List<CompactRoleDto> { DtoMapper.ConvertCompactRole(a.Role) }
        }).ToList();

        return suppliers;
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<ConnectionDto>>> GetConsumers(Guid supplierId, Guid? consumerId = null, CancellationToken cancellationToken = default)
    {
        var supplier = await GetEntity(supplierId, cancellationToken);
        if (supplier.IsProblem)
        {
            return supplier.Problem;
        }

        if (!IsOrganization(supplier.Value))
        {
            return Problems.PartyNotFound;
        }

        // Direct EF query for consumer assignments
        var assignments = await dbContext.Assignments
            .AsNoTracking()
            .Include(a => a.From)
            .ThenInclude(e => e.Type)
            .Include(a => a.Role)
            .Where(a => a.ToId == supplierId)
            .Where(a => a.RoleId == RoleConstants.Supplier.Id)
            .WhereIf(consumerId.HasValue, a => a.FromId == consumerId.Value)
            .ToListAsync(cancellationToken);

        var consumers = assignments.Select(a => new ConnectionDto
        {
            Party = DtoMapper.Convert(a.From),
            Roles = new List<CompactRoleDto> { DtoMapper.ConvertCompactRole(a.Role) }
        }).ToList();

        return consumers;
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<ResourcePermissionDto>>> GetSupplierResources(
        Guid consumerId,
        Guid? supplierId = null,
        Guid? resourceId = null,
        CancellationToken cancellationToken = default)
    {
        var consumer = await GetEntity(consumerId, cancellationToken);
        if (consumer.IsProblem)
        {
            return consumer.Problem;
        }

        if (!IsOrganization(consumer.Value))
        {
            return Problems.PartyNotFound;
        }

        // Direct EF query for delegated resources
        var delegatedResources = await dbContext.AssignmentResources
            .AsNoTracking()
            .Include(ar => ar.Assignment)
            .ThenInclude(a => a.To)
            .ThenInclude(e => e.Type)
            .Include(ar => ar.Assignment)
            .ThenInclude(a => a.Role)
            .Include(ar => ar.Resource)
            .ThenInclude(r => r.Type)
            .Where(ar => ar.Assignment.FromId == consumerId)
            .Where(ar => ar.Assignment.RoleId == RoleConstants.Supplier.Id)
            .Where(ar => ar.Resource.Type.Name == MaskinportenSchemaTypeName)
            .WhereIf(supplierId.HasValue, ar => ar.Assignment.ToId == supplierId.Value)
            .WhereIf(resourceId.HasValue, ar => ar.ResourceId == resourceId.Value)
            .ToListAsync(cancellationToken);

        // Group by resource
        var groupedByResource = delegatedResources
            .GroupBy(ar => ar.ResourceId)
            .Select(g => new ResourcePermissionDto
            {
                Resource = DtoMapper.Convert(g.First().Resource),
                Permissions = g.Select(ar => new PermissionDto
                {
                    From = DtoMapper.Convert(consumer.Value),
                    To = DtoMapper.Convert(ar.Assignment.To),
                    Role = DtoMapper.ConvertCompactRole(ar.Assignment.Role)
                }).ToList()
            })
            .ToList();

        return groupedByResource;
    }

    /// <inheritdoc />
    public async Task<Result<IEnumerable<ResourcePermissionDto>>> GetConsumerResources(
        Guid supplierId,
        Guid? consumerId = null,
        Guid? resourceId = null,
        CancellationToken cancellationToken = default)
    {
        var supplier = await GetEntity(supplierId, cancellationToken);
        if (supplier.IsProblem)
        {
            return supplier.Problem;
        }

        if (!IsOrganization(supplier.Value))
        {
            return Problems.PartyNotFound;
        }

        // Direct EF query for delegated resources
        var delegatedResources = await dbContext.AssignmentResources
            .AsNoTracking()
            .Include(ar => ar.Assignment)
            .ThenInclude(a => a.From)
            .ThenInclude(e => e.Type)
            .Include(ar => ar.Assignment)
            .ThenInclude(a => a.Role)
            .Include(ar => ar.Resource)
            .ThenInclude(r => r.Type)
            .Where(ar => ar.Assignment.ToId == supplierId)
            .Where(ar => ar.Assignment.RoleId == RoleConstants.Supplier.Id)
            .Where(ar => ar.Resource.Type.Name == MaskinportenSchemaTypeName)
            .WhereIf(consumerId.HasValue, ar => ar.Assignment.FromId == consumerId.Value)
            .WhereIf(resourceId.HasValue, ar => ar.ResourceId == resourceId.Value)
            .ToListAsync(cancellationToken);

        // Group by resource
        var groupedByResource = delegatedResources
            .GroupBy(ar => ar.ResourceId)
            .Select(g => new ResourcePermissionDto
            {
                Resource = DtoMapper.Convert(g.First().Resource),
                Permissions = g.Select(ar => new PermissionDto
                {
                    From = DtoMapper.Convert(ar.Assignment.From),
                    To = DtoMapper.Convert(supplier.Value),
                    Role = DtoMapper.ConvertCompactRole(ar.Assignment.Role)
                }).ToList()
            })
            .ToList();

        return groupedByResource;
    }

    /// <inheritdoc />
    public async Task<Result<ResourceCheckDto>> ResourceDelegationCheck(
        Guid authenticatedUserUuid,
        Guid consumerId,
        string resource,
        string languageCode = "nb",
        CancellationToken cancellationToken = default)
    {
        var consumer = await GetEntity(consumerId, cancellationToken);
        if (consumer.IsProblem)
        {
            return consumer.Problem;
        }

        if (!IsOrganization(consumer.Value))
        {
            return Problems.PartyNotFound;
        }

        // Validate that the resource is a MaskinportenSchema
        var resourceObj = await dbContext.Resources
            .Include(r => r.Type)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.RefId == resource, cancellationToken);

        if (resourceObj == null || !resourceObj.Type.Name.Equals(MaskinportenSchemaTypeName, StringComparison.InvariantCultureIgnoreCase))
        {
            return Problems.InvalidResource;
        }

        // Delegate to ConnectionService with allowMaskinportenSchema = true for MaskinportenSchema resources
        // This allows delegation even if delegable=false in Resource Registry, but still requires valid access rights.
        // 
        // Technical note: allowMaskinportenSchema bypasses the MaskinportenSchema denial logic in 
        // ConnectionService.MapFromInternalToExternalRight, allowing rights to have Result=true
        // when the caller has proper authorization. This enables AddResource to succeed with delegable rights.
        return await connectionService.ResourceDelegationCheck(
            authenticatedUserUuid,
            consumerId,
            resource,
            configureConnection: null,
            languageCode: languageCode,
            ignoreDelegableFlag: false,
            allowMaskinportenSchema: true,
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<bool>> AddResource(
        Guid consumerId,
        Guid supplierId,
        string resource,
        CancellationToken cancellationToken = default)
    {
        var (validation, entities) = await GetAndValidateOrganizations(consumerId, supplierId, cancellationToken);
        if (validation.IsProblem)
        {
            return validation.Problem;
        }

        // Validate resource exists and is MaskinportenSchema
        var resourceResult = await GetResourceByRefId(resource, cancellationToken);
        if (resourceResult.IsProblem)
        {
            return resourceResult.Problem;
        }

        var resourceObj = resourceResult.Value;

        // Perform delegation check
        var by = await GetEntity(auditAccessor.AuditValues.ChangedBy, cancellationToken);
        if (by.IsProblem)
        {
            return by.Problem;
        }

        var delegationCheck = await ResourceDelegationCheck(by.Value.Id, consumerId, resource, cancellationToken: cancellationToken);
        if (delegationCheck.IsProblem)
        {
            return delegationCheck.Problem;
        }

        // Get all delegable rights from the delegation check
        var delegableRightKeys = delegationCheck.Value.Rights
            .Where(r => r.Result)
            .Select(r => r.Right.Key)
            .ToList();

        if (!delegableRightKeys.Any())
        {
            return Problems.NotAuthorizedForDelegationRequest;
        }

        // Ensure supplier assignment exists
        var connection = await GetSuppliers(consumerId, supplierId, cancellationToken);
        if (!connection.IsSuccess || !connection.Value.Any())
        {
            return Problems.MissingConnection;
        }

        // Write delegation policy rules directly to avoid double delegation check
        // Cannot use ConnectionService.AddResource because it performs ResourceDelegationCheck
        // without allowMaskinportenSchema=true, which would deny all MaskinportenSchema rights
        var result = await singleRightsService.TryWriteDelegationPolicyRules(
            entities.Consumer,
            entities.Supplier,
            resourceObj,
            delegableRightKeys,
            by.Value,
            ignoreExistingPolicy: false,
            cancellationToken: cancellationToken);

        if (!result.Any() || !result.All(r => r.CreatedSuccessfully))
        {
            return Problems.DelegationPolicyRuleWriteFailed;
        }

        return true;
    }

    /// <inheritdoc />
    public async Task<ValidationProblemInstance> RemoveResource(
        Guid consumerId,
        Guid supplierId,
        string resource,
        CancellationToken cancellationToken = default)
    {
        var (validation, _) = await GetAndValidateOrganizations(consumerId, supplierId, cancellationToken);
        if (validation.IsProblem)
        {
            return validation.Problem as ValidationProblemInstance;
        }

        // Validate resource exists and is MaskinportenSchema
        var resourceResult = await GetResourceByRefId(resource, cancellationToken);
        if (resourceResult.IsProblem)
        {
            return resourceResult.Problem as ValidationProblemInstance;
        }

        var resourceObj = resourceResult.Value;

        // Find the supplier assignment (not rightholder)
        var assignment = await dbContext.Assignments
            .AsNoTracking()
            .Include(a => a.From)
            .Include(a => a.To)
            .Where(a => a.FromId == consumerId)
            .Where(a => a.ToId == supplierId)
            .Where(a => a.RoleId == RoleConstants.Supplier.Id)  // Supplier, not Rightholder!
            .FirstOrDefaultAsync(cancellationToken);

        if (assignment is null)
        {
            return null;  // No supplier assignment exists
        }

        // Find the assignment resource for this specific resource
        var existingAssignmentResource = await dbContext.AssignmentResources
            .AsTracking()
            .Where(a => a.AssignmentId == assignment.Id)
            .Where(a => a.ResourceId == resourceObj.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingAssignmentResource is null)
        {
            return null;  // Resource not delegated to this supplier
        }

        // Clear delegation policy rules
        var newVersion = await singleRightsService.ClearPolicyRules(
            existingAssignmentResource.PolicyPath,
            existingAssignmentResource.PolicyVersion,
            cancellationToken);

        if (newVersion is null)
        {
            // Policy clear failed - do not delete DB record to maintain consistency
            return ValidationComposer.Validate(
                ResourceValidation.PolicyClearFailed(newVersion, resource)
            );
        }

        existingAssignmentResource.PolicyVersion = newVersion;

        // Remove the assignment resource
        dbContext.Remove(existingAssignmentResource);
        await dbContext.SaveChangesAsync(cancellationToken);

        return null;
    }

    #region Private Helper Methods

    private async Task<Result<Entity>> GetEntity(Guid entityId, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Entities
            .AsNoTracking()
            .Include(e => e.Type)
            .FirstOrDefaultAsync(e => e.Id == entityId, cancellationToken);

        if (entity is null)
        {
            return Problems.PartyNotFound;
        }

        return entity;
    }

    /// <inheritdoc />
    public async Task<Result<Entity>> GetEntity(string organizationNumber, CancellationToken cancellationToken = default)
    {
        var entity = await entityService.GetByOrgNoWithType(organizationNumber, cancellationToken);

        if (entity is null)
        {
            return Problems.PartyNotFound;
        }

        return entity;
    }

    /// <inheritdoc />
    public async Task<Result<Resource>> GetResourceByRefId(string resourceRefId, CancellationToken cancellationToken = default)
    {
        var resource = await dbContext.Resources
            .AsNoTracking()
            .Include(r => r.Type)
            .FirstOrDefaultAsync(r => r.RefId == resourceRefId, cancellationToken);

        if (resource is null)
        {
            return Problems.InvalidResource;
        }

        // Validate that the resource is a MaskinportenSchema
        if (!resource.Type.Name.Equals(MaskinportenSchemaTypeName, StringComparison.InvariantCultureIgnoreCase))
        {
            return ValidationComposer.Validate(
                ResourceValidation.ResourceTypeIs(resource, MaskinportenSchemaTypeName)
            );
        }

        return resource;
    }

    private async Task<(Result<bool> Validation, (Entity Consumer, Entity Supplier) Organizations)> GetAndValidateOrganizations(
        Guid consumerId,
        Guid supplierId,
        CancellationToken cancellationToken)
    {
        var entities = await dbContext.Entities
            .AsNoTracking()
            .Where(e => e.Id == consumerId || e.Id == supplierId)
            .Include(e => e.Type)
            .ToListAsync(cancellationToken);

        var consumer = entities.FirstOrDefault(e => e.Id == consumerId);
        var supplier = entities.FirstOrDefault(e => e.Id == supplierId);

        var problem = ValidationComposer.Validate(
            EntityValidation.FromExists(consumer),
            EntityValidation.ToExists(supplier),
            EntityValidation.FromIsNotTo(consumerId, supplierId, "party", "supplier"),
            EntityTypeValidation.FromIsOfType(consumer?.TypeId ?? Guid.Empty, [EntityTypeConstants.Organization.Id]),
            EntityTypeValidation.ToIsOfType(supplier?.TypeId ?? Guid.Empty, [EntityTypeConstants.Organization.Id])
        );

        if (problem is { })
        {
            return (problem, default);
        }

        return (true, (consumer, supplier));
    }

    private static bool IsOrganization(Entity entity)
        => entity.TypeId == EntityTypeConstants.Organization.Id;

    #endregion
}
