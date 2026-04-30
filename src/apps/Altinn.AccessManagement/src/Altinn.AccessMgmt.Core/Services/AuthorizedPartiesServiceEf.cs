using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Enums;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessMgmt.Core.Appsettings;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.Core.Utils.Helper;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Queries.Connection.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement.Enums;
using Microsoft.Extensions.Caching.Memory;

namespace Altinn.AccessManagement.Core.Services;

/// <inheritdoc/>
public class AuthorizedPartiesServiceEf(
    IAltinnRolesClient altinnRolesClient,
    IContextRetrievalService contextRetrievalService,
    IAuthorizedPartyRepoServiceEf repoService,
    IMemoryCache memoryCache) : IAuthorizedPartiesService
{
    private static readonly MemoryCacheEntryOptions _cacheEntryOptions = new() { AbsoluteExpirationRelativeToNow = new TimeSpan(0, 5, 0) };

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
        AltinnXacmlConstants.MatchAttributeIdentifiers.IdPortenEmail => await GetAuthorizedPartiesByIdPortenEmailId(subjectAttribute.Value, filter, cancellationToken),
        _ => throw new ArgumentException(message: $"Unknown attribute type: {subjectAttribute.Type}", paramName: nameof(subjectAttribute))
    };

    public async Task<List<AuthorizedParty>> GetAuthorizedPartiesByEntity(Entity subject, AuthorizedPartiesFilters filter, CancellationToken cancellationToken = default)
    {
        if (subject == null)
        {
            return await Task.FromResult(new List<AuthorizedParty>());
        }

        if (filter.ProviderCode != null || filter.AnyOfResourceIds?.Length > 0)
        {
            filter = await ProcessProviderAndResourceFilters(filter, cancellationToken);

            if (filter.ResourceFilter == null || filter.ResourceFilter.Count() == 0)
            {
                // ServiceOwner or Resource filter specified, but no resources found matching.
                return new List<AuthorizedParty>();
            }

            // Provider/resource filtering relies entirely on the Altinn 3 ConnectionQuery; skip Altinn 2.
            filter.IncludeAltinn2 = false;
        }

        filter = await ProcessAutoFilters(filter, subject, cancellationToken);

        if (!AuthorizedPartiesSettings.IncludeAltinn2)
        {
            filter.IncludeAltinn2 = false;
        }

        switch (subject.TypeId)
        {
            case var id when id == EntityTypeConstants.Person.Id:

                return await GetAuthorizedPartiesInternal(filter, subject, cancellationToken);

            case var id when id == EntityTypeConstants.EnterpriseUser.Id:

                return await GetAuthorizedPartiesInternal(filter, subject, cancellationToken);

            case var id when id == EntityTypeConstants.Organization.Id:

                // Organizations can not have Altinn 2 roles, only Altinn 3 delegations.
                filter.IncludeAltinn2 = false;
                return await GetAuthorizedPartiesInternal(filter, subject, cancellationToken);

            case var id when id == EntityTypeConstants.SystemUser.Id:

                // System users can not have Altinn 2 roles, only Altinn 3 delegations.
                filter.IncludeAltinn2 = false;
                return await GetAuthorizedPartiesInternal(filter, subject, cancellationToken);

            case var id when id == EntityTypeConstants.SelfIdentified.Id:

                // SelfIdentified are fully imported to Altinn 3 so no longer need to check Altinn 2.
                filter.IncludeAltinn2 = false;
                return await GetAuthorizedPartiesInternal(filter, subject, cancellationToken);

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
        return await GetAuthorizedPartiesByEntity(subject, filter, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<AuthorizedParty>> GetAuthorizedPartiesByPartyId(int subjectPartyId, AuthorizedPartiesFilters filter, CancellationToken cancellationToken)
    {
        var subject = await repoService.GetEntityByPartyId(subjectPartyId, cancellationToken);
        return await GetAuthorizedPartiesByEntity(subject, filter, cancellationToken);
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
        return await GetAuthorizedPartiesByEntity(subject, filter, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<AuthorizedParty>> GetAuthorizedPartiesByPersonId(string subjectPersonId, AuthorizedPartiesFilters filter, CancellationToken cancellationToken)
    {
        var subject = await repoService.GetEntityByPersonId(subjectPersonId, cancellationToken);
        return await GetAuthorizedPartiesByEntity(subject, filter, cancellationToken);
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
        return await GetAuthorizedPartiesByEntity(subject, filter, cancellationToken);
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
        return await GetAuthorizedPartiesByEntity(subject, filter, cancellationToken);
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
    public async Task<List<AuthorizedParty>> GetAuthorizedPartiesByIdPortenEmailId(string subjectIdPortenEmailId, AuthorizedPartiesFilters filter, CancellationToken cancellationToken)
    {
        var subject = await repoService.GetEntityByIdPortenEmailId(subjectIdPortenEmailId, cancellationToken);
        return await GetAuthorizedPartiesByEntity(subject, filter, cancellationToken);
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

    private async Task<List<AuthorizedParty>> GetAuthorizedPartiesInternal(AuthorizedPartiesFilters filter, Entity userSubject, CancellationToken cancellationToken = default)
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

                return (a2AuthorizedParties.AsEnumerable(), new Dictionary<Guid, Entity>());
            });
        }

        if (filter.IncludeAltinn3)
        {
            a3Task = Task.Run(async () =>
            {
                var (allA3Parties, a3AuthorizedParties) = await GetAltinn3AuthorizedParties(filter, userSubject.Id, cancellationToken);
                return (a3AuthorizedParties, allA3Parties);
            });
        }

        await Task.WhenAll(a2Task, a3Task);

        var a2Result = await a2Task;
        var a3Result = await a3Task;

        // Since EF does not support parallel use of DbContexts, we need to fetch the Altinn 2 parties separately here
        if (filter.IncludeAltinn2 && a2Result.A2AuthorizedParties.Any())
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
            a3Result.AllA3Parties
        );
    }

    private List<AuthorizedParty> MergeAuthorizePartyLists(IEnumerable<AuthorizedParty> a2AuthorizedParties, Dictionary<Guid, Entity> allA2Parties, IEnumerable<AuthorizedParty> a3AuthorizedParties, Dictionary<Guid, AuthorizedParty> allParties)
    {
        List<AuthorizedParty> result = a3AuthorizedParties.ToList();

        // Merge Altinn 2 authorized parties with Altinn 3 authorized parties, ensuring no duplicates
        foreach (AuthorizedParty a2Party in a2AuthorizedParties)
        {
            if (allParties.TryGetValue(a2Party.PartyUuid, out AuthorizedParty existingA3Party))
            {
                if (!a2Party.OnlyHierarchyElementWithNoAccess)
                {
                    // Only set to false if Altinn 2 party has actual access
                    existingA3Party.OnlyHierarchyElementWithNoAccess = false;
                }

                foreach (AuthorizedParty a2SubUnit in a2Party.Subunits)
                {
                    if (allParties.TryGetValue(a2SubUnit.PartyUuid, out AuthorizedParty existingSubUnit))
                    {
                        // No longer need to enrich with role info, so can just continue
                        continue;
                    }
                    else
                    {
                        // Add new Altinn 2 subunit
                        var enhancedA2SubUnit = BuildAuthorizedPartyFromEntity(allA2Parties[a2SubUnit.PartyUuid]);

                        existingA3Party.Subunits.Add(enhancedA2SubUnit);
                        allParties.Add(enhancedA2SubUnit.PartyUuid, enhancedA2SubUnit);
                    }
                }
            }
            else
            {
                // Add new Altinn 2 party and its subunits
                var enhancedA2Party = BuildAuthorizedPartyFromEntity(allA2Parties[a2Party.PartyUuid], onlyHierarchyElement: a2Party.OnlyHierarchyElementWithNoAccess);

                allParties.Add(a2Party.PartyUuid, enhancedA2Party);
                foreach (AuthorizedParty a2SubUnit in a2Party.Subunits)
                {
                    var enhancedA2SubUnit = BuildAuthorizedPartyFromEntity(allA2Parties[a2SubUnit.PartyUuid]);
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
        CancellationToken cancellationToken = default)
    {
        List<ConnectionQueryExtendedRecord> connections = await repoService.GetConnectionsFromOthers(toId, filters: filter, ct: cancellationToken);

        // Post-query filtering of connections when resource/provider filters are active
        if (filter.ProviderCode != null || filter.AnyOfResourceIds?.Length > 0)
        {
            connections = FilterConnections(connections, filter);
        }

        var fromUuids = connections.Select(c => c.FromId).Distinct();
        var fromParties = await repoService.GetEntities(fromUuids, cancellationToken);
        var fromSubUnits = await repoService.GetSubunits(fromUuids, cancellationToken);

        (Dictionary<Guid, AuthorizedParty> parties, IEnumerable<AuthorizedParty> authorizedParties) = BuildDictionaryFromEntities(fromParties, fromSubUnits);

        // Enrich AuthorizedParties with all authorized Roles, AccessPackages, Resources and Instances from the connections if requested in the filters
        EnrichWithPartiesWithAccessInfo(parties, connections, filter);

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

    private void EnrichWithPartiesWithAccessInfo(Dictionary<Guid, AuthorizedParty> parties, List<ConnectionQueryExtendedRecord> connections, AuthorizedPartiesFilters filters)
    {
        if (!filters.IncludeRoles && !filters.IncludeAccessPackages && !filters.IncludeResources && !filters.IncludeInstances)
        {
            return;
        }

        foreach (var connection in connections)
        {
            if (parties.TryGetValue(connection.FromId, out AuthorizedParty party))
            {
                if (filters.IncludeRoles && RoleConstants.TryGetById(connection.RoleId, out var role) && (role.Id != RoleConstants.Rightholder.Id && role.Id != RoleConstants.Agent.Id))
                {
                    if (filters.RoleFilter == null || filters.RoleFilter.ContainsKey(role.Entity.Code) || (role.Entity.LegacyCode != null && filters.RoleFilter.ContainsKey(role.Entity.LegacyCode)))
                    {
                        party.EnrichWithRole(role.Entity.Code);

                        if (role.Entity.LegacyCode != null)
                        {
                            party.EnrichWithRole(role.Entity.LegacyCode);
                        }
                    }
                }

                if (filters.IncludeAccessPackages && connection.Packages != null && connection.Packages.Count > 0)
                {
                    party.EnrichWithAccessPackage(connection.Packages.DistinctBy(p => p.Id).Select(p => PackageConstants.TryGetById(p.Id, out var package) ? package.Entity.Code : null).Where(p => p != null));
                }

                if (filters.IncludeResources && connection.Resources != null && connection.Resources.Count > 0)
                {
                    party.EnrichWithResourceAccess(connection.Resources.DistinctBy(r => r.Id).Select(r => r.RefId));
                }

                if (filters.IncludeInstances && connection.Instances != null && connection.Instances.Count > 0)
                {
                    foreach (var instance in connection.Instances)
                    {
                        var instanceId = instance.InstanceId;
                        if (DelegationCheckHelper.IsAppResource(instance.ResourceRefId, out string _, out string _) && instanceId.StartsWith(AltinnXacmlConstants.MatchAttributeIdentifiers.InstanceAttribute, StringComparison.OrdinalIgnoreCase))
                        {
                            // Remove prefix from instanceId to remain backwards compatible. 
                            var partyAndInstanceId = instanceId.Substring(AltinnXacmlConstants.MatchAttributeIdentifiers.InstanceAttribute.Length + 1);
                            var split = partyAndInstanceId.Split('/');
                            instanceId = split.Length == 2 ? split[1] : partyAndInstanceId;
                        }

                        party.EnrichWithResourceInstanceAccess(instance.ResourceRefId, instanceId, instance.InstanceId);
                    }
                }
            }
            else
            {
                // This should not happen as all parties are retrieved based on the from parties on the delegations
                Unreachable();
            }
        }
    }

    private static List<ConnectionQueryExtendedRecord> FilterConnections(List<ConnectionQueryExtendedRecord> connections, AuthorizedPartiesFilters filters)
    {
        bool hasResourceFilter = filters.ResourceFilter?.Count > 0;
        bool hasPackageFilter = filters.PackageFilter?.Count > 0;
        bool hasRoleFilter = filters.RoleFilter?.Count > 0;

        if (!hasResourceFilter && !hasPackageFilter && !hasRoleFilter)
        {
            return connections;
        }

        List<ConnectionQueryExtendedRecord> filtered = new();
        foreach (var connection in connections)
        {
            bool matchesFilter = false;

            // Roles: keep the connection if the role matches, but do not trim the role here.
            // Role filtering during enrichment is handled in EnrichWithPartiesWithAccessInfo.
            if (hasRoleFilter && RoleConstants.TryGetById(connection.RoleId, out var role) &&
                (filters.RoleFilter.ContainsKey(role.Entity.Code) || (role.Entity.LegacyCode != null && filters.RoleFilter.ContainsKey(role.Entity.LegacyCode))))
            {
                matchesFilter = true;
            }

            // Packages: trim to only matching packages
            if (connection.Packages != null)
            {
                if (hasPackageFilter)
                {
                    connection.Packages = connection.Packages.Where(p => filters.PackageFilter.ContainsKey(p.Id)).ToList();
                }
                else
                {
                    connection.Packages = new();
                }

                if (connection.Packages.Count > 0)
                {
                    matchesFilter = true;
                }
            }

            // Resources: trim to only matching resources
            if (connection.Resources != null)
            {
                if (hasResourceFilter)
                {
                    connection.Resources = connection.Resources.Where(r => filters.ResourceFilter.ContainsKey(r.RefId)).ToList();
                }
                else
                {
                    connection.Resources = new();
                }

                if (connection.Resources.Count > 0)
                {
                    matchesFilter = true;
                }
            }

            // Instances: trim to only matching instances
            if (connection.Instances != null)
            {
                if (hasResourceFilter)
                {
                    connection.Instances = connection.Instances.Where(i => filters.ResourceFilter.ContainsKey(i.ResourceRefId)).ToList();
                }
                else
                {
                    connection.Instances = new();
                }

                if (connection.Instances.Count > 0)
                {
                    matchesFilter = true;
                }
            }

            if (matchesFilter)
            {
                filtered.Add(connection);
            }
        }

        return filtered;
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
            case var orgType when orgType == EntityTypeConstants.Organization.Id:
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
                party.EmailId = entity.EmailIdentifier;
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
        if (filter.ProviderCode != null || filter.AnyOfResourceIds?.Length > 0)
        {
            // Make sure all include filters are set to true, as we need all access info when filtering on provider/resources
            filter.IncludeAltinn2 = filter.IncludeAltinn3 = filter.IncludeRoles = filter.IncludeAccessPackages = filter.IncludeResources = filter.IncludeInstances = true;  // ToDo: Remove?

            // Build cache key. Currently PackageFilter and RoleFilter are always empty when entering this method
            StringBuilder cacheBuilder = new($"pparf:{filter.ProviderCode}:");
            filter.AnyOfResourceIds?.ToList().ForEach(r => cacheBuilder.Append($"{r}:"));
            cacheBuilder.Append("|");
            filter.PackageFilter?.ToList().ForEach(p => cacheBuilder.Append($"{p.Key}:"));
            cacheBuilder.Append("|");
            filter.RoleFilter?.ToList().ForEach(r => cacheBuilder.Append($"{r.Key}:"));
            string cacheKey = cacheBuilder.ToString();
            if (!memoryCache.TryGetValue(cacheKey, out (SortedDictionary<string, string> ResourceFilter, SortedDictionary<Guid, Guid>? PackageFilter, SortedDictionary<string, string>? RoleFilter) cachedFilters))
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
                foreach (var packageResource in packageResources)
                {
                    if (!filter.PackageFilter.ContainsKey(packageResource.PackageId))
                    {
                        filter.PackageFilter[packageResource.PackageId] = packageResource.PackageId;
                    }
                }

                // Build filter.RoleFilter based on roleResources and packageRoles
                filter.RoleFilter ??= new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                List<string> roleCodes = await repoService.GetRoleCodesFromRoleResources(filter.ProviderCode, filter.ResourceFilter.Keys, ct: cancellationToken);
                foreach (var roleCode in roleCodes)
                {
                    filter.RoleFilter[roleCode] = roleCode;
                }

                cachedFilters.ResourceFilter = filter.ResourceFilter;
                cachedFilters.PackageFilter = filter.PackageFilter;
                cachedFilters.RoleFilter = filter.RoleFilter;

                memoryCache.Set(cacheKey, cachedFilters, _cacheEntryOptions);
            }

            filter.ResourceFilter = cachedFilters.ResourceFilter;
            filter.PackageFilter = cachedFilters.PackageFilter;
            filter.RoleFilter = cachedFilters.RoleFilter;
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
