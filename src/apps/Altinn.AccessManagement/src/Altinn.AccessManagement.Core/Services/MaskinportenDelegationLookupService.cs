using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Models.ResourceRegistry;
using Altinn.AccessManagement.Core.Repositories.Interfaces;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Platform.Register.Models;
using Microsoft.EntityFrameworkCore;
using Delegation = Altinn.AccessManagement.Core.Models.Delegation;
using ResourceType = Altinn.AccessManagement.Core.Models.ResourceRegistry.ResourceType;

namespace Altinn.AccessManagement.Core.Services
{
    /// <summary>
    /// Looks up active MaskinportenSchema delegations between organizations for the
    /// Maskinporten delegations proxy endpoint.
    /// </summary>
    public class MaskinportenDelegationLookupService : IMaskinportenDelegationLookupService
    {
        private readonly AppDbContext _db;
        private readonly IDelegationMetadataRepository _delegationRepository;
        private readonly IContextRetrievalService _contextRetrievalService;
        private readonly IResourceAdministrationPoint _resourceAdministrationPoint;

        /// <summary>
        /// Initializes a new instance of the <see cref="MaskinportenDelegationLookupService"/> class.
        /// </summary>
        public MaskinportenDelegationLookupService(
            AppDbContext dbContext,
            IDelegationMetadataRepository delegationRepository,
            IContextRetrievalService contextRetrievalService,
            IResourceAdministrationPoint resourceAdministrationPoint)
        {
            _db = dbContext;
            _delegationRepository = delegationRepository;
            _contextRetrievalService = contextRetrievalService;
            _resourceAdministrationPoint = resourceAdministrationPoint;
        }

        /// <inheritdoc/>
        public async Task<List<Delegation>> GetMaskinportenDelegations(string? supplierOrg, string? consumerOrg, string? scope, CancellationToken cancellationToken = default)
        {
            int consumerPartyId = 0;
            Entity consumerParty = null;
            if (!string.IsNullOrEmpty(consumerOrg))
            {
                consumerParty = await _db.Entities
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.OrganizationIdentifier == consumerOrg, cancellationToken);

                if (consumerParty is null)
                {
                    throw new ArgumentException($"The specified consumerOrg: {consumerOrg}, is not a valid organization number", nameof(consumerOrg));
                }

                if (consumerParty.PartyId is { } partyId && partyId > 0)
                {
                    consumerPartyId = partyId;
                }
                else
                {
                    throw new ArgumentException($"The specified consumerOrg: {consumerOrg}, is not associated with a valid party", nameof(consumerOrg));
                }
            }

            int supplierPartyId = 0;
            Entity supplierEntity = null;
            if (!string.IsNullOrEmpty(supplierOrg))
            {
                supplierEntity = await _db.Entities
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.OrganizationIdentifier == supplierOrg, cancellationToken);

                if (supplierEntity is null)
                {
                    throw new ArgumentException($"The specified supplierOrg: {supplierOrg}, is not a valid organization number", nameof(supplierOrg));
                }

                if (supplierEntity.PartyId is { } partyId && partyId > 0)
                {
                    supplierPartyId = partyId;
                }
                else
                {
                    throw new ArgumentException($"The specified supplierOrg: {supplierOrg}, is not associated with a valid party", nameof(supplierOrg));
                }
            }

            return await GetAllMaskinportenSchemaDelegations(supplierPartyId, supplierEntity?.Id, consumerPartyId, consumerParty?.Id, scope, cancellationToken);
        }

        private async Task<List<Delegation>> GetAllMaskinportenSchemaDelegations(int supplierPartyId, Guid? supplierPartyUuid, int consumerPartyId, Guid? consumerPartyUuid, string scopes, CancellationToken cancellationToken = default)
        {
            List<Delegation> delegations = new List<Delegation>();

            IEnumerable<ServiceResource> resources = await _resourceAdministrationPoint.GetResources(scopes, cancellationToken);
            if (!resources.Any())
            {
                return delegations;
            }

            List<DelegationChange> delegationChanges = await _delegationRepository.GetResourceRegistryDelegationChanges(resources.Select(d => d.Identifier).ToList(), consumerPartyUuid, supplierPartyUuid, ResourceType.MaskinportenSchema, cancellationToken);
            if (delegationChanges.Count == 0)
            {
                return delegations;
            }

            return await BuildDelegationsResponseUsingUuids(delegationChanges, resources, cancellationToken);
        }

        private async Task<List<Delegation>> BuildDelegationsResponseUsingUuids(List<DelegationChange> delegationChanges, IEnumerable<ServiceResource> resources, CancellationToken cancellationToken)
        {
            List<Delegation> delegations = new List<Delegation>();
            List<Guid> parties = delegationChanges.Select(d => (Guid)d.FromUuid).ToList();
            parties.AddRange(delegationChanges.Select(d => (Guid)d.ToUuid).ToList());

            var partyList = await _contextRetrievalService.GetPartiesByUuids(parties, cancellationToken: cancellationToken);

            foreach (DelegationChange delegationChange in delegationChanges)
            {
                Party offeredByParty = partyList[delegationChange.FromUuid.ToString()];
                Party coveredByParty = partyList[delegationChange.ToUuid.ToString()];
                ServiceResource resource = resources?.FirstOrDefault(r => r.Identifier == delegationChange.ResourceId);
                delegations.Add(BuildDelegationModel(delegationChange, offeredByParty, coveredByParty, resource));
            }

            return delegations;
        }

        private static Delegation BuildDelegationModel(DelegationChange delegationChange, Party offeredByParty, Party coveredByParty, ServiceResource resource)
        {
            ResourceType resourceType = Enum.TryParse(delegationChange.ResourceType, true, out ResourceType type) ? type : ResourceType.Default;
            Delegation delegation = new Delegation
            {
                OfferedByPartyId = offeredByParty.PartyId,
                OfferedByName = offeredByParty?.Name,
                OfferedByOrganizationNumber = offeredByParty?.OrgNumber,
                CoveredByPartyId = coveredByParty.PartyId,
                CoveredByName = coveredByParty?.Name,
                CoveredByOrganizationNumber = coveredByParty?.OrgNumber,
                PerformedByUserId = delegationChange.PerformedByUserId,
                PerformedByPartyId = delegationChange.PerformedByPartyId,
                Created = delegationChange.Created ?? DateTime.MinValue,
                ResourceId = delegationChange.ResourceId,
                ResourceType = resourceType
            };

            if (resource != null)
            {
                delegation.ResourceTitle = resource?.Title;
                delegation.Description = resource?.Description;
                delegation.RightDescription = resource?.RightDescription;
                delegation.ResourceReferences = resource?.ResourceReferences;
                delegation.HasCompetentAuthority = resource?.HasCompetentAuthority;
            }

            return delegation;
        }
    }
}
