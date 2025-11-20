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
using Altinn.AccessMgmt.PersistenceEF.Queries.Connection.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;

namespace Altinn.AccessManagement.Core.Services;

/// <inheritdoc/>
public class AuthorizedPartiesServiceEf(
    IAltinnRolesClient altinnRolesClient,
    IDelegationMetadataRepository resourceDelegationRepository,
    IContextRetrievalService contextRetrievalService,
    IAuthorizedPartyRepoServiceEf repoService) : IAuthorizedPartiesService
{
    /// <inheritdoc/>
    public async Task<List<AuthorizedParty>> GetAuthorizedParties(BaseAttribute subjectAttribute, AuthorizedPartiesFilters filter, CancellationToken cancellationToken = default) => subjectAttribute.Type switch
    {
        AltinnXacmlConstants.MatchAttributeIdentifiers.PartyUuidAttribute => await GetAuthorizedPartiesByPartyUuid(subjectAttribute.Value, filter, cancellationToken),
        AltinnXacmlConstants.MatchAttributeIdentifiers.PartyAttribute => await GetAuthorizedPartiesByPartyId(subjectAttribute.Value, filter, cancellationToken),
        AltinnXacmlConstants.MatchAttributeIdentifiers.UserAttribute => await GetAuthorizedPartiesByUserId(subjectAttribute.Value, filter, cancellationToken),
        AltinnXacmlConstants.MatchAttributeIdentifiers.PersonId => await GetAuthorizedPartiesByPersonId(subjectAttribute.Value, filter, cancellationToken),
        AltinnXacmlConstants.MatchAttributeIdentifiers.PersonUuid => await GetAuthorizedPartiesByPartyUuid(subjectAttribute.Value, filter, cancellationToken),
        AltinnXacmlConstants.MatchAttributeIdentifiers.OrganizationId => await GetAuthorizedPartiesByOrganizationId(subjectAttribute.Value, filter, cancellationToken),
        AltinnXacmlConstants.MatchAttributeIdentifiers.OrganizationUuid => await GetAuthorizedPartiesByPartyUuid(subjectAttribute.Value, filter, cancellationToken),
        AltinnXacmlConstants.MatchAttributeIdentifiers.SystemUserUuid => await GetAuthorizedPartiesBySystemUserUuid(subjectAttribute.Value, filter, cancellationToken),
        AltinnXacmlConstants.MatchAttributeIdentifiers.EnterpriseUserName => await GetAuthorizedPartiesByEnterpriseUsername(subjectAttribute.Value, filter, cancellationToken),
        AltinnXacmlConstants.MatchAttributeIdentifiers.EnterpriseUserUuid => await GetAuthorizedPartiesByPartyUuid(subjectAttribute.Value, filter, cancellationToken),
        _ => throw new ArgumentException(message: $"Unknown attribute type: {subjectAttribute.Type}", paramName: nameof(subjectAttribute))
    };

    public async Task<List<AuthorizedParty>> GetAuthorizedParties(Entity subject, AuthorizedPartiesFilters filter, CancellationToken cancellationToken = default)
    {
        if (subject == null)
        {
            return await Task.FromResult(new List<AuthorizedParty>());
        }

        switch (subject.TypeId)
        {
            case var id when id == EntityTypeConstants.Person.Id:

                // Persons can have key roles for other parties, meaning they inherit access to others via these parties.
                var keyRoleAssignments = await repoService.GetKeyRoleAssignments(subject.Id, cancellationToken);
                List<Guid> keyRoleEntities = keyRoleAssignments.Select(t => t.FromId).Distinct().ToList();

                // Also get any sub-units of key role entities
                if (keyRoleEntities.Count > 0)
                {
                    var subUnits = await repoService.GetSubunits(keyRoleEntities, cancellationToken);
                    keyRoleEntities.AddRange(subUnits.Select(t => t.Id));
                }

                return await GetAuthorizedParties(filter, subject, keyRoleEntities, cancellationToken);

            case var id when id == EntityTypeConstants.EnterpriseUser.Id:

                // Enterprise user can also have key role (ECKeyRole) for their organization. Will still need to get these via SBL Bridge until A2-role import is complete.
                IEnumerable<Entity> ecKeyRoleEntities = [];
                if (subject.UserId.HasValue)
                {
                    // A2 lookup of key role parties includes subunits by default
                    List<int> keyRolePartyIds = await contextRetrievalService.GetKeyRolePartyIds(subject.UserId.Value, cancellationToken);
                    ecKeyRoleEntities = await repoService.GetEntitiesByPartyIds(keyRolePartyIds, cancellationToken);
                }

                return await GetAuthorizedParties(filter, subject, ecKeyRoleEntities.Select(t => t.Id), cancellationToken);

            case var id when id == EntityTypeConstants.Organisation.Id:

                // Organizations can not have Altinn 2 roles, only Altinn 3 delegations.
                filter.IncludeAltinn2 = false;
                return await GetAuthorizedParties(filter, subject, null, cancellationToken);

            case var id when id == EntityTypeConstants.SystemUser.Id:

                // System users can not have Altinn 2 roles, only Altinn 3 delegations.
                filter.IncludeAltinn2 = false;
                return await GetAuthorizedParties(filter, subject, null, cancellationToken);

            case var id when id == EntityTypeConstants.SelfIdentified.Id:

                // SelfIdentified users can only have Altinn 2 roles (for themselves) for now.
                filter.IncludeAltinn3 = false;
                return await GetAuthorizedParties(filter, subject, null, cancellationToken);

            default:
                throw new ArgumentException(message: $"Unknown party type: {subject.Type.Name}", paramName: nameof(subject));
        }
    }

    public async Task<List<AuthorizedParty>> GetAuthorizedPartiesByPartyUuid(string subjectPartyUuid, AuthorizedPartiesFilters filter, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(subjectPartyUuid, out Guid partyUuid))
        {
            throw new ArgumentException(message: $"Not a well-formed uuid: {subjectPartyUuid}", paramName: nameof(subjectPartyUuid));
        }

        var subject = await repoService.GetEntity(partyUuid, cancellationToken);
        return await GetAuthorizedParties(subject, filter, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<AuthorizedParty>> GetAuthorizedPartiesByPartyId(int subjectPartyId, AuthorizedPartiesFilters filter, CancellationToken cancellationToken)
    {
        var subject = await repoService.GetEntityByPartyId(subjectPartyId, cancellationToken);
        return await GetAuthorizedParties(subject, filter, cancellationToken);
    }

    public async Task<List<AuthorizedParty>> GetAuthorizedPartiesByPartyId(string subjectPartyId, AuthorizedPartiesFilters filter, CancellationToken cancellationToken)
    {
        if (!int.TryParse(subjectPartyId, out int partyId))
        {
            throw new ArgumentException(message: $"Not a valid integer: {subjectPartyId}", paramName: nameof(subjectPartyId));
        }

        return await GetAuthorizedPartiesByPartyId(partyId, filter, cancellationToken);
    }

    public async Task<List<AuthorizedParty>> GetAuthorizedPartiesByUserId(string subjectUserId, AuthorizedPartiesFilters filter, CancellationToken cancellationToken)
    {
        if (!int.TryParse(subjectUserId, out int userId))
        {
            throw new ArgumentException(message: $"Not a valid integer: {subjectUserId}", paramName: nameof(subjectUserId));
        }

        return await GetAuthorizedPartiesByUserId(userId, filter, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<AuthorizedParty>> GetAuthorizedPartiesByUserId(int subjectUserId, AuthorizedPartiesFilters filter, CancellationToken cancellationToken)
    {
        var subject = await repoService.GetEntityByUserId(subjectUserId, cancellationToken);
        return await GetAuthorizedParties(subject, filter, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<AuthorizedParty>> GetAuthorizedPartiesByPersonId(string subjectPersonId, AuthorizedPartiesFilters filter, CancellationToken cancellationToken)
    {
        var subject = await repoService.GetEntityByPersonId(subjectPersonId, cancellationToken);
        return await GetAuthorizedParties(subject, filter, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<AuthorizedParty>> GetAuthorizedPartiesByPersonUuid(string subjectPersonUuid, AuthorizedPartiesFilters filter, CancellationToken cancellationToken)
    {
        return await GetAuthorizedPartiesByPartyUuid(subjectPersonUuid, filter, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<AuthorizedParty>> GetAuthorizedPartiesByOrganizationId(string subjectOrganizationNumber, AuthorizedPartiesFilters filter, CancellationToken cancellationToken)
    {
        var subject = await repoService.GetEntityByOrganizationId(subjectOrganizationNumber, cancellationToken);
        return await GetAuthorizedParties(subject, filter, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<AuthorizedParty>> GetAuthorizedPartiesByOrganizationUuid(string subjectOrganizationUuid, AuthorizedPartiesFilters filter, CancellationToken cancellationToken)
    {
        return await GetAuthorizedPartiesByPartyUuid(subjectOrganizationUuid, filter, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<AuthorizedParty>> GetAuthorizedPartiesByEnterpriseUsername(string subjectEnterpriseUsername, AuthorizedPartiesFilters filter, CancellationToken cancellationToken)
    {
        var subject = await repoService.GetEntityByUsername(subjectEnterpriseUsername, cancellationToken);
        return await GetAuthorizedParties(subject, filter, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<AuthorizedParty>> GetAuthorizedPartiesByEnterpriseUserUuid(string subjectEnterpriseUserUuid, AuthorizedPartiesFilters filter, CancellationToken cancellationToken)
    {
        return await GetAuthorizedPartiesByPartyUuid(subjectEnterpriseUserUuid, filter, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<AuthorizedParty>> GetAuthorizedPartiesBySystemUserUuid(string subjectSystemUserUuid, AuthorizedPartiesFilters filter, CancellationToken cancellationToken)
    {
        return await GetAuthorizedPartiesByPartyUuid(subjectSystemUserUuid, filter, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Guid>> GetPartyFilterUuids(IEnumerable<BaseAttribute> partyAttributes, CancellationToken cancellationToken = default)
    {
        List<Guid> partyUuids = new();
        foreach (var partyAttribute in partyAttributes)
        {
            switch (partyAttribute.Type)
            {
                case AltinnXacmlConstants.MatchAttributeIdentifiers.PartyUuidAttribute:
                case AltinnXacmlConstants.MatchAttributeIdentifiers.PersonUuid:
                case AltinnXacmlConstants.MatchAttributeIdentifiers.OrganizationUuid:
                case AltinnXacmlConstants.MatchAttributeIdentifiers.EnterpriseUserUuid:
                case AltinnXacmlConstants.MatchAttributeIdentifiers.SystemUserUuid:
                    if (!Guid.TryParse(partyAttribute.Value, out Guid partyUuid))
                    {
                        throw new ArgumentException(message: $"Not a well-formed uuid: {partyAttribute.Value}", paramName: nameof(partyAttributes));
                    }

                    // Directly adds the uuid we don't bother checking existence here
                    partyUuids.Add(partyUuid);
                    break;
                case AltinnXacmlConstants.MatchAttributeIdentifiers.PartyAttribute:
                    if (!int.TryParse(partyAttribute.Value, out int partyId))
                    {
                        throw new ArgumentException(message: $"Not a valid integer: {partyAttribute.Value}", paramName: nameof(partyAttributes));
                    }

                    var entity = await repoService.GetEntityByPartyId(partyId, cancellationToken);
                    if (entity != null)
                    {
                        partyUuids.Add(entity.Id);
                    }

                    break;
                case AltinnXacmlConstants.MatchAttributeIdentifiers.UserAttribute:
                    if (!int.TryParse(partyAttribute.Value, out int userId))
                    {
                        throw new ArgumentException(message: $"Not a valid integer: {partyAttribute.Value}", paramName: nameof(partyAttributes));
                    }

                    var userEntity = await repoService.GetEntityByUserId(userId, cancellationToken);
                    if (userEntity != null)
                    {
                        partyUuids.Add(userEntity.Id);
                    }

                    break;
                case AltinnXacmlConstants.MatchAttributeIdentifiers.PersonId:
                    var personEntity = await repoService.GetEntityByPersonId(partyAttribute.Value, cancellationToken);
                    if (personEntity != null)
                    {
                        partyUuids.Add(personEntity.Id);
                    }

                    break;
                case AltinnXacmlConstants.MatchAttributeIdentifiers.OrganizationId:
                    var orgEntity = await repoService.GetEntityByOrganizationId(partyAttribute.Value, cancellationToken);
                    if (orgEntity != null)
                    {
                        partyUuids.Add(orgEntity.Id);
                    }

                    break;
                case AltinnXacmlConstants.MatchAttributeIdentifiers.EnterpriseUserName:
                    var entEntity = await repoService.GetEntityByUsername(partyAttribute.Value, cancellationToken);
                    if (entEntity != null)
                    {
                        partyUuids.Add(entEntity.Id);
                    }

                    break;
                default:
                    throw new ArgumentException(message: $"Unknown attribute type: {partyAttribute.Type}", paramName: nameof(partyAttributes));
            }
        }

        return partyUuids;
    }

    private async Task<List<AuthorizedParty>> GetAuthorizedParties(AuthorizedPartiesFilters filter, Entity userSubject, IEnumerable<Guid> orgSubjectParties = null, CancellationToken cancellationToken = default)
    {
        Task<(IEnumerable<AuthorizedParty> a2AuthorizedParties, Dictionary<Guid, Entity> allA2Parties)> a2Task = Task.FromResult((Enumerable.Empty<AuthorizedParty>(), new Dictionary<Guid, Entity>()));
        Task<(IEnumerable<AuthorizedParty> a3AuthorizedParties, Dictionary<Guid, AuthorizedParty> allA3Parties)> a3Task = Task.FromResult((Enumerable.Empty<AuthorizedParty>(), new Dictionary<Guid, AuthorizedParty>()));

        if (filter.IncludeAltinn2 && userSubject.UserId.HasValue)
        {
            a2Task = Task.Run(async () =>
            {
                var a2AuthorizedParties = await altinnRolesClient.GetAuthorizedPartiesWithRoles(userSubject.UserId.Value, cancellationToken);

                if (filter.PartyFilter?.Count() > 0)
                {
                    a2AuthorizedParties = GetFilteredA2Parties(a2AuthorizedParties, filter);
                }

                return (a2AuthorizedParties.AsEnumerable(), new Dictionary<Guid, Entity>());
            });
        }

        if (filter.IncludeAltinn3)
        {
            a3Task = Task.Run(async () =>
            {
                var (allA3Parties, a3AuthorizedParties) = await GetAltinn3AuthorizedParties(filter, userSubject.Id, orgSubjectParties?.ToList(), cancellationToken);
                return (a3AuthorizedParties, allA3Parties);
            });
        }

        await Task.WhenAll(a2Task, a3Task);

        var a2Result = await a2Task;
        var a3Result = await a3Task;

        // Since EF does not support parallel use of DbContexts, we need to fetch the Altinn 2 parties separately here
        if (a2Result.a2AuthorizedParties.Count() > 0)
        {
            List<Guid> a2PartyUuids = a2Result.a2AuthorizedParties.Select(p => p.PartyUuid).Distinct().ToList();
            a2PartyUuids.AddRange(a2Result.a2AuthorizedParties.SelectMany(p => p.Subunits).Select(su => su.PartyUuid).Distinct());
            var a2Parties = await repoService.GetEntities(a2PartyUuids, cancellationToken);

            foreach (var a2Party in a2Parties)
            {
                a2Result.allA2Parties[a2Party.Id] = a2Party;
            }
        }

        return MergeAuthorizePartyLists(
            a2Result.a2AuthorizedParties,
            a2Result.allA2Parties,
            a3Result.a3AuthorizedParties,
            a3Result.allA3Parties,
            filter
        ).ToList();
    }

    private IEnumerable<AuthorizedParty> MergeAuthorizePartyLists(IEnumerable<AuthorizedParty> a2AuthorizedParties, Dictionary<Guid, Entity> allA2Parties, IEnumerable<AuthorizedParty> a3AuthorizedParties, Dictionary<Guid, AuthorizedParty> allParties, AuthorizedPartiesFilters filters)
    {
        List<AuthorizedParty> result = a3AuthorizedParties.ToList();

        // ToDo: Merge Altinn 2 authorized parties with Altinn 3 authorized parties, ensuring no duplicates and enrich model with AuthorizedRoles from Altinn 2
        foreach (AuthorizedParty a2Party in a2AuthorizedParties)
        {
            if (allParties.TryGetValue(a2Party.PartyUuid, out AuthorizedParty existingA3Party))
            {
                // Merge roles from Altinn 2 into existing Altinn 3 party
                existingA3Party.AuthorizedRoles = filters.IncludeRoles ? a2Party.AuthorizedRoles : [];

                if (!a2Party.OnlyHierarchyElementWithNoAccess)
                {
                    // Only set to false if Altinn 2 party has actual access
                    existingA3Party.OnlyHierarchyElementWithNoAccess = false;
                }

                foreach (AuthorizedParty a2SubUnit in a2Party.Subunits)
                {
                    if (allParties.TryGetValue(a2SubUnit.PartyUuid, out AuthorizedParty existingSubUnit))
                    {
                        // Merge roles from Altinn 2 into existing Altinn 3 subunit
                        existingSubUnit.AuthorizedRoles = filters.IncludeRoles ? a2SubUnit.AuthorizedRoles : [];
                    }
                    else
                    {
                        // Add new Altinn 2 subunit
                        var enhancedA2SubUnit = BuildAuthorizedPartyFromEntity(allA2Parties[a2SubUnit.PartyUuid]);
                        enhancedA2SubUnit.AuthorizedRoles = filters.IncludeRoles ? a2SubUnit.AuthorizedRoles : [];

                        existingA3Party.Subunits.Add(enhancedA2SubUnit);
                        allParties.Add(enhancedA2SubUnit.PartyUuid, enhancedA2SubUnit);
                    }
                }
            }
            else
            {
                // Add new Altinn 2 party and its subunits
                var enhancedA2Party = BuildAuthorizedPartyFromEntity(allA2Parties[a2Party.PartyUuid], onlyHierarchyElement: a2Party.OnlyHierarchyElementWithNoAccess);
                enhancedA2Party.AuthorizedRoles = filters.IncludeRoles ? a2Party.AuthorizedRoles : [];

                allParties.Add(a2Party.PartyUuid, enhancedA2Party);
                foreach (AuthorizedParty a2SubUnit in a2Party.Subunits)
                {
                    var enhancedA2SubUnit = BuildAuthorizedPartyFromEntity(allA2Parties[a2SubUnit.PartyUuid]);
                    enhancedA2SubUnit.AuthorizedRoles = filters.IncludeRoles ? a2SubUnit.AuthorizedRoles : [];
                    enhancedA2Party.Subunits.Add(enhancedA2SubUnit);

                    allParties.Add(enhancedA2SubUnit.PartyUuid, enhancedA2SubUnit);
                }

                result.Add(enhancedA2Party);
            }
        }

        return result;
    }

    private async Task<Tuple<Dictionary<Guid, AuthorizedParty>, IEnumerable<AuthorizedParty>>> GetAltinn3AuthorizedParties(AuthorizedPartiesFilters filter, Guid toId, List<Guid> toOrgs = null, CancellationToken cancellationToken = default)
    {
        // Get AccessPackage Delegations
        ////var packagePermissions = await repoService.GetPackagesFromOthers(toId, filters: filter, ct: cancellationToken);
        var connections = await repoService.GetConnectionsFromOthers(toId, filters: filter, ct: cancellationToken);

        // Get App, Resource and Instance delegations
        List<Guid> allToParties = toOrgs ?? new List<Guid>();
        allToParties.Add(toId);

        var resourceDelegations = await resourceDelegationRepository.GetAllDelegationChangesForAuthorizedParties(allToParties, cancellationToken: cancellationToken);
        resourceDelegations = await AddInstanceDelegations(resourceDelegations, allToParties, cancellationToken);

        if (filter.PartyFilter?.Count() > 0)
        {
            resourceDelegations = resourceDelegations.Where(d => filter.PartyFilter.ContainsKey(d.FromUuid.Value)).ToList();
        }

        // Get Party info for all from-uuids
        var fromUuids = resourceDelegations.Where(d => d.FromUuid.HasValue).Select(d => d.FromUuid.Value).ToList();
        ////fromUuids.AddRange(packagePermissions.SelectMany(p => p.Permissions).Select(p => p.From.Id));
        fromUuids.AddRange(connections.Select(c => c.FromId).Distinct());
        var fromParties = await repoService.GetEntities(fromUuids.Distinct(), cancellationToken);
        var fromSubUnits = await repoService.GetSubunits(fromUuids.Distinct(), cancellationToken);

        if (filter.PartyFilter?.Count() > 0)
        {
            fromSubUnits = fromSubUnits.Where(su => filter.PartyFilter.ContainsKey(su.Id)).ToList();
        }

        (Dictionary<Guid, AuthorizedParty> parties, IEnumerable<AuthorizedParty> authorizedParties) = BuildDictionaryFromEntities(fromParties, fromSubUnits);

        // Enrich AuthorizedParties with all authorized AccessPackages, Resources and Instances
        ////EnrichWithAccessPackageParties(parties, packagePermissions, filter);
        EnrichWithAccessPackageParties(parties, connections, filter);
        EnrichWithResourceAndInstanceParties(parties, resourceDelegations, filter);

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
                // Either person or top-level organization.
                // Still need to check whether already exists (may have been added as parent (with onlyHierarchyElement = true) through a subunit access). If exists, just continue.
                if (!allPartiesDict.TryGetValue(party.Id, out AuthorizedParty _))
                {
                    allPartiesDict[party.Id] = BuildAuthorizedPartyFromEntity(party);
                    authorizedParties.Add(allPartiesDict[party.Id]);
                }
            }
        }

        // Add all subunits to their top-level organization and to the dictionary
        foreach (var subunit in subunits)
        {
            // Need to check whether subunit already exists (may have been added through a direct subunit access). If exists, just continue.
            if (!allPartiesDict.TryGetValue(subunit.Id, out AuthorizedParty _))
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

    private static void EnrichWithAccessPackageParties(Dictionary<Guid, AuthorizedParty> parties, IEnumerable<PackagePermissionDto> packagePermissions, AuthorizedPartiesFilters filters)
    {
        if (!filters.IncludeAccessPackages)
        {
            return;
        }

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

    private static void EnrichWithAccessPackageParties(Dictionary<Guid, AuthorizedParty> parties, List<ConnectionQueryExtendedRecord> packageConnections, AuthorizedPartiesFilters filters)
    {
        if (!filters.IncludeAccessPackages)
        {
            return;
        }

        foreach (var packageConnection in packageConnections)
        {
            if (parties.TryGetValue(packageConnection.FromId, out AuthorizedParty party))
            {
                party.EnrichWithAccessPackage(packageConnection.Packages.DistinctBy(p => p.Id).Select(p => p.Urn.Split(":").Last()).ToList());
            }
            else
            {
                // This should not happen as all parties are retrieved based on the from parties on the delegations
                Unreachable();
            }
        }
    }

    private static void EnrichWithResourceAndInstanceParties(Dictionary<Guid, AuthorizedParty> parties, List<DelegationChange> resourceDelegations, AuthorizedPartiesFilters filters)
    {
        if (!filters.IncludeResources && !filters.IncludeInstances)
        {
            return;
        }

        foreach (DelegationChange delegation in resourceDelegations)
        {
            if (parties.TryGetValue(delegation.FromUuid.Value, out AuthorizedParty party))
            {
                if (delegation.InstanceId != null && filters.IncludeInstances)
                {
                    party.EnrichWithResourceInstanceAccess(delegation.ResourceId, delegation.InstanceId);
                }
                else if (filters.IncludeResources)
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

    private List<AuthorizedParty> GetFilteredA2Parties(IEnumerable<AuthorizedParty> parties, AuthorizedPartiesFilters filters)
    {
        List<AuthorizedParty> result = new();
        foreach (var party in parties)
        {
            List<AuthorizedParty> subunits = new();
            foreach (var subunit in party.Subunits)
            {
                if (filters.PartyFilter.ContainsKey(subunit.PartyUuid))
                {
                    subunits.Add(subunit);
                }
            }

            party.Subunits = subunits;
            if (filters.PartyFilter.ContainsKey(party.PartyUuid) || party.Subunits.Count > 0)
            {
                result.Add(party);
            }
        }

        return result;
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
            case var siUserType when siUserType == EntityTypeConstants.SelfIdentified.Id:
                party.Type = AuthorizedPartyType.SelfIdentified;
                break;
            default:
                // Only Organizations and Persons can be represented by others. SIUsers can represent themselves.
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
