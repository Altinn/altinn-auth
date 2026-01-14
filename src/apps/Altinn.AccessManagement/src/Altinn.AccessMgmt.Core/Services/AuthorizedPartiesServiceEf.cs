using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Enums;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Repositories.Interfaces;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Queries.Connection.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement.Enums;

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

        if (filter.ProviderCode != null || filter.AnyOfResourceIds?.Count() > 0)
        {
            filter = await ProcessProviderAndResourceFilters(filter, cancellationToken);

            if (filter.ResourceFilter == null || filter.ResourceFilter.Count() == 0)
            {
                // ServiceOwner or Resource filter specified, but no resources found matching.
                return new List<AuthorizedParty>();
            }
        }

        filter = await ProcessAutoFilters(filter, subject, cancellationToken);

        switch (subject.TypeId)
        {
            case var id when id == EntityTypeConstants.Person.Id:
                List<Guid> keyRoleEntities = [];
                if (filter.IncludePartiesViaKeyRoles == AuthorizedPartiesIncludeFilter.Auto || filter.IncludePartiesViaKeyRoles == AuthorizedPartiesIncludeFilter.True)
                {
                    // Persons can have key roles for other parties, meaning they inherit access to others via these parties.
                    var keyRoleAssignments = await repoService.GetKeyRoleAssignments(subject.Id, cancellationToken);
                    keyRoleEntities = keyRoleAssignments.Select(t => t.FromId).Distinct().ToList();

                    // Also get any sub-units of key role entities
                    if (keyRoleEntities.Count > 0)
                    {
                        var subUnits = await repoService.GetSubunits(keyRoleEntities, cancellationToken);
                        keyRoleEntities.AddRange(subUnits.Select(t => t.Id));
                    }
                }

                return await GetAuthorizedParties(filter, subject, keyRoleEntities, cancellationToken);

            case var id when id == EntityTypeConstants.EnterpriseUser.Id:

                // Enterprise user can also have key role (ECKeyRole) for their organization. Will still need to get these via SBL Bridge until A2-role import is complete.
                IEnumerable<Entity> eckeyroleEntities = [];
                if (subject.UserId.HasValue && (filter.IncludePartiesViaKeyRoles == AuthorizedPartiesIncludeFilter.Auto || filter.IncludePartiesViaKeyRoles == AuthorizedPartiesIncludeFilter.True))
                {
                    // A2 lookup of key role parties includes subunits by default
                    List<int> keyRolePartyIds = await contextRetrievalService.GetKeyRolePartyIds(subject.UserId.Value, cancellationToken);
                    eckeyroleEntities = await repoService.GetEntitiesByPartyIds(keyRolePartyIds, cancellationToken);
                }

                return await GetAuthorizedParties(filter, subject, eckeyroleEntities.Select(t => t.Id), cancellationToken);

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
                case AltinnXacmlConstants.MatchAttributeIdentifiers.PersonUuid:
                case AltinnXacmlConstants.MatchAttributeIdentifiers.EnterpriseUserUuid:
                case AltinnXacmlConstants.MatchAttributeIdentifiers.SystemUserUuid:
                    if (!Guid.TryParse(partyAttribute.Value, out Guid partyUuid))
                    {
                        throw new ArgumentException(message: $"Not a well-formed uuid: {partyAttribute.Value}", paramName: nameof(partyAttributes));
                    }

                    // Directly adds the uuid we don't bother checking existence here
                    partyUuids.Add(partyUuid);

                    break;
                case AltinnXacmlConstants.MatchAttributeIdentifiers.PartyUuidAttribute:
                case AltinnXacmlConstants.MatchAttributeIdentifiers.OrganizationUuid:
                    if (!Guid.TryParse(partyAttribute.Value, out partyUuid))
                    {
                        throw new ArgumentException(message: $"Not a well-formed uuid: {partyAttribute.Value}", paramName: nameof(partyAttributes));
                    }

                    var uuidEntity = await repoService.GetEntity(partyUuid, cancellationToken);
                    if (uuidEntity != null)
                    {
                        partyUuids.Add(uuidEntity.Id);

                        if (uuidEntity.ParentId.HasValue)
                        {
                            // Also add parent uuid to cover subunit filters
                            partyUuids.Add(uuidEntity.ParentId.Value);
                        }
                    }

                    break;
                case AltinnXacmlConstants.MatchAttributeIdentifiers.PartyAttribute:
                    if (!int.TryParse(partyAttribute.Value, out int partyId))
                    {
                        throw new ArgumentException(message: $"Not a valid integer: {partyAttribute.Value}", paramName: nameof(partyAttributes));
                    }

                    var partyIdEntity = await repoService.GetEntityByPartyId(partyId, cancellationToken);
                    if (partyIdEntity != null)
                    {
                        partyUuids.Add(partyIdEntity.Id);

                        if (partyIdEntity.ParentId.HasValue)
                        {
                            // Also add parent uuid to cover subunit filters
                            partyUuids.Add(partyIdEntity.ParentId.Value);
                        }
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

                        if (orgEntity.ParentId.HasValue)
                        {
                            // Also add parent uuid to cover subunit filters
                            partyUuids.Add(orgEntity.ParentId.Value);
                        }
                    }

                    break;
                case AltinnXacmlConstants.MatchAttributeIdentifiers.EnterpriseUserName:
                    var enterpriseUserEntity = await repoService.GetEntityByUsername(partyAttribute.Value, cancellationToken);
                    if (enterpriseUserEntity != null)
                    {
                        partyUuids.Add(enterpriseUserEntity.Id);
                    }

                    break;
                default:
                    throw new ArgumentException(message: $"Unknown attribute type: {partyAttribute.Type}", paramName: nameof(partyAttributes));
            }
        }

        return partyUuids;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Guid>> GetPartyFilterUuids(IEnumerable<Guid> filterUuids, CancellationToken cancellationToken = default)
    {
        List<Guid> partyUuids = new();
        var entities = await repoService.GetEntities(filterUuids, cancellationToken);
        foreach (var entity in entities)
        {
            partyUuids.Add(entity.Id);
            if (entity.ParentId.HasValue)
            {
                // Also add parent uuid to cover subunit filters
                partyUuids.Add(entity.ParentId.Value);
            }
        }

        return partyUuids;
    }

    private async Task<List<AuthorizedParty>> GetAuthorizedParties(AuthorizedPartiesFilters filter, Entity userSubject, IEnumerable<Guid> orgSubjectParties = null, CancellationToken cancellationToken = default)
    {
        Task<(IEnumerable<AuthorizedParty> A2AuthorizedParties, Dictionary<Guid, Entity> AllA2Parties)> a2Task = Task.FromResult((Enumerable.Empty<AuthorizedParty>(), new Dictionary<Guid, Entity>()));
        Task<(IEnumerable<AuthorizedParty> A3AuthorizedParties, Dictionary<Guid, AuthorizedParty> AllA3Parties)> a3Task = Task.FromResult((Enumerable.Empty<AuthorizedParty>(), new Dictionary<Guid, AuthorizedParty>()));

        if (filter.IncludeAltinn2 && userSubject.UserId.HasValue)
        {
            a2Task = Task.Run(async () =>
            {
                var a2AuthorizedParties = await altinnRolesClient.GetAuthorizedPartiesWithRoles(userSubject.UserId.Value, filter.IncludePartiesViaKeyRoles == AuthorizedPartiesIncludeFilter.True, cancellationToken);
                
                if (filter.PartyFilter?.Count > 0)
                {
                    a2AuthorizedParties = GetFilteredA2Parties(a2AuthorizedParties, filter);
                }
                
                if (filter.ProviderCode != null || filter.AnyOfResourceIds?.Count() > 0)
                {
                    a2AuthorizedParties = FilterRoles(a2AuthorizedParties, filter);
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
        if (a2Result.A2AuthorizedParties.Count() > 0)
        {
            List<Guid> a2PartyUuids = a2Result.A2AuthorizedParties.Select(p => p.PartyUuid).Distinct().ToList();
            a2PartyUuids.AddRange(a2Result.A2AuthorizedParties.SelectMany(p => p.Subunits).Select(su => su.PartyUuid).Distinct());
            var a2Parties = await repoService.GetEntities(a2PartyUuids, cancellationToken);

            foreach (var a2Party in a2Parties)
            {
                a2Result.AllA2Parties[a2Party.Id] = a2Party;
            }
        }

        return MergeAuthorizePartyLists(
            a2Result.A2AuthorizedParties,
            a2Result.AllA2Parties,
            a3Result.A3AuthorizedParties,
            a3Result.AllA3Parties,
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

    private async Task<Tuple<Dictionary<Guid, AuthorizedParty>, IEnumerable<AuthorizedParty>>> GetAltinn3AuthorizedParties(
        AuthorizedPartiesFilters filter,
        Guid toId,
        List<Guid> toOrgs = null,
        CancellationToken cancellationToken = default)
    {
        /*
         * Get AccessPackage connections but only if no resource filter is specified, or if both resource and package filters are specified.
         * Note: will need updating when connection query are to be used for lookup and filters of roles and/or resource connections as well.
         */
        List<ConnectionQueryExtendedRecord> connections = [];
        bool resourceFilterSpecified = filter.ProviderCode != null || filter.AnyOfResourceIds?.Count() > 0;
        if (!resourceFilterSpecified || filter.PackageFilter?.Count > 0)
        {
            connections = await repoService.GetConnectionsFromOthers(toId, filters: filter, ct: cancellationToken);
        }

        // Get App, Resource and Instance delegations
        List<Guid> allToParties = toOrgs ?? new List<Guid>();
        allToParties.Add(toId);

        var resourceDelegations = await resourceDelegationRepository.GetAllDelegationChangesForAuthorizedParties(allToParties, cancellationToken: cancellationToken);
        resourceDelegations = await AddInstanceDelegations(resourceDelegations, allToParties, cancellationToken);

        if (filter.PartyFilter?.Count > 0)
        {
            resourceDelegations = resourceDelegations.Where(d => filter.PartyFilter.ContainsKey(d.FromUuid.Value)).ToList();
        }

        if (filter.ResourceFilter?.Count() > 0)
        {
            resourceDelegations = resourceDelegations.Where(d => d.ResourceId != null && filter.ResourceFilter.ContainsKey(d.ResourceId)).ToList();
        }

        // Get Party info for all from-uuids
        var fromUuids = resourceDelegations.Where(d => d.FromUuid.HasValue).Select(d => d.FromUuid.Value).ToList();
        fromUuids.AddRange(connections.Select(c => c.FromId).Distinct());
        var fromParties = await repoService.GetEntities(fromUuids.Distinct(), cancellationToken);
        var fromSubUnits = await repoService.GetSubunits(fromUuids.Distinct(), cancellationToken);

        if (filter.PartyFilter?.Count > 0)
        {
            fromSubUnits = fromSubUnits.Where(su => filter.PartyFilter.ContainsKey(su.Id)).ToList();
        }

        (Dictionary<Guid, AuthorizedParty> parties, IEnumerable<AuthorizedParty> authorizedParties) = BuildDictionaryFromEntities(fromParties, fromSubUnits);

        // Enrich AuthorizedParties with all authorized AccessPackages, Resources and Instances
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
                // Still need to check whether already exists (may have been added as parent (with onlyHierarchyElement = true) through a subunit access). If exists, reset onlyHierarchyElement to false.
                if (allPartiesDict.TryGetValue(party.Id, out AuthorizedParty existingParty))
                {
                    existingParty.OnlyHierarchyElementWithNoAccess = false;
                }
                else
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
        bool filterParties = filters.PartyFilter?.Count > 0;

        List<AuthorizedParty> result = new();
        foreach (var party in parties)
        {
            List<AuthorizedParty> subunits = new();
            foreach (var subunit in party.Subunits)
            {
                if (filterParties && !filters.PartyFilter.ContainsKey(subunit.PartyUuid))
                {
                    continue;
                }
                
                subunits.Add(subunit);
            }

            party.Subunits = subunits;
            if (filterParties && !filters.PartyFilter.ContainsKey(party.PartyUuid) && party.Subunits.Count == 0)
            {
                continue;
            }

            result.Add(party);
        }

        return result;
    }

    private static List<AuthorizedParty> FilterRoles(IEnumerable<AuthorizedParty> authorizedParties, AuthorizedPartiesFilters filter)
    {
        List<AuthorizedParty> parties = new();
        foreach (var party in authorizedParties)
        {
            party.AuthorizedRoles = party.AuthorizedRoles.Where(role => filter.RoleFilter.ContainsKey(role)).ToList();

            // Reset subunits and re-add only those with roles after filtering
            var subunits = party.Subunits;
            party.Subunits = new List<AuthorizedParty>();
            foreach (var subunit in subunits)
            {
                subunit.AuthorizedRoles = subunit.AuthorizedRoles.Where(role => filter.RoleFilter.ContainsKey(role)).ToList();

                if (subunit.AuthorizedRoles.Any())
                {
                    party.Subunits.Add(subunit);
                }
            }

            // Only add party if it has any roles or subunits with roles
            if (party.AuthorizedRoles.Count > 0 || party.Subunits.Count > 0)
            {
                parties.Add(party);
            }
        }

        return parties;
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

    private async Task<AuthorizedPartiesFilters> ProcessProviderAndResourceFilters(AuthorizedPartiesFilters filter, CancellationToken cancellationToken)
    {
        // Only build resource filters if providerCode or anyOfResourceIds filters are specified
        if (filter.ProviderCode != null || filter.AnyOfResourceIds?.Count() > 0)
        {
            List<Resource> resources = await repoService.GetResources(filter.ProviderCode, filter.AnyOfResourceIds, ct: cancellationToken);

            if (resources.Count == 0)
            {
                // ServiceOwner or Resource filter specified, but no resources found matching.
                return filter;
            }

            filter.ResourceFilter = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var resource in resources)
            {
                filter.ResourceFilter[resource.RefId] = resource.RefId;
            }

            List<PackageResource> packageResources = await repoService.GetPackageResources(filter.ProviderCode, filter.ResourceFilter.Keys, ct: cancellationToken);

            // Add packageIds from packageResources to filter.PackageFilter
            filter.PackageFilter ??= new SortedDictionary<Guid, Guid>();
            foreach (var package in packageResources)
            {
                if (!filter.PackageFilter.ContainsKey(package.Id))
                {
                    filter.PackageFilter[package.Id] = package.Id;
                }
            }

            // Build filter.RoleFilter based on roleResources and packageRoles
            filter.RoleFilter ??= new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            
            List<RoleResource> roleResources = await repoService.GetRoleResources(filter.ProviderCode, filter.ResourceFilter.Keys, ct: cancellationToken);
            foreach (var group in roleResources.GroupBy(rr => rr.RoleId))
            {
                Role role = group.First().Role;
                filter.RoleFilter[role.Code] = role.Code;

                if (role.LegacyCode != null)
                {
                    filter.RoleFilter[role.LegacyCode] = role.LegacyCode;
                }
            }

            // Find all Roles for the PackageResources found above, as we need to include roles giving access via packages as well.
            List<RolePackage> rolePackages = await repoService.GetRolePackages(packageIds: filter.PackageFilter.Keys, ct: cancellationToken);
            foreach (var group in rolePackages.GroupBy(rp => rp.PackageId))
            {
                foreach (var role in group.Select(rp => rp.Role))
                {
                    if (!filter.RoleFilter.ContainsKey(role.Code))
                    {
                        filter.RoleFilter[role.Code] = role.Code;
                    }

                    if (role.LegacyCode != null && !filter.RoleFilter.ContainsKey(role.LegacyCode))
                    {
                        filter.RoleFilter[role.LegacyCode] = role.LegacyCode;
                    }
                }
            }
        }

        return filter;
    }

    private async Task<AuthorizedPartiesFilters> ProcessAutoFilters(AuthorizedPartiesFilters filters, Entity subject, CancellationToken cancellationToken)
    {
        if (filters.IncludePartiesViaKeyRoles != AuthorizedPartiesIncludeFilter.Auto &&
            filters.IncludeSubParties != AuthorizedPartiesIncludeFilter.Auto &&
            filters.IncludeInactiveParties != AuthorizedPartiesIncludeFilter.Auto)
        {
            // No auto processing needed
            return filters;
        }

        if (subject.TypeId != EntityTypeConstants.Person.Id &&
            subject.TypeId != EntityTypeConstants.SelfIdentified.Id &&
            subject.TypeId != EntityTypeConstants.EnterpriseUser.Id)
        {
            // Only users have profile settings, for other entity types we default to including all
            filters.IncludePartiesViaKeyRoles = AuthorizedPartiesIncludeFilter.True;
            filters.IncludeSubParties = AuthorizedPartiesIncludeFilter.True;
            filters.IncludeInactiveParties = AuthorizedPartiesIncludeFilter.True;
            return filters;
        }

        if (!subject.UserId.HasValue)
        {
            Unreachable();
        }

        var userProfile = await contextRetrievalService.GetNewUserProfile(subject.UserId.Value, cancellationToken);
        if (userProfile == null)
        {
            // Should not happen, but if it does (brand new user perhaps?) we default to including all
            filters.IncludePartiesViaKeyRoles = AuthorizedPartiesIncludeFilter.True;
            filters.IncludeSubParties = AuthorizedPartiesIncludeFilter.True;
            filters.IncludeInactiveParties = AuthorizedPartiesIncludeFilter.True;
            return filters;
        }

        if (filters.IncludePartiesViaKeyRoles == AuthorizedPartiesIncludeFilter.Auto)
        {
            filters.IncludePartiesViaKeyRoles = userProfile.ProfileSettingPreference.ShowClientUnits ? AuthorizedPartiesIncludeFilter.True : AuthorizedPartiesIncludeFilter.False;
        }

        if (filters.IncludeSubParties == AuthorizedPartiesIncludeFilter.Auto)
        {
            filters.IncludeSubParties = userProfile.ProfileSettingPreference.ShouldShowSubEntities ? AuthorizedPartiesIncludeFilter.True : AuthorizedPartiesIncludeFilter.False;
        }

        if (filters.IncludeInactiveParties == AuthorizedPartiesIncludeFilter.Auto)
        {
            filters.IncludeInactiveParties = userProfile.ProfileSettingPreference.ShouldShowDeletedEntities ? AuthorizedPartiesIncludeFilter.True : AuthorizedPartiesIncludeFilter.False;
        }

        return filters;
    }

    [DoesNotReturn]
    private static void Unreachable()
    {
        throw new UnreachableException();
    }
}
