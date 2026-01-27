using Altinn.AccessManagement.Core.Enums;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Repositories.Interfaces;
using Altinn.AccessManagement.Enums;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.Core.Services.Legacy;

/// <inheritdoc/>
public class DelegationMetadataEF : IDelegationMetadataRepository
{
    private string ConvertFromAppResourceId(string resourceId)
    {
        // TODO: Verify
        //// app_skd_skattemelding => skd/skattemelding
        return resourceId.Replace("app_", string.Empty).Replace('_', '/');
    }

    private string ConvertToAppResourceId(Resource resource)
    {
        // TODO: Verify
        //// skattemelding => app_skd_skattemelding
        return $"app_{resource.Provider.Code}_{resource.RefId}";
    }

    private DelegationChange Convert(AssignmentResource assignmentResource)
    {
        return new DelegationChange()
        {
            DelegationChangeId = assignmentResource.DelegationChangeId,
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

            ToUuid = assignmentResource.Assignment.ToId,
            ToUuidType = ConvertEntityTypeToUuidType(assignmentResource.Assignment.To.TypeId),
            CoveredByPartyId = assignmentResource.Assignment.To.PartyId,
            CoveredByUserId = assignmentResource.Assignment.To.UserId,
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
            .Include(t => t.Resource).ThenInclude(t => t.Provider)
            .Where(t => t.Id == id)
            .SingleAsync(t => t.Id == id)
            );
    }

    private async Task<InstanceDelegationChange> GetAssignmentInstance(Guid id)
    {
        return Convert(await DbContext.AssignmentInstances
            .Include(t => t.Assignment).ThenInclude(t => t.From)
            .Include(t => t.Assignment).ThenInclude(t => t.To)
            .Include(t => t.Resource).ThenInclude(t => t.Provider)
            .Where(t => t.Id == id)
            .SingleAsync(t => t.Id == id)
            );
    }

    private async Task<Resource> GetResource(string resourceIdentifier, CancellationToken cancellationToken = default)
    {
        return await DbContext.Resources.AsNoTracking().SingleAsync(t => t.RefId == resourceIdentifier, cancellationToken);
    }

    public AppDbContext DbContext { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DelegationMetadataEF"/> class
    /// </summary>
    /// <param name="dbContext">AppDbContext</param>
    public DelegationMetadataEF(AppDbContext dbContext)
    {
        DbContext = dbContext;
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

        var result = await DbContext.AssignmentResources.AsNoTracking()
            .Include(t => t.Assignment).ThenInclude(t => t.From)
            .Include(t => t.Assignment).ThenInclude(t => t.To)
            .Include(t => t.Resource)
            .Where(t => altinnAppIds.Contains(t.Resource.RefId))
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

        var resourceUuids = altinnAppIds.Select(t => Guid.Parse(t));

        var result = await DbContext.AssignmentResources.AsNoTracking()
            .Include(t => t.Assignment).ThenInclude(t => t.From)
            .Include(t => t.Assignment).ThenInclude(t => t.To)
            .Where(t => resourceUuids.Contains(t.ResourceId))
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

        var resourceUuid = Guid.Parse(resourceId);

        var from = await DbContext.Entities.AsNoTracking().SingleAsync(t => t.PartyId == offeredByPartyId, cancellationToken);

        var result = await DbContext.AssignmentResources.AsNoTracking()
            .Include(t => t.Assignment).ThenInclude(t => t.To)
            .Where(t => t.ResourceId == resourceUuid)
            .Where(t => t.Assignment.FromId == from.Id)
            .WhereIf(coveredByPartyId != null, t => t.Assignment.To.PartyId == coveredByPartyId)
            .WhereIf(coveredByPartyId != null, t => t.Assignment.To.UserId == coveredByUserId)
            .WhereIf(toUuid.HasValue, t => t.Assignment.ToId == toUuid.Value)
            .SingleAsync(cancellationToken);

        return Convert(result);
    }

    /// <inheritdoc/>
    public async Task<DelegationChange> InsertDelegation(ResourceAttributeMatchType resourceMatchType, DelegationChange delegationChange, CancellationToken cancellationToken = default)
    {
        var role = RoleConstants.Rightholder;
        var from = await DbContext.Entities.AsNoTracking().SingleAsync(t => t.PartyId == delegationChange.OfferedByPartyId, cancellationToken);
        var to = await DbContext.Entities.AsNoTracking().SingleAsync(t => t.PartyId == delegationChange.CoveredByPartyId, cancellationToken);
        var resource = await DbContext.Resources.AsNoTracking().SingleAsync(t => t.RefId == delegationChange.ResourceId, cancellationToken);

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
        }

        var assignmentResource = await DbContext.AssignmentResources.FirstOrDefaultAsync(t => t.AssignmentId == assignment.Id && t.ResourceId == resource.Id, cancellationToken);
        if (assignmentResource == null)
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
        }

        if (delegationChange.DelegationChangeType == DelegationChangeType.RevokeLast)
        {
            DbContext.AssignmentResources.Remove(assignmentResource);
        }

        await DbContext.SaveChangesAsync();

        return await GetAssignmentResource(assignmentResource.Id);
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
          .Include(t => t.Resource)
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
           .Include(t => t.Resource)

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
        }

        var assignmentInstance = await DbContext.AssignmentInstances.FirstOrDefaultAsync(t => t.AssignmentId == assignment.Id && t.ResourceId == resource.Id && t.InstanceId == instanceDelegationChange.InstanceId, cancellationToken);
        if (assignmentInstance == null)
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

        if (instanceDelegationChange.DelegationChangeType == DelegationChangeType.RevokeLast)
        {
            DbContext.AssignmentInstances.Remove(assignmentInstance);
        }

        await DbContext.SaveChangesAsync();

        return await GetAssignmentInstance(assignmentInstance.Id);
    }

    /// <inheritdoc />
    public async Task<bool> InsertMultipleInstanceDelegations(List<PolicyWriteOutput> policyWriteOutputs, CancellationToken cancellationToken = default)
    {
        try
        {
            var role = RoleConstants.Rightholder;

            foreach (var policy in policyWriteOutputs)
            {
                var resource = await GetResource(policy.Rules.ResourceId, cancellationToken);
                var assignment = await DbContext.Assignments.FirstOrDefaultAsync(t => t.FromId == policy.Rules.FromUuid && t.ToId == policy.Rules.ToUuid && t.RoleId == role.Id, cancellationToken);
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

            await DbContext.SaveChangesAsync();
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
           .Include(t => t.Resource)

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
           .Include(t => t.Resource)
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

        if (coveredByPartyIds?.Count > 0)
        {
            delegationChanges.AddRange(await GetReceivedResourceRegistryDelegationsForCoveredByPartys(coveredByPartyIds, offeredByPartyIds, resourceRegistryIds, cancellationToken: cancellationToken));
        }

        if (coveredByUserId.HasValue)
        {
            delegationChanges.AddRange(await GetReceivedResourceRegistryDelegationsForCoveredByUser(coveredByUserId.Value, offeredByPartyIds, resourceRegistryIds, cancellationToken: cancellationToken));
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
    public async Task<List<DelegationChange>> GetOfferedResourceRegistryDelegations(int offeredByPartyId, List<string> resourceRegistryIds = null, List<AccessManagement.Core.Models.ResourceRegistry.ResourceType> resourceTypes = null, CancellationToken cancellationToken = default)
    {
        var result = await DbContext.AssignmentResources.AsNoTracking()
           .Include(t => t.Assignment).ThenInclude(t => t.From)
           .Include(t => t.Assignment).ThenInclude(t => t.To)
           .Include(t => t.Resource)
           .Where(t => t.Assignment.From.PartyId.HasValue && t.Assignment.From.PartyId.Value == offeredByPartyId)
           .WhereIf(resourceRegistryIds != null && resourceRegistryIds.Any(), t => resourceRegistryIds.Contains(t.Resource.RefId))
           .ToListAsync(cancellationToken);

        return result.Select(Convert).ToList();
    }

    /// <inheritdoc/>
    public async Task<List<DelegationChange>> GetReceivedResourceRegistryDelegationsForCoveredByPartys(List<int> coveredByPartyIds, List<int> offeredByPartyIds = null, List<string> resourceRegistryIds = null, List<AccessManagement.Core.Models.ResourceRegistry.ResourceType> resourceTypes = null, CancellationToken cancellationToken = default)
    {
        var result = await DbContext.AssignmentResources.AsNoTracking()
           .Include(t => t.Assignment).ThenInclude(t => t.From)
           .Include(t => t.Assignment).ThenInclude(t => t.To)
           .Include(t => t.Resource)
           .Where(t => t.Assignment.To.PartyId.HasValue && coveredByPartyIds.Contains(t.Assignment.To.PartyId.Value))
           .WhereIf(resourceRegistryIds != null && resourceRegistryIds.Any(), t => resourceRegistryIds.Contains(t.Resource.RefId))
           .WhereIf(offeredByPartyIds != null && offeredByPartyIds.Any(), t => t.Assignment.From.PartyId.HasValue && offeredByPartyIds.Contains(t.Assignment.From.PartyId.Value))
           .ToListAsync(cancellationToken);

        return result.Select(Convert).ToList();
    }

    /// <inheritdoc/>
    public async Task<List<DelegationChange>> GetReceivedResourceRegistryDelegationsForCoveredByUser(int coveredByUserId, List<int> offeredByPartyIds, List<string> resourceRegistryIds = null, List<AccessManagement.Core.Models.ResourceRegistry.ResourceType> resourceTypes = null, CancellationToken cancellationToken = default)
    {
        var result = await DbContext.AssignmentResources.AsNoTracking()
           .Include(t => t.Assignment).ThenInclude(t => t.From)
           .Include(t => t.Assignment).ThenInclude(t => t.To)
           .Include(t => t.Resource)
           .Where(t => t.Assignment.To.UserId.HasValue && t.Assignment.To.UserId.Value == coveredByUserId)
           .Where(t => t.Assignment.From.PartyId.HasValue && offeredByPartyIds.Contains(t.Assignment.From.PartyId.Value))
           .WhereIf(resourceRegistryIds != null && resourceRegistryIds.Any(), t => resourceRegistryIds.Contains(t.Resource.RefId))
           .ToListAsync(cancellationToken);

        return result.Select(Convert).ToList();
    }

    /// <inheritdoc/>
    public async Task<List<DelegationChange>> GetResourceRegistryDelegationChanges(List<string> resourceIds, int offeredByPartyId, int coveredByPartyId, AccessManagement.Core.Models.ResourceRegistry.ResourceType resourceType, CancellationToken cancellationToken = default)
    {
        var result = await DbContext.AssignmentResources.AsNoTracking()
           .Include(t => t.Assignment).ThenInclude(t => t.From)
           .Include(t => t.Assignment).ThenInclude(t => t.To)
           .Include(t => t.Resource)
           .Where(t => t.Assignment.From.PartyId.HasValue && t.Assignment.From.PartyId.Value == offeredByPartyId)
           .Where(t => t.Assignment.To.PartyId.HasValue && t.Assignment.To.PartyId.Value == coveredByPartyId)
           .Where(t => resourceIds.Contains(t.Resource.RefId))
           .ToListAsync(cancellationToken);

        return result.Select(Convert).ToList();
    }

    /// <inheritdoc/>
    public async Task<List<DelegationChange>> GetOfferedDelegations(List<int> offeredByPartyIds, CancellationToken cancellationToken = default)
    {
        var result = await DbContext.AssignmentResources.AsNoTracking()
           .Include(t => t.Assignment).ThenInclude(t => t.From)
           .Include(t => t.Assignment).ThenInclude(t => t.To)
           .Include(t => t.Resource)
           .Where(t => t.Assignment.From.PartyId.HasValue && offeredByPartyIds.Contains(t.Assignment.From.PartyId.Value))
           .ToListAsync(cancellationToken);

        return result.Select(Convert).ToList();
    }

    /// <inheritdoc/>
    public async Task<List<DelegationChange>> GetAllDelegationChangesForAuthorizedParties(List<int> coveredByUserIds, List<int> coveredByPartyIds, CancellationToken cancellationToken = default)
    {
        var partyChanges = DbContext.AssignmentResources.AsNoTracking()
          .Include(t => t.Assignment).ThenInclude(t => t.From)
          .Include(t => t.Assignment).ThenInclude(t => t.To)
          .Include(t => t.Resource)
          .Where(t => t.Assignment.To.PartyId.HasValue && coveredByPartyIds.Contains(t.Assignment.To.PartyId.Value));

        var userChanges = DbContext.AssignmentResources.AsNoTracking()
          .Include(t => t.Assignment).ThenInclude(t => t.From)
          .Include(t => t.Assignment).ThenInclude(t => t.To)
          .Include(t => t.Resource)
          .Where(t => t.Assignment.To.UserId.HasValue && coveredByUserIds.Contains(t.Assignment.To.UserId.Value));

        var result = await partyChanges.Union(userChanges).ToListAsync(cancellationToken);

        return result.Select(Convert).ToList();
    }

    /// <inheritdoc/>
    public async Task<List<DelegationChange>> GetAllDelegationChangesForAuthorizedParties(List<Guid> toPartyUuids, CancellationToken cancellationToken = default)
    {
        var result = await DbContext.AssignmentResources.AsNoTracking()
          .Include(t => t.Assignment).ThenInclude(t => t.From)
          .Include(t => t.Assignment).ThenInclude(t => t.To)
          .Include(t => t.Resource)
          .Where(t => toPartyUuids.Contains(t.Assignment.ToId))
          .ToListAsync(cancellationToken);

        return result.Select(Convert).ToList();
    }
}
