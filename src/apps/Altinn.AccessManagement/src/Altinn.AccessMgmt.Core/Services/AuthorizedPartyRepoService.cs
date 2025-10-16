using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Altinn.AccessManagement.Core.Enums;
using Altinn.AccessManagement.Core.Helpers.Extensions;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Repositories.Interfaces;
using Altinn.AccessManagement.Core.Services.Contracts;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.ProblemDetails;
using Altinn.Platform.Register.Models;

namespace Altinn.AccessMgmt.Core.Services;

/// <inheritdoc/>
public class AuthorizedPartyRepoService(
    IDelegationMetadataRepository resourceDelegationRepository,
    IConnectionService connectionService,
    IContextRetrievalService contextRetrievalService
    ) : IAuthorizedPartyRepoService
{
    /// <inheritdoc/>
    public async Task<Result<IEnumerable<AuthorizedParty>>> Get(Guid toId, CancellationToken cancellationToken = default)
    {
        Dictionary<Guid, AuthorizedParty> parties = new();

        // Get AccessPackage Delegations
        var packagePermissions = await connectionService.GetPackagePermissionsFromOthers(toId, null, null, null, cancellationToken: cancellationToken);
        
        // Get App and Resource Delegations
        List<DelegationChange> resourceDelegations = await resourceDelegationRepository.GetAllDelegationChangesForAuthorizedParties(toId.SingleToList(), cancellationToken: cancellationToken);

        // Get Party info for all from-uuids
        var fromUuids = resourceDelegations.Where(d => d.FromUuid.HasValue).Select(d => d.FromUuid.Value).ToList();
        fromUuids.AddRange(packagePermissions.SelectMany(p => p.Permissions).Select(p => p.From.Id));
        var fromParties = await contextRetrievalService.GetPartiesByUuids(fromUuids.Distinct().ToList(), true, cancellationToken);

        EnrichWithAccessPackageParties(parties, packagePermissions, fromParties);
        EnrichWithResourceParties(parties, resourceDelegations, fromParties);

        return parties.Values;
    }

    private static void EnrichWithAccessPackageParties(Dictionary<Guid, AuthorizedParty> parties, IEnumerable<PackagePermissionDto> packagePermissions, Dictionary<string, Party> fromParties)
    {
        foreach (var packagePermission in packagePermissions)
        {
            foreach (var permission in packagePermission.Permissions)
            {
                if (!parties.TryGetValue(permission.From.Id, out AuthorizedParty party))
                {
                    if (!fromParties.TryGetValue(permission.From.Id.ToString(), out Party fromParty))
                    {
                        continue;
                    }

                    party = new AuthorizedParty(fromParty);
                    parties[permission.From.Id] = party;
                }

                party.EnrichWithAccessPackage(packagePermission.Package.Urn.Split(":").Last().SingleToList());
            }
        }
    }

    private static void EnrichWithResourceParties(Dictionary<Guid, AuthorizedParty> parties, List<DelegationChange> resourceDelegations, Dictionary<string, Party> fromParties)
    {
        foreach (DelegationChange delegation in resourceDelegations)
        {
            if (!parties.TryGetValue(delegation.FromUuid.Value, out AuthorizedParty party))
            {
                if (!fromParties.TryGetValue(delegation.FromUuid.ToString(), out Party fromParty))
                {
                    continue;
                }

                party = new AuthorizedParty(fromParty);
                parties[delegation.FromUuid.Value] = party;
            }

            party.EnrichWithResourceAccess(delegation.ResourceId);
        }
    }

    private static AuthorizedParty BuildAuthorizedPartyFromCompactEntity(CompactEntityDto entity)
    {
        var party = new AuthorizedParty
        {
            PartyUuid = entity.Id,
            Name = entity.Name
        };

        if (entity.Type == "Organisasjon")
        {
            party.OrganizationNumber = entity.KeyValues["OrganizationIdentifier"];
            party.Type = AuthorizedPartyType.Organization;
            party.UnitType = entity.Variant;
            
            if (int.TryParse(entity.KeyValues.FirstOrDefault(t => t.Key == "PartyId").Value, out int partyId))
            {
                party.PartyId = partyId;
            }

            if (entity.Children != null)
            {
                foreach (var child in entity.Children.DistinctBy(t => t.Id))
                {
                    var subunit = BuildAuthorizedPartyFromCompactEntity(child);
                    party.Subunits.Add(subunit);
                }
            }
        }
        else
        {
            party.PersonId = entity.KeyValues["PersonIdentifier"];
            party.Type = AuthorizedPartyType.Person;
        }

        return party;
    }

    [DoesNotReturn]
    private static void Unreachable()
    {
        throw new UnreachableException();
    }
}
