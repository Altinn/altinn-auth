using Altinn.AccessManagement.Core.Enums;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Repositories.Interfaces;
using Altinn.AccessManagement.Enums;
using Altinn.AccessMgmt.PersistenceEF.Audit;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Microsoft.EntityFrameworkCore;
using ResourceRegistryResourceType = Altinn.AccessManagement.Core.Models.ResourceRegistry.ResourceType;

namespace Altinn.AccessMgmt.Core.Services.Legacy;

/// <inheritdoc/>
public class DelegationMetadataEF(IAuditAccessor AuditAccessor, AppDbContext DbContext) : IDelegationMetadataRepository
{
    private string ConvertFromAppResourceId(string resourceId)
    {
        // TODO: Verify
        //// app_skd_skattemelding => skd/skattemelding
        return resourceId.Replace("app_", string.Empty).Replace('_', '/');
    }

    private string CheckAndConvertIfAppResourceId(string delegationChangeResourceId)
    {
        var resourceParts = delegationChangeResourceId.Split("/");
        if (resourceParts.Length == 2)
        {
            return $"app_{resourceParts[0]}_{resourceParts[1]}";
        }

        return delegationChangeResourceId;
    }

    private DelegationChange Convert(AssignmentResource assignmentResource)
    {
        return new DelegationChange()
        {
            DelegationChangeId = assignmentResource.Resource.Type.Name == "AltinnApp"
            ? assignmentResource.DelegationChangeId
            : 0,            
            ResourceRegistryDelegationChangeId = assignmentResource.Resource.Type.Name != "AltinnApp" 
            ? assignmentResource.DelegationChangeId
            : 0,
            Created = assignmentResource.Audit_ValidFrom.UtcDateTime,
            
            ResourceId = assignmentResource.Resource.Type.Name == "AltinnApp"
            ? ConvertFromAppResourceId(assignmentResource.Resource.RefId)
            : assignmentResource.Resource.RefId, 

            ResourceType = assignmentResource.Resource.Type.Name,
            BlobStoragePolicyPath = assignmentResource.PolicyPath,
            BlobStorageVersionId = assignmentResource.PolicyVersion,
            
            FromUuid = assignmentResource.Assignment.FromId,
            FromUuidType = ConvertEntityTypeToUuidType(assignmentResource.Assignment.From.TypeId),
            OfferedByPartyId = assignmentResource.Assignment.From.PartyId.Value,           
            
            PerformedByUuid = assignmentResource.Audit_ChangedBy.ToString(),
            PerformedByPartyId = assignmentResource.ChangedBy.PartyId,
            PerformedByUserId = assignmentResource.ChangedBy.UserId,
            PerformedByUuidType = ConvertEntityTypeToUuidType(assignmentResource.ChangedBy.TypeId),

            DelegationChangeType = DelegationChangeType.Grant,

            ToUuid = assignmentResource.Assignment.ToId,
            ToUuidType = ConvertEntityTypeToUuidType(assignmentResource.Assignment.To.TypeId),
            CoveredByUserId = assignmentResource.Assignment.To.UserId,
            CoveredByPartyId = assignmentResource.Assignment.To.UserId.HasValue ? null : assignmentResource.Assignment.To.PartyId, // If CoveredByUserId already has value skip setting CoveredByPartyId as old logic expects only one of these
        };
    }

    private InstanceDelegationChange Convert(AssignmentInstance assignmentInstance)
    {
        return new InstanceDelegationChange()
        {
            ToUuidType = ConvertEntityTypeToUuidType(assignmentInstance.Assignment.To.TypeId),
            ToUuid = assignmentInstance.Assignment.ToId,
            
            PerformedBy = assignmentInstance.Audit_ChangedBy.ToString(),
            PerformedByType = UuidType.NotSpecified,

            FromUuid = assignmentInstance.Assignment.FromId,
            FromUuidType = ConvertEntityTypeToUuidType(assignmentInstance.Assignment.From.TypeId),
            
            BlobStoragePolicyPath = assignmentInstance.PolicyPath,
            BlobStorageVersionId = assignmentInstance.PolicyVersion,
            
            ResourceId = assignmentInstance.ResourceId.ToString(),
            InstanceId = assignmentInstance.InstanceId,
            
            InstanceDelegationMode = InstanceDelegationMode.ParallelSigning,
            InstanceDelegationChangeId = assignmentInstance.DelegationChangeId,

            Created = assignmentInstance.Audit_ValidFrom.UtcDateTime,
        };
    }

    private UuidType ConvertEntityTypeToUuidType(Guid entityTypeId)
    {
        /*
        Missing map for:
            - UuidType.Resource
            - UuidType.Party
        */

        if (entityTypeId.Equals(EntityTypeConstants.Person))
        {
            return UuidType.Person;
        }

        if (entityTypeId.Equals(EntityTypeConstants.Organization))
        {
            return UuidType.Organization;
        }

        if (entityTypeId.Equals(EntityTypeConstants.SystemUser))
        {
            return UuidType.SystemUser;
        }

        if (entityTypeId.Equals(EntityTypeConstants.EnterpriseUser))
        {
            return UuidType.EnterpriseUser;
        }

        return UuidType.NotSpecified;
    }

    private async Task<DelegationChange> GetAssignmentResource(Guid id)
    {
        return Convert(await DbContext.AssignmentResources
            .Include(t => t.Assignment).ThenInclude(t => t.From)
            .Include(t => t.Assignment).ThenInclude(t => t.To)
            .Include(t => t.Resource).ThenInclude(t => t.Type)
            .Include(t => t.Resource).ThenInclude(t => t.Provider)
            .Include(t => t.ChangedBy)
            .SingleAsync(t => t.Id == id)
            );
    }

    private async Task<InstanceDelegationChange> GetAssignmentInstance(Guid id)
    {
        return Convert(await DbContext.AssignmentInstances
            .Include(t => t.Assignment).ThenInclude(t => t.From)
            .Include(t => t.Assignment).ThenInclude(t => t.To)
            .Include(t => t.Resource).ThenInclude(t => t.Type)
            .Include(t => t.Resource).ThenInclude(t => t.Provider)
            .SingleAsync(t => t.Id == id)
            );
    }

    private async Task<Resource> GetResource(string resourceIdentifier, CancellationToken cancellationToken = default)
    {
        return await DbContext.Resources.AsNoTracking().Include(t => t.Type).FirstOrDefaultAsync(t => t.RefId == resourceIdentifier, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<DelegationChange>> GetAllCurrentAppDelegationChanges(
        List<int> offeredByPartyIds, 
        List<string> altinnAppIds, 
        List<int> coveredByPartyIds = null, 
        List<int> coveredByUserIds = null, 
        CancellationToken cancellationToken = default
        )
    {
        if (offeredByPartyIds?.Count < 1)
        {
            throw new ArgumentNullException(nameof(offeredByPartyIds));
        }

        if (altinnAppIds?.Count < 1)
        {
            throw new ArgumentNullException(nameof(altinnAppIds));
        }

        if (coveredByPartyIds == null && coveredByUserIds == null)
        {
            throw new ArgumentException($"Both params: {nameof(coveredByUserIds)}, {nameof(coveredByPartyIds)} cannot be null.");
        }

        var resourceRefIds = altinnAppIds.Select(CheckAndConvertIfAppResourceId);

        var result = await DbContext.AssignmentResources.AsNoTracking()
            .Include(t => t.Assignment).ThenInclude(t => t.From)
            .Include(t => t.Assignment).ThenInclude(t => t.To)
            .Include(t => t.Resource).ThenInclude(t => t.Type)
            .Include(t => t.ChangedBy)
            .Where(t => resourceRefIds.Contains(t.Resource.RefId))
            .Where(t => t.Assignment.From.PartyId.HasValue && offeredByPartyIds.Contains(t.Assignment.From.PartyId.Value))
            .WhereIf(coveredByPartyIds != null && coveredByPartyIds.Any(), t => t.Assignment.To.PartyId.HasValue && coveredByPartyIds.Contains(t.Assignment.To.PartyId.Value))
            .WhereIf(coveredByUserIds != null && coveredByUserIds.Any(), t => t.Assignment.To.UserId.HasValue && coveredByUserIds.Contains(t.Assignment.To.UserId.Value))
            .ToListAsync(cancellationToken);

        return result.Select(Convert).ToList();
    }

    /// <inheritdoc/>
    public async Task<List<DelegationChange>> GetAllCurrentAppDelegationChanges(
        List<string> altinnAppIds, 
        List<int> fromPartyIds, 
        UuidType toUuidType, 
        Guid toUuid, 
        CancellationToken cancellationToken = default
        )
    {
        if (altinnAppIds?.Count < 1)
        {
            throw new ArgumentNullException(nameof(altinnAppIds));
        }

        if (fromPartyIds?.Count < 1)
        {
            throw new ArgumentNullException(nameof(fromPartyIds));
        }

        if (toUuidType == UuidType.NotSpecified)
        {
            throw new ArgumentException($"Param: {nameof(toUuidType)} must be specified.");
        }

        if (toUuid == Guid.Empty)
        {
            throw new ArgumentException($"Param: {nameof(toUuid)} must be specified.");
        }

        var resourceRefIds = altinnAppIds.Select(CheckAndConvertIfAppResourceId);

        var result = await DbContext.AssignmentResources.AsNoTracking()
            .Include(t => t.Assignment).ThenInclude(t => t.From)
            .Include(t => t.Assignment).ThenInclude(t => t.To)
            .Include(t => t.Resource).ThenInclude(t => t.Type)
            .Include(t => t.ChangedBy)
            .Where(t => resourceRefIds.Contains(t.Resource.RefId))
            .Where(t => t.Assignment.From.PartyId.HasValue && fromPartyIds.Contains(t.Assignment.From.PartyId.Value))
            .Where(t => t.Assignment.ToId == toUuid)
            .ToListAsync(cancellationToken);

        return result.Select(Convert).ToList();
    }

    /// <inheritdoc/>
    public async Task<DelegationChange> GetCurrentDelegationChange(ResourceAttributeMatchType resourceMatchType, string resourceId, int offeredByPartyId, int? coveredByPartyId, int? coveredByUserId, Guid? toUuid, UuidType toUuidType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(resourceId))
        {
            throw new ArgumentException($"Param: {nameof(resourceId)} cannot be null or whitespace.");
        }

        if (offeredByPartyId == 0)
        {
            throw new ArgumentException($"Param: {nameof(offeredByPartyId)} cannot be zero.");
        }

        if (coveredByPartyId == null && coveredByUserId == null && toUuidType != UuidType.SystemUser)
        {
            throw new ArgumentException($"All params: {nameof(coveredByUserId)}, {nameof(coveredByPartyId)}, {nameof(toUuid)} cannot be null.");
        }

        var from = await DbContext.Entities.AsNoTracking().FirstOrDefaultAsync(t => t.PartyId == offeredByPartyId, cancellationToken);
        
        if (from == null)
        {
            throw new Exception($"OfferedBy not found with partyId '{offeredByPartyId}'");
        }

        var result = await DbContext.AssignmentResources.AsNoTracking()
            .Include(t => t.Assignment).ThenInclude(t => t.To)
            .Include(t => t.Assignment).ThenInclude(t => t.From)
            .Include(t => t.Resource).ThenInclude(t => t.Type)
            .Include(t => t.ChangedBy)
            .Where(t => t.Resource.RefId == CheckAndConvertIfAppResourceId(resourceId))
            .Where(t => t.Assignment.FromId == from.Id)
            .WhereIf(coveredByPartyId != null, t => t.Assignment.To.PartyId == coveredByPartyId)
            .WhereIf(coveredByUserId != null, t => t.Assignment.To.UserId == coveredByUserId)
            .WhereIf(toUuid.HasValue, t => t.Assignment.ToId == toUuid.Value)
            .SingleOrDefaultAsync(cancellationToken);

        return result == null 
            ? null
            : Convert(result);
    }

    /// <inheritdoc/>
    public async Task<DelegationChange> InsertDelegation(ResourceAttributeMatchType resourceMatchType, DelegationChange delegationChange, CancellationToken cancellationToken = default)
    {
        delegationChange.DelegationChangeId = delegationChange.DelegationChangeId == 0 ? 1 : delegationChange.DelegationChangeId;

        if (AuditAccessor.AuditValues.ChangedBySystem == SystemEntityConstants.Altinn2AddRulesApi)
        {
            var performedByValid = Guid.TryParse(delegationChange.PerformedByUuid, out var performedById);
            var changedBy = performedByValid ? performedById : AuditAccessor.AuditValues.ChangedBy;
            var validFrom = delegationChange.Created.HasValue ? delegationChange.Created.Value : AuditAccessor.AuditValues.ValidFrom;
            var operationId = AuditAccessor.AuditValues.OperationId; // delegationChange.DelegationChangeId > 1 ? delegationChange.DelegationChangeId.ToString() : AuditAccessor.AuditValues.OperationId;

            AuditAccessor.AuditValues = new AuditValues(changedBy, SystemEntityConstants.Altinn2AddRulesApi, operationId, validFrom);
        }

        var resource = await DbContext.Resources.AsNoTracking().Include(t => t.Type).SingleAsync(t => t.RefId == CheckAndConvertIfAppResourceId(delegationChange.ResourceId), cancellationToken);

        var role = RoleConstants.Rightholder;
        if (resource.Type.Name == "MaskinportenSchema")
        {
            role = RoleConstants.Supplier;
        }

        var from = await DbContext.Entities.AsNoTracking().SingleAsync(t => t.PartyId == delegationChange.OfferedByPartyId, cancellationToken);
        var to = delegationChange.CoveredByUserId.HasValue ?
            await DbContext.Entities.AsNoTracking().SingleAsync(t => t.UserId == delegationChange.CoveredByUserId, cancellationToken) :
            await DbContext.Entities.AsNoTracking().SingleAsync(t => t.PartyId == delegationChange.CoveredByPartyId, cancellationToken);

        var assignment = await DbContext.Assignments.FirstOrDefaultAsync(t => t.FromId == from.Id && t.ToId == to.Id && t.RoleId == role.Id, cancellationToken);
        AssignmentResource assignmentResource = null;

        if (assignment == null)
        {
            if (delegationChange.DelegationChangeType == DelegationChangeType.RevokeLast)
            {
                return null;
            }

            assignment = new Assignment()
            {
                Id = Guid.CreateVersion7(),
                FromId = from.Id,
                ToId = to.Id,
                RoleId = role.Id
            };
            DbContext.Assignments.Add(assignment);
            await DbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            assignmentResource = await DbContext.AssignmentResources.FirstOrDefaultAsync(t => t.AssignmentId == assignment.Id && t.ResourceId == resource.Id, cancellationToken);
        }

        if (assignmentResource == null)
        {
            if (delegationChange.DelegationChangeType != DelegationChangeType.RevokeLast)
            {
                assignmentResource = new AssignmentResource()
                {
                    Id = Guid.CreateVersion7(),
                    AssignmentId = assignment.Id,
                    ResourceId = resource.Id,
                    PolicyPath = delegationChange.BlobStoragePolicyPath,
                    PolicyVersion = delegationChange.BlobStorageVersionId,
                    DelegationChangeId = delegationChange.DelegationChangeId,
                };
                DbContext.AssignmentResources.Add(assignmentResource);
                await DbContext.SaveChangesAsync(cancellationToken);

                return await GetAssignmentResource(assignmentResource.Id);
            }

            return null;

            /*
            // If we want audit log
            else
            {
                assignmentResource = new AssignmentResource()
                {
                    Id = Guid.CreateVersion7(),
                    AssignmentId = assignment.Id,
                    ResourceId = resource.Id,
                    PolicyPath = delegationChange.BlobStoragePolicyPath,
                    PolicyVersion = delegationChange.BlobStorageVersionId,
                    DelegationChangeId = delegationChange.DelegationChangeId,
                };
                DbContext.AssignmentResources.Add(assignmentResource);
                await DbContext.SaveChangesAsync(cancellationToken);

                DbContext.AssignmentResources.Remove(assignmentResource);
                await DbContext.SaveChangesAsync(cancellationToken);
            }
            */
        }
        else
        {
            if (delegationChange.DelegationChangeType == DelegationChangeType.RevokeLast)
            {
                /*
                // If we want audit log
                assignmentResource.PolicyPath = delegationChange.BlobStoragePolicyPath;
                assignmentResource.PolicyVersion = delegationChange.BlobStorageVersionId;
                assignmentResource.DelegationChangeId = delegationChange.DelegationChangeId;
                await DbContext.SaveChangesAsync(cancellationToken);
                */

                DbContext.AssignmentResources.Remove(assignmentResource);
                await DbContext.SaveChangesAsync(cancellationToken);

                return null;
            }
            else
            {
                assignmentResource.PolicyPath = delegationChange.BlobStoragePolicyPath;
                assignmentResource.PolicyVersion = delegationChange.BlobStorageVersionId;
                assignmentResource.DelegationChangeId = delegationChange.DelegationChangeId;

                await DbContext.SaveChangesAsync(cancellationToken);

                return await GetAssignmentResource(assignmentResource.Id);
            }
        }
    }

    /// <summary>
    ///  Fetches all instance delegated to given param
    /// </summary>
    /// <param name="toUuid">list of parties that has received an instance delegation</param>
    /// <param name="cancellationToken">cancellation token</param>
    /// <returns></returns>
    public async Task<List<InstanceDelegationChange>> GetAllCurrentReceivedInstanceDelegations(List<Guid> toUuid, CancellationToken cancellationToken = default)
    {
        var result = await DbContext.AssignmentInstances.AsNoTracking()
          .Include(t => t.Assignment).ThenInclude(t => t.From)
          .Include(t => t.Assignment).ThenInclude(t => t.To)
          .Include(t => t.Resource).ThenInclude(t => t.Type)
          .Where(t => toUuid.Contains(t.Assignment.ToId))
          .ToListAsync(cancellationToken);

        return result.Select(Convert).ToList();
    }

    /// <inheritdoc />
    public async Task<InstanceDelegationChange> GetLastInstanceDelegationChange(InstanceDelegationChangeRequest request, CancellationToken cancellationToken = default)
    {
        var result = await DbContext.AssignmentInstances.AsNoTracking()
           .Include(t => t.Assignment).ThenInclude(t => t.From)
           .Include(t => t.Assignment).ThenInclude(t => t.To)
           .Include(t => t.Resource).ThenInclude(t => t.Type)

           .Where(t => t.Resource.RefId == request.Resource)
           .Where(t => t.InstanceId == request.Instance)
           .Where(t => t.Assignment.FromId == request.FromUuid)
           .Where(t => t.Assignment.ToId == request.ToUuid)
           .SingleAsync(cancellationToken);

        return Convert(result);
    }

    /// <inheritdoc />
    public async Task<InstanceDelegationChange> InsertInstanceDelegation(InstanceDelegationChange instanceDelegationChange, CancellationToken cancellationToken = default)
    {
        instanceDelegationChange.InstanceDelegationChangeId = instanceDelegationChange.InstanceDelegationChangeId == 0 ? 1 : instanceDelegationChange.InstanceDelegationChangeId;

        if (AuditAccessor.AuditValues.ChangedBySystem == SystemEntityConstants.Altinn2AddRulesApi)
        {
            var performedByValid = Guid.TryParse(instanceDelegationChange.PerformedBy, out var performedById);
            var changedBy = performedByValid ? performedById : AuditAccessor.AuditValues.ChangedBy;
            var validFrom = instanceDelegationChange.Created.HasValue ? instanceDelegationChange.Created.Value : AuditAccessor.AuditValues.ValidFrom;
            var operationId = AuditAccessor.AuditValues.OperationId; // delegationChange.DelegationChangeId > 1 ? delegationChange.DelegationChangeId.ToString() : AuditAccessor.AuditValues.OperationId;

            AuditAccessor.AuditValues = new AuditValues(changedBy, SystemEntityConstants.Altinn2AddRulesApi, operationId, validFrom);
        }

        var role = RoleConstants.Rightholder;
        var from = await DbContext.Entities.AsNoTracking().SingleAsync(t => t.Id == instanceDelegationChange.FromUuid, cancellationToken);
        var to = await DbContext.Entities.AsNoTracking().SingleAsync(t => t.Id == instanceDelegationChange.ToUuid, cancellationToken);
        var resource = await DbContext.Resources.AsNoTracking().SingleAsync(t => t.RefId == instanceDelegationChange.ResourceId, cancellationToken);

        var assignment = await DbContext.Assignments.FirstOrDefaultAsync(t => t.FromId == from.Id && t.ToId == to.Id && t.RoleId == role.Id, cancellationToken);
        if (assignment == null)
        {
            assignment = new Assignment()
            {
                Id = Guid.CreateVersion7(),
                FromId = from.Id,
                ToId = to.Id,
                RoleId = role.Id
            };
            DbContext.Assignments.Add(assignment);
            await DbContext.SaveChangesAsync(cancellationToken);
        }

        var assignmentInstance = await DbContext.AssignmentInstances.FirstOrDefaultAsync(t => t.AssignmentId == assignment.Id && t.ResourceId == resource.Id && t.InstanceId == instanceDelegationChange.InstanceId, cancellationToken);

        if (assignmentInstance == null)
        {
            if (instanceDelegationChange.DelegationChangeType != DelegationChangeType.RevokeLast)
            {
                assignmentInstance = new AssignmentInstance()
                {
                    Id = Guid.CreateVersion7(),
                    AssignmentId = assignment.Id,
                    ResourceId = resource.Id,
                    InstanceId = instanceDelegationChange.InstanceId,
                    PolicyPath = instanceDelegationChange.BlobStoragePolicyPath,
                    PolicyVersion = instanceDelegationChange.BlobStorageVersionId,
                    DelegationChangeId = instanceDelegationChange.InstanceDelegationChangeId,
                };
                DbContext.AssignmentInstances.Add(assignmentInstance);
            }
            //// If we want audit log
            /*
            else
            {
                assignmentInstance = new AssignmentInstance()
                {
                    Id = Guid.CreateVersion7(),
                    AssignmentId = assignment.Id,
                    ResourceId = resource.Id,
                    InstanceId = instanceDelegationChange.InstanceId,
                    PolicyPath = instanceDelegationChange.BlobStoragePolicyPath,
                    PolicyVersion = instanceDelegationChange.BlobStorageVersionId,
                    DelegationChangeId = instanceDelegationChange.InstanceDelegationChangeId,
                };
                DbContext.AssignmentInstances.Add(assignmentInstance);
                await DbContext.SaveChangesAsync(cancellationToken);

                DbContext.AssignmentInstances.Remove(assignmentInstance);
                await DbContext.SaveChangesAsync(cancellationToken);
            }
            */
        }
        else
        {
            if (instanceDelegationChange.DelegationChangeType == DelegationChangeType.RevokeLast)
            {
                /*
                // If we want audit log
                assignmentInstance.PolicyPath = instanceDelegationChange.BlobStoragePolicyPath;
                assignmentInstance.PolicyVersion = instanceDelegationChange.BlobStorageVersionId;
                assignmentInstance.DelegationChangeId = instanceDelegationChange.InstanceDelegationChangeId;
                await DbContext.SaveChangesAsync(cancellationToken);
                */

                DbContext.AssignmentInstances.Remove(assignmentInstance);
            }
            else
            {
                assignmentInstance.PolicyPath = instanceDelegationChange.BlobStoragePolicyPath;
                assignmentInstance.PolicyVersion = instanceDelegationChange.BlobStorageVersionId;
                assignmentInstance.DelegationChangeId = instanceDelegationChange.InstanceDelegationChangeId;
            }
        }

        await DbContext.SaveChangesAsync(cancellationToken);

        return await GetAssignmentInstance(assignmentInstance.Id);
    }

    /// <inheritdoc />
    public async Task<bool> InsertMultipleInstanceDelegations(List<PolicyWriteOutput> policyWriteOutputs, CancellationToken cancellationToken = default)
    {
        try
        {
            foreach (var policy in policyWriteOutputs)
            {
                var resource = await GetResource(policy.Rules.ResourceId, cancellationToken);

                var role = resource.Type.Name == "MaskinportenSchema"
                    ? RoleConstants.Supplier
                    : RoleConstants.Rightholder;

                var assignment = await DbContext.Assignments.FirstOrDefaultAsync(t => t.FromId == policy.Rules.FromUuid && t.ToId == policy.Rules.ToUuid && t.RoleId == role.Id, cancellationToken);

                if (assignment == null || resource == null)
                {
                    throw new Exception("Assignment or resource not found for given policy  ");
                }

                var assignmentInstance = await DbContext.AssignmentInstances.FirstOrDefaultAsync(t => t.AssignmentId == assignment.Id && t.ResourceId == resource.Id && t.InstanceId == policy.Rules.InstanceId, cancellationToken);

                if (assignmentInstance is null)
                {
                    assignmentInstance = new AssignmentInstance()
                    {
                        Id = Guid.CreateVersion7(),
                        AssignmentId = assignment.Id,
                        ResourceId = resource.Id,
                        InstanceId = policy.Rules.InstanceId,
                        DelegationChangeId = 0,
                        PolicyPath = policy.PolicyPath,
                        PolicyVersion = policy.VersionId,
                    };

                    DbContext.AssignmentInstances.Add(assignmentInstance);
                }

                if (policy.ChangeType == DelegationChangeType.RevokeLast)
                {
                    DbContext.AssignmentInstances.Remove(assignmentInstance);
                }
            }

            await DbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<List<InstanceDelegationChange>> GetAllLatestInstanceDelegationChanges(InstanceDelegationSource source, string resourceID, string instanceID, CancellationToken cancellationToken = default)
    {
        var result = await DbContext.AssignmentInstances.AsNoTracking()
           .Include(t => t.Assignment).ThenInclude(t => t.From)
           .Include(t => t.Assignment).ThenInclude(t => t.To)
           .Include(t => t.Resource).ThenInclude(t => t.Type)

           .Where(t => t.Resource.RefId == resourceID)
           .Where(t => t.InstanceId == instanceID)
           .ToListAsync(cancellationToken);

        return result.Select(Convert).ToList();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<InstanceDelegationChange>> GetActiveInstanceDelegations(List<string> resourceIds, Guid from, List<Guid> to, CancellationToken cancellationToken = default)
    {
        var result = await DbContext.AssignmentInstances.AsNoTracking()
           .Include(t => t.Assignment).ThenInclude(t => t.From)
           .Include(t => t.Assignment).ThenInclude(t => t.To)
           .Include(t => t.Resource).ThenInclude(t => t.Type)
           .Where(t => t.Assignment.FromId == from)
           .Where(t => resourceIds.Contains(t.Resource.RefId))
           .Where(t => to.Contains(t.Assignment.ToId))
           .ToListAsync(cancellationToken);

        return result.Select(Convert).ToList();
    }

    /// <inheritdoc/>
    public async Task<List<DelegationChange>> GetAllCurrentResourceRegistryDelegationChanges(List<int> offeredByPartyIds, List<string> resourceRegistryIds, List<int> coveredByPartyIds = null, int? coveredByUserId = null, CancellationToken cancellationToken = default)
    {
        if (offeredByPartyIds?.Count == 0)
        {
            throw new ArgumentNullException(nameof(offeredByPartyIds));
        }

        List<DelegationChange> delegationChanges = new List<DelegationChange>();
        var resourceRefIds = resourceRegistryIds.Select(CheckAndConvertIfAppResourceId).ToList();

        if (coveredByPartyIds?.Count > 0)
        {
            delegationChanges.AddRange(await GetReceivedResourceRegistryDelegationsForCoveredByPartys(coveredByPartyIds, offeredByPartyIds, resourceRefIds, cancellationToken: cancellationToken));
        }

        if (coveredByUserId.HasValue)
        {
            delegationChanges.AddRange(await GetReceivedResourceRegistryDelegationsForCoveredByUser(coveredByUserId.Value, offeredByPartyIds, resourceRefIds, cancellationToken: cancellationToken));
        }

        return delegationChanges;
    }

    /// <inheritdoc/>
    public async Task<List<DelegationChange>> GetAllCurrentResourceRegistryDelegationChanges(List<string> resourceRegistryIds, List<int> fromPartyIds, UuidType toUuidType, Guid toUuid, CancellationToken cancellationToken = default)
    {
        return await GetAllCurrentAppDelegationChanges(
            altinnAppIds: resourceRegistryIds,
            fromPartyIds: fromPartyIds,
            toUuidType: toUuidType,
            toUuid: toUuid,
            cancellationToken: cancellationToken
            );
    }

    /// <inheritdoc/>
    public async Task<List<DelegationChange>> GetOfferedResourceRegistryDelegations(int offeredByPartyId, List<string> resourceRegistryIds = null, List<ResourceRegistryResourceType> resourceTypes = null, CancellationToken cancellationToken = default)
    {
        var resourceTypeNames = resourceTypes?.Select(MapResourceTypeToResourceTypeName).ToList();
        var resourceRefIds = resourceRegistryIds?.Select(CheckAndConvertIfAppResourceId);
        var result = await DbContext.AssignmentResources.AsNoTracking()
           .Include(t => t.Assignment).ThenInclude(t => t.From)
           .Include(t => t.Assignment).ThenInclude(t => t.To)
           .Include(t => t.Resource).ThenInclude(t => t.Type)
           .Include(t => t.ChangedBy)
           .Where(t => t.Assignment.From.PartyId.HasValue && t.Assignment.From.PartyId.Value == offeredByPartyId)
           .WhereIf(resourceRefIds != null && resourceRefIds.Any(), t => resourceRefIds.Contains(t.Resource.RefId))
           .WhereIf(resourceTypes != null && resourceTypes.Any(), t => resourceTypeNames.Contains(t.Resource.Type.Name))
           .ToListAsync(cancellationToken);

        return result.Select(Convert).ToList();
    }

    /// <inheritdoc/>
    public async Task<List<DelegationChange>> GetReceivedResourceRegistryDelegationsForCoveredByPartys(List<int> coveredByPartyIds, List<int> offeredByPartyIds = null, List<string> resourceRegistryIds = null, List<ResourceRegistryResourceType> resourceTypes = null, CancellationToken cancellationToken = default)
    {
        var resourceTypeNames = resourceTypes?.Select(MapResourceTypeToResourceTypeName).ToList();
        var resourceRefIds = resourceRegistryIds?.Select(CheckAndConvertIfAppResourceId);
        var result = await DbContext.AssignmentResources.AsNoTracking()
           .Include(t => t.Assignment).ThenInclude(t => t.From)
           .Include(t => t.Assignment).ThenInclude(t => t.To)
           .Include(t => t.Resource).ThenInclude(t => t.Type)
           .Include(t => t.ChangedBy)
           .Where(t => t.Assignment.To.PartyId.HasValue && coveredByPartyIds.Contains(t.Assignment.To.PartyId.Value))
           .WhereIf(resourceRefIds != null && resourceRefIds.Any(), t => resourceRefIds.Contains(t.Resource.RefId))
           .WhereIf(offeredByPartyIds != null && offeredByPartyIds.Any(), t => t.Assignment.From.PartyId.HasValue && offeredByPartyIds.Contains(t.Assignment.From.PartyId.Value))
           .WhereIf(resourceTypes != null && resourceTypes.Any(), t => resourceTypeNames.Contains(t.Resource.Type.Name))
           .ToListAsync(cancellationToken);

        return result.Select(Convert).ToList();
    }

    /// <inheritdoc/>
    public async Task<List<DelegationChange>> GetReceivedResourceRegistryDelegationsForCoveredByUser(int coveredByUserId, List<int> offeredByPartyIds, List<string> resourceRegistryIds = null, List<ResourceRegistryResourceType> resourceTypes = null, CancellationToken cancellationToken = default)
    {
        var resourceTypeNames = resourceTypes?.Select(MapResourceTypeToResourceTypeName).ToList();
        var resourceRefIds = resourceRegistryIds?.Select(CheckAndConvertIfAppResourceId);
        var result = await DbContext.AssignmentResources.AsNoTracking()
           .Include(t => t.Assignment).ThenInclude(t => t.From)
           .Include(t => t.Assignment).ThenInclude(t => t.To)
           .Include(t => t.Resource).ThenInclude(t => t.Type)
           .Include(t => t.ChangedBy)
           .Where(t => t.Assignment.To.UserId.HasValue && t.Assignment.To.UserId.Value == coveredByUserId)
           .Where(t => t.Assignment.From.PartyId.HasValue && offeredByPartyIds.Contains(t.Assignment.From.PartyId.Value))
           .WhereIf(resourceRefIds != null && resourceRefIds.Any(), t => resourceRefIds.Contains(t.Resource.RefId))
           .WhereIf(resourceTypes != null && resourceTypes.Any(), t => resourceTypeNames.Contains(t.Resource.Type.Name))
           .ToListAsync(cancellationToken);

        return result.Select(Convert).ToList();
    }

    /// <inheritdoc/>
    public async Task<List<DelegationChange>> GetResourceRegistryDelegationChanges(List<string> resourceIds, int offeredByPartyId, int coveredByPartyId, ResourceRegistryResourceType resourceType, CancellationToken cancellationToken = default)
    {
        var resourceTypeName = MapResourceTypeToResourceTypeName(resourceType);
        var resourceRefIds = resourceIds.Select(CheckAndConvertIfAppResourceId);
        var result = await DbContext.AssignmentResources.AsNoTracking()
           .Include(t => t.Assignment).ThenInclude(t => t.From)
           .Include(t => t.Assignment).ThenInclude(t => t.To)
           .Include(t => t.Resource).ThenInclude(t => t.Type)
           .Include(t => t.ChangedBy)
           .Where(t => t.Assignment.From.PartyId.HasValue && t.Assignment.From.PartyId.Value == offeredByPartyId)
           .Where(t => t.Assignment.To.PartyId.HasValue && t.Assignment.To.PartyId.Value == coveredByPartyId)
           .Where(t => resourceRefIds.Contains(t.Resource.RefId))
           .Where(t => t.Resource.Type.Name == resourceTypeName)
           .ToListAsync(cancellationToken);

        return result.Select(Convert).ToList();
    }

    /// <inheritdoc/>
    public async Task<List<DelegationChange>> GetOfferedDelegations(List<int> offeredByPartyIds, CancellationToken cancellationToken = default)
    {
        var result = await DbContext.AssignmentResources.AsNoTracking()
           .Include(t => t.Assignment).ThenInclude(t => t.From)
           .Include(t => t.Assignment).ThenInclude(t => t.To)
           .Include(t => t.Resource).ThenInclude(t => t.Type)
           .Include(t => t.ChangedBy)
           .Where(t => t.Assignment.From.PartyId.HasValue && offeredByPartyIds.Contains(t.Assignment.From.PartyId.Value))
           .Where(t => t.Assignment.RoleId == RoleConstants.Rightholder)
           .ToListAsync(cancellationToken);

        return result.Select(Convert).ToList();
    }

    /// <inheritdoc/>
    public async Task<List<DelegationChange>> GetAllDelegationChangesForAuthorizedParties(List<int> coveredByUserIds, List<int> coveredByPartyIds, CancellationToken cancellationToken = default)
    {
        var partyChanges = DbContext.AssignmentResources.AsNoTracking()
          .Include(t => t.Assignment).ThenInclude(t => t.From)
          .Include(t => t.Assignment).ThenInclude(t => t.To)
          .Include(t => t.Resource).ThenInclude(t => t.Type)
          .Include(t => t.ChangedBy)
          .Where(t => t.Assignment.To.PartyId.HasValue && coveredByPartyIds.Contains(t.Assignment.To.PartyId.Value))
          .Where(t => t.Assignment.RoleId == RoleConstants.Rightholder);

        var userChanges = DbContext.AssignmentResources.AsNoTracking()
          .Include(t => t.Assignment).ThenInclude(t => t.From)
          .Include(t => t.Assignment).ThenInclude(t => t.To)
          .Include(t => t.Resource).ThenInclude(t => t.Type)
          .Include(t => t.ChangedBy)
          .Where(t => t.Assignment.To.UserId.HasValue && coveredByUserIds.Contains(t.Assignment.To.UserId.Value))
          .Where(t => t.Assignment.RoleId == RoleConstants.Rightholder);

        var result = await partyChanges.Union(userChanges).ToListAsync(cancellationToken);

        return result.Select(Convert).ToList();
    }

    /// <inheritdoc/>
    public async Task<List<DelegationChange>> GetAllDelegationChangesForAuthorizedParties(List<Guid> toPartyUuids, CancellationToken cancellationToken = default)
    {
        var result = await DbContext.AssignmentResources.AsNoTracking()
          .Include(t => t.Assignment).ThenInclude(t => t.From)
          .Include(t => t.Assignment).ThenInclude(t => t.To)
          .Include(t => t.Resource).ThenInclude(t => t.Type)
          .Include(t => t.ChangedBy)
          .Where(t => toPartyUuids.Contains(t.Assignment.ToId))
          .Where(t => t.Assignment.RoleId == RoleConstants.Rightholder)
          .ToListAsync(cancellationToken);

        return result.Select(Convert).ToList();
    }

    Task<List<DelegationChange>> IDelegationMetadataRepository.GetNextPageAppDelegationChanges(long startFeedIndex, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    Task<List<DelegationChange>> IDelegationMetadataRepository.GetNextPageResourceDelegationChanges(long startFeedIndex, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    Task<List<InstanceDelegationChange>> IDelegationMetadataRepository.GetNextPageInstanceDelegationChanges(long startFeedIndex, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    private static string MapResourceTypeToResourceTypeName(ResourceRegistryResourceType resourceType)
    {
        return resourceType switch
        {
            ResourceRegistryResourceType.AltinnApp => "AltinnApp",
            ResourceRegistryResourceType.MaskinportenSchema => "MaskinportenSchema",
            ResourceRegistryResourceType.Systemresource => "SystemResource",
            ResourceRegistryResourceType.GenericAccessResource => "GenericAccessResource",
            ResourceRegistryResourceType.Altinn2Service => "Altinn2Service",
            ResourceRegistryResourceType.BrokerService => "BrokerService",
            ResourceRegistryResourceType.CorrespondenceService => "CorrespondenceService",
            ResourceRegistryResourceType.Consent => "Consent",
            _ => throw new ArgumentOutOfRangeException(nameof(resourceType), $"Not expected resource type value: {resourceType}"),
        };
    }
}
