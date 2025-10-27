using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Enums;
using Altinn.AccessManagement.Core.Helpers.Extensions;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Repositories.Interfaces;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;

namespace Altinn.AccessManagement.Core.Services;

/// <inheritdoc/>
public class AuthorizedPartiesServiceEf(
    IEntityService entityService,
    IConnectionService connectionService,
    IAssignmentService assignmentService,
    IAltinnRolesClient altinnRolesClient,
    IDelegationMetadataRepository resourceDelegationRepository,
    IContextRetrievalService contextRetrievalService) : IAuthorizedPartiesService
{
    /// <inheritdoc/>
    public async Task<List<AuthorizedParty>> GetAuthorizedParties(BaseAttribute subjectAttribute, bool includeAltinn2, bool includeAltinn3 = true, CancellationToken cancellationToken = default) => subjectAttribute.Type switch
    {
        AltinnXacmlConstants.MatchAttributeIdentifiers.PartyUuidAttribute => await GetAuthorizedPartiesByPartyUuid(subjectAttribute.Value, includeAltinn2, includeAltinn3, cancellationToken),
        AltinnXacmlConstants.MatchAttributeIdentifiers.PartyAttribute => await GetAuthorizedPartiesByPartyId(subjectAttribute.Value, includeAltinn2, includeAltinn3, cancellationToken),
        AltinnXacmlConstants.MatchAttributeIdentifiers.UserAttribute => await GetAuthorizedPartiesByUserId(subjectAttribute.Value, includeAltinn2, includeAltinn3, cancellationToken),
        AltinnXacmlConstants.MatchAttributeIdentifiers.PersonId => await GetAuthorizedPartiesByPersonId(subjectAttribute.Value, includeAltinn2, includeAltinn3, cancellationToken),
        AltinnXacmlConstants.MatchAttributeIdentifiers.PersonUuid => await GetAuthorizedPartiesByPartyUuid(subjectAttribute.Value, includeAltinn2, includeAltinn3, cancellationToken),
        AltinnXacmlConstants.MatchAttributeIdentifiers.OrganizationId => await GetAuthorizedPartiesByOrganizationId(subjectAttribute.Value, includeAltinn2, includeAltinn3, cancellationToken),
        AltinnXacmlConstants.MatchAttributeIdentifiers.OrganizationUuid => await GetAuthorizedPartiesByPartyUuid(subjectAttribute.Value, includeAltinn2, includeAltinn3, cancellationToken),
        AltinnXacmlConstants.MatchAttributeIdentifiers.SystemUserUuid => await GetAuthorizedPartiesBySystemUserUuid(subjectAttribute.Value, cancellationToken),
        AltinnXacmlConstants.MatchAttributeIdentifiers.EnterpriseUserName => await GetAuthorizedPartiesByEnterpriseUsername(subjectAttribute.Value, includeAltinn2, includeAltinn3, cancellationToken),
        AltinnXacmlConstants.MatchAttributeIdentifiers.EnterpriseUserUuid => await GetAuthorizedPartiesByPartyUuid(subjectAttribute.Value, includeAltinn2, includeAltinn3, cancellationToken),
        _ => throw new ArgumentException(message: $"Unknown attribute type: {subjectAttribute.Type}", paramName: nameof(subjectAttribute))
    };

    public async Task<List<AuthorizedParty>> GetAuthorizedParties(Entity subject, bool includeAltinn2, bool includeAltinn3 = true, CancellationToken cancellationToken = default)
    {
        if (subject == null)
        {
            return await Task.FromResult(new List<AuthorizedParty>());
        }

        switch (subject.TypeId)
        {
            case var id when id == EntityTypeConstants.Person.Id:

                // Persons can have key roles for other parties, meaning they inherit access to others via these parties.
                var keyRoleAssignments = await assignmentService.GetKeyRoleAssignments(subject.Id, cancellationToken);
                List<Entity> keyRoleEntities = keyRoleAssignments.Select(t => t.From).GroupBy(e => e.Id).Select(g => g.First()).ToList();

                // Also get any sub-units of key role entities
                if (keyRoleEntities.Count > 0)
                {
                    var subUnits = await entityService.GetChildren(keyRoleAssignments.Select(t => t.From.Id), cancellationToken);
                    keyRoleEntities.AddRange(subUnits);
                }

                return await GetAuthorizedParties(subject, keyRoleEntities, includeAltinn2, includeAltinn3, cancellationToken);

            case var id when id == EntityTypeConstants.EnterpriseUser.Id:

                // Enterprise user can also have key role (ECKeyRole) for their organization. Will still need to get these via SBL Bridge until A2-role import is complete.
                IEnumerable<Entity> ecKeyRoleEntities = [];
                if (subject.UserId.HasValue)
                {
                    // A2 lookup of key role parties includes subunits by default
                    List<int> keyRolePartyIds = await contextRetrievalService.GetKeyRolePartyIds(subject.UserId.Value, cancellationToken);
                    ecKeyRoleEntities = await entityService.GetEntitiesByPartyIds(keyRolePartyIds, cancellationToken);
                }

                return await GetAuthorizedParties(subject, ecKeyRoleEntities, includeAltinn2, includeAltinn3, cancellationToken);

            case var id when id == EntityTypeConstants.Organisation.Id:

                // Organizations can not have Altinn 2 roles, only Altinn 3 delegations.
                return await GetAuthorizedParties(subject, null, includeAltinn2: false, includeAltinn3, cancellationToken);

            case var id when id == EntityTypeConstants.SystemUser.Id:

                // System users can not have Altinn 2 roles, only Altinn 3 delegations.
                return await GetAuthorizedParties(subject, null, includeAltinn2: false, includeAltinn3, cancellationToken);

            case var id when id == EntityTypeConstants.SelfIdentified.Id:

                // SelfIdentified users can only have Altinn 2 roles (for themselves) for now.
                return await GetAuthorizedParties(subject, null, includeAltinn2, includeAltinn3: false, cancellationToken);

            default:
                throw new ArgumentException(message: $"Unknown party type: {subject.Type.Name}", paramName: nameof(subject));
        }
    }

    public async Task<List<AuthorizedParty>> GetAuthorizedPartiesByPartyUuid(string subjectPartyUuid, bool includeAltinn2, bool includeAltinn3, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(subjectPartyUuid, out Guid partyUuid))
        {
            throw new ArgumentException(message: $"Not a well-formed uuid: {subjectPartyUuid}", paramName: nameof(subjectPartyUuid));
        }

        var subject = await entityService.GetEntity(partyUuid, cancellationToken);
        return await GetAuthorizedParties(subject, includeAltinn2, includeAltinn3, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<AuthorizedParty>> GetAuthorizedPartiesByPartyId(int subjectPartyId, bool includeAltinn2, bool includeAltinn3, CancellationToken cancellationToken)
    {
        var subject = await entityService.GetByPartyId(subjectPartyId, cancellationToken);
        return await GetAuthorizedParties(subject, includeAltinn2, includeAltinn3: true, cancellationToken);
    }

    public async Task<List<AuthorizedParty>> GetAuthorizedPartiesByPartyId(string subjectPartyId, bool includeAltinn2, bool includeAltinn3, CancellationToken cancellationToken)
    {
        if (!int.TryParse(subjectPartyId, out int partyId))
        {
            throw new ArgumentException(message: $"Not a valid integer: {subjectPartyId}", paramName: nameof(subjectPartyId));
        }

        return await GetAuthorizedPartiesByPartyId(partyId, includeAltinn2, includeAltinn3, cancellationToken);
    }

    public async Task<List<AuthorizedParty>> GetAuthorizedPartiesByUserId(string subjectUserId, bool includeAltinn2, bool includeAltinn3, CancellationToken cancellationToken)
    {
        if (!int.TryParse(subjectUserId, out int userId))
        {
            throw new ArgumentException(message: $"Not a valid integer: {subjectUserId}", paramName: nameof(subjectUserId));
        }

        return await GetAuthorizedPartiesByUserId(userId, includeAltinn2, includeAltinn3, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<AuthorizedParty>> GetAuthorizedPartiesByUserId(int subjectUserId, bool includeAltinn2, bool includeAltinn3, CancellationToken cancellationToken)
    {
        var subject = await entityService.GetByUserId(subjectUserId, cancellationToken);
        return await GetAuthorizedParties(subject, includeAltinn2, includeAltinn3, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<AuthorizedParty>> GetAuthorizedPartiesByPersonId(string subjectNationalId, bool includeAltinn2, bool includeAltinn3, CancellationToken cancellationToken)
    {
        var subject = await entityService.GetByPersNo(subjectNationalId, cancellationToken);
        return await GetAuthorizedParties(subject, includeAltinn2, includeAltinn3, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<AuthorizedParty>> GetAuthorizedPartiesByPersonUuid(string subjectPersonUuid, bool includeAltinn2, bool includeAltinn3, CancellationToken cancellationToken)
    {
        return await GetAuthorizedPartiesByPartyUuid(subjectPersonUuid, includeAltinn2, includeAltinn3, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<AuthorizedParty>> GetAuthorizedPartiesByOrganizationId(string subjectOrganizationNumber, bool includeAltinn2, bool includeAltinn3, CancellationToken cancellationToken)
    {
        var subject = await entityService.GetByOrgNo(subjectOrganizationNumber, cancellationToken);
        return await GetAuthorizedParties(subject, includeAltinn2, includeAltinn3, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<AuthorizedParty>> GetAuthorizedPartiesByOrganizationUuid(string subjectOrganizationUuid, bool includeAltinn2, bool includeAltinn3, CancellationToken cancellationToken)
    {
        return await GetAuthorizedPartiesByPartyUuid(subjectOrganizationUuid, includeAltinn2, includeAltinn3, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<AuthorizedParty>> GetAuthorizedPartiesByEnterpriseUsername(string subjectEnterpriseUsername, bool includeAltinn2, bool includeAltinn3, CancellationToken cancellationToken)
    {
        var subject = await entityService.GetByUsername(subjectEnterpriseUsername, cancellationToken);
        return await GetAuthorizedParties(subject, includeAltinn2, includeAltinn3, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<AuthorizedParty>> GetAuthorizedPartiesByEnterpriseUserUuid(string subjectEnterpriseUserUuid, bool includeAltinn2, bool includeAltinn3, CancellationToken cancellationToken)
    {
        return await GetAuthorizedPartiesByPartyUuid(subjectEnterpriseUserUuid, includeAltinn2, includeAltinn3, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<AuthorizedParty>> GetAuthorizedPartiesBySystemUserUuid(string subjectSystemUserUuid, CancellationToken cancellationToken)
    {
        return await GetAuthorizedPartiesByPartyUuid(subjectSystemUserUuid, includeAltinn2: false, includeAltinn3: true, cancellationToken);
    }

    private async Task<List<AuthorizedParty>> GetAuthorizedParties(Entity userSubject, IEnumerable<Entity> orgSubjectParties = null, bool includeAltinn2 = true, bool includeAltinn3 = true, CancellationToken cancellationToken = default)
    {
        IEnumerable<AuthorizedParty> a2AuthorizedParties = [];
        if (includeAltinn2 && userSubject.UserId.HasValue)
        {
            a2AuthorizedParties = await altinnRolesClient.GetAuthorizedPartiesWithRoles(userSubject.UserId.Value, cancellationToken);

            if (!includeAltinn3)
            {
                return a2AuthorizedParties.ToList();
            }
        }

        IEnumerable<AuthorizedParty> a3AuthorizedParties = null;
        Dictionary<Guid, AuthorizedParty> allA3Parties = null;
        if (includeAltinn3)
        {
            (allA3Parties, a3AuthorizedParties) = await GetAltinn3AuthorizedParties(userSubject.Id, orgSubjectParties?.Select(p => p.Id).ToList(), cancellationToken);

            if (!includeAltinn2)
            {
                return a3AuthorizedParties.ToList();
            }
        }

        return MergeAuthorizePartyLists(a2AuthorizedParties, a3AuthorizedParties, allA3Parties).ToList();
    }

    private IEnumerable<AuthorizedParty> MergeAuthorizePartyLists(IEnumerable<AuthorizedParty> a2AuthorizedParties, IEnumerable<AuthorizedParty> a3AuthorizedParties, Dictionary<Guid, AuthorizedParty> allParties)
    {
        List<AuthorizedParty> result = a3AuthorizedParties.ToList();

        // ToDo: Merge Altinn 2 authorized parties with Altinn 3 authorized parties, ensuring no duplicates and enrich model with AuthorizedRoles from Altinn 2
        foreach (AuthorizedParty a2Party in a2AuthorizedParties)
        {
            if (allParties.TryGetValue(a2Party.PartyUuid, out AuthorizedParty existingA3Party))
            {
                // Merge roles from Altinn 2 into existing Altinn 3 party
                existingA3Party.AuthorizedRoles = a2Party.AuthorizedRoles;
                existingA3Party.OnlyHierarchyElementWithNoAccess = false;

                foreach (AuthorizedParty a2SubUnit in a2Party.Subunits)
                {
                    if (allParties.TryGetValue(a2SubUnit.PartyUuid, out AuthorizedParty existingSubUnit))
                    {
                        // Merge roles from Altinn 2 into existing Altinn 3 subunit
                        existingSubUnit.AuthorizedRoles = a2SubUnit.AuthorizedRoles;
                    }
                    else
                    {
                        // Add new Altinn 2 subunit
                        existingA3Party.Subunits.Add(a2SubUnit);
                        allParties.Add(a2SubUnit.PartyUuid, a2SubUnit);
                    }
                }
            }
            else
            {
                // Add new Altinn 2 party and its subunits
                allParties.Add(a2Party.PartyUuid, a2Party);
                foreach (AuthorizedParty a2SubUnit in a2Party.Subunits)
                {
                    allParties.Add(a2SubUnit.PartyUuid, a2SubUnit);
                }

                result.Add(a2Party);
            }
        }

        return result;
    }

    // ToDo: Can be removed if A2 is merged into A3, instead of A3 into A2
    private Dictionary<Guid, AuthorizedParty> GetDictionaryFromList(IEnumerable<AuthorizedParty> authorizedParties)
    {
        Dictionary<Guid, AuthorizedParty> authorizedPartyDict = [];
        foreach (AuthorizedParty authParty in authorizedParties)
        {
            authorizedPartyDict.Add(authParty.PartyUuid, authParty);
            if (authParty.Subunits != null)
            {
                foreach (AuthorizedParty subunit in authParty.Subunits)
                {
                    // Some bad ER-data exists from A2 where a subunit have multiple parents. Ignore duplicates.
                    if (!authorizedPartyDict.ContainsKey(subunit.PartyUuid))
                    {
                        authorizedPartyDict.Add(subunit.PartyUuid, subunit);
                    }
                }
            }
        }

        return authorizedPartyDict;
    }

    private async Task<Tuple<Dictionary<Guid, AuthorizedParty>, IEnumerable<AuthorizedParty>>> GetAltinn3AuthorizedParties(Guid toId, List<Guid> toOrgs = null, CancellationToken cancellationToken = default)
    {
        // Get AccessPackage Delegations
        var packagePermissions = await connectionService.GetPackagePermissionsFromOthers(toId, null, null, null, cancellationToken: cancellationToken);

        // Get App, Resource and Instance delegations
        List<Guid> allToParties = toOrgs ?? new List<Guid>();
        allToParties.Add(toId);

        List<DelegationChange> resourceDelegations = await resourceDelegationRepository.GetAllDelegationChangesForAuthorizedParties(allToParties, cancellationToken: cancellationToken);
        resourceDelegations = await AddInstanceDelegations(resourceDelegations, allToParties, cancellationToken);

        // Get Party info for all from-uuids
        var fromUuids = resourceDelegations.Where(d => d.FromUuid.HasValue).Select(d => d.FromUuid.Value).ToList();
        fromUuids.AddRange(packagePermissions.SelectMany(p => p.Permissions).Select(p => p.From.Id));
        var fromParties = await entityService.GetEntities(fromUuids.Distinct(), cancellationToken);
        var fromSubUnits = await entityService.GetChildren(fromUuids.Distinct(), cancellationToken);

        (Dictionary<Guid, AuthorizedParty> parties, IEnumerable<AuthorizedParty> authorizedParties) = BuildDictionaryFromEntities(fromParties, fromSubUnits);

        // Enrich AuthorizedParties with all authorized AccessPackages, Resources and Instances
        EnrichWithAccessPackageParties(parties, packagePermissions);
        EnrichWithResourceAndInstanceParties(parties, resourceDelegations);

        return Tuple.Create(parties, authorizedParties.AsEnumerable());
    }

    private static Tuple<Dictionary<Guid, AuthorizedParty>, IEnumerable<AuthorizedParty>> BuildDictionaryFromEntities(IEnumerable<Entity> parties, IEnumerable<Entity> subunits)
    {
        Dictionary<Guid, AuthorizedParty> allPartiesDict = new();
        List<AuthorizedParty> authorizedParties = new();

        // Parties list is expected to be distinct parties where "some" access exists
        foreach (var party in parties)
        {
            if (party.ParentId.HasValue)
            {
                var subUnit = BuildAuthorizedPartyFromEntity(party);
                allPartiesDict[party.Id] = subUnit;

                // Either add to existing parent or create parent if not exists
                if (allPartiesDict.TryGetValue(party.ParentId.Value, out AuthorizedParty parent))
                {
                    allPartiesDict[party.ParentId.Value].Subunits.Add(subUnit);
                }
                else
                {
                    allPartiesDict[party.ParentId.Value] = BuildAuthorizedPartyFromEntity(party.Parent, onlyHierarchyElement: true);
                    allPartiesDict[party.ParentId.Value].Subunits.Add(subUnit);
                    authorizedParties.Add(allPartiesDict[party.ParentId.Value]);
                }
            }
            else
            {
                // Either person or top-level organization
                allPartiesDict[party.Id] = BuildAuthorizedPartyFromEntity(party);
                authorizedParties.Add(allPartiesDict[party.Id]);
            }
        }

        // Add all subunits to their top-level organization and to the dictionary
        foreach (var subunit in subunits)
        {
            var subunitAuthParty = BuildAuthorizedPartyFromEntity(subunit);
            if (allPartiesDict.TryGetValue(subunit.ParentId.Value, out AuthorizedParty parent))
            {
                allPartiesDict[subunit.ParentId.Value].Subunits.Add(subunitAuthParty);
            }
            else
            {
                // This should not happen as all subunits are retrieved based on the from parties above.
                Unreachable();
            }

            allPartiesDict[subunit.Id] = subunitAuthParty;
        }

        return Tuple.Create(allPartiesDict, authorizedParties.AsEnumerable());
    }

    private async Task<List<DelegationChange>> AddInstanceDelegations(List<DelegationChange> delegations, List<Guid> subjectPartyIds, CancellationToken cancellationToken)
    {
        if (subjectPartyIds.Count > 0)
        {
            IEnumerable<InstanceDelegationChange> instanceDelegations = await resourceDelegationRepository.GetAllCurrentReceivedInstanceDelegations(subjectPartyIds, cancellationToken);

            foreach (var instanceDelegation in instanceDelegations)
            {
                delegations.Add(new DelegationChange
                {
                    ResourceId = instanceDelegation.ResourceId,
                    InstanceId = instanceDelegation.InstanceId,
                    FromUuidType = instanceDelegation.FromUuidType,
                    FromUuid = instanceDelegation.FromUuid,
                    ToUuidType = instanceDelegation.ToUuidType,
                    ToUuid = instanceDelegation.ToUuid,
                    PerformedByUuidType = instanceDelegation.PerformedByType,
                    PerformedByUuid = instanceDelegation.PerformedBy,
                    DelegationChangeType = instanceDelegation.DelegationChangeType,
                    BlobStoragePolicyPath = instanceDelegation.BlobStoragePolicyPath,
                    BlobStorageVersionId = instanceDelegation.BlobStorageVersionId,
                    Created = instanceDelegation.Created
                });
            }
        }

        return delegations;
    }

    private static void EnrichWithAccessPackageParties(Dictionary<Guid, AuthorizedParty> parties, IEnumerable<PackagePermissionDto> packagePermissions)
    {
        foreach (var packagePermission in packagePermissions)
        {
            foreach (var permission in packagePermission.Permissions)
            {
                if (parties.TryGetValue(permission.From.Id, out AuthorizedParty party))
                {
                    party.EnrichWithAccessPackage(packagePermission.Package.Urn.Split(":").Last().SingleToList());
                }
                else
                {
                    // This should not happen as all parties are retrieved based on the from parties on the delegations
                    Unreachable();
                }
            }
        }
    }

    private static void EnrichWithResourceAndInstanceParties(Dictionary<Guid, AuthorizedParty> parties, List<DelegationChange> resourceDelegations)
    {
        foreach (DelegationChange delegation in resourceDelegations)
        {
            if (parties.TryGetValue(delegation.FromUuid.Value, out AuthorizedParty party))
            {
                if (delegation.InstanceId != null)
                {
                    party.EnrichWithResourceInstanceAccess(delegation.ResourceId, delegation.InstanceId);
                }
                else
                {
                    party.EnrichWithResourceAccess(delegation.ResourceId);
                }
            }
            else
            {
                // This should not happen as all parties are retrieved based on the from parties on the delegations
                Unreachable();
            }
        }
    }

    private static AuthorizedParty BuildAuthorizedPartyFromEntity(Entity entity, bool onlyHierarchyElement = false)
    {
        var party = new AuthorizedParty
        {
            PartyUuid = entity.Id,
            Name = entity.Name,
            IsDeleted = entity.IsDeleted,
            PartyId = entity.PartyId.HasValue ? entity.PartyId.Value : 0
        };

        switch (entity.TypeId)
        {
            case var orgType when orgType == EntityTypeConstants.Organisation.Id:
                party.OrganizationNumber = entity.OrganizationIdentifier;
                party.Type = AuthorizedPartyType.Organization;

                EntityVariantConstants.TryGetById(entity.VariantId, out ConstantDefinition<EntityVariant> variant);
                party.UnitType = variant.Entity.Name;
                party.OnlyHierarchyElementWithNoAccess = onlyHierarchyElement;

                break;
            case var personType when personType == EntityTypeConstants.Person.Id:
                party.PersonId = entity.PersonIdentifier;
                party.Type = AuthorizedPartyType.Person;
                party.DateOfBirth = entity.DateOfBirth;
                break;
            default:
                // Only Organizations and Persons can be represented by others.
                Unreachable();
                break;
        }

        return party;
    }

    [DoesNotReturn]
    private static void Unreachable()
    {
        throw new UnreachableException();
    }
}
