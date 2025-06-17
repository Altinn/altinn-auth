using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Altinn.AccessManagement.Core.Enums;
using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessManagement.Core.Helpers.Extensions;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Repositories.Interfaces;
using Altinn.AccessManagement.Core.Services.Contracts;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Models;
using Altinn.AccessMgmt.Persistence.Repositories.Contracts;
using Altinn.AccessMgmt.Persistence.Services.Contracts;
using Altinn.AccessMgmt.Persistence.Services.Models;
using Altinn.Authorization.ProblemDetails;
using Altinn.Platform.Register.Models;

namespace Altinn.AccessMgmt.Persistence.Services;

/// <inheritdoc/>
public class AuthorizedPartyRepoService(
    IEntityRepository entityRepository,
    IDelegationMetadataRepository resourceDelegationRepository,
    IRelationService relationService,
    IContextRetrievalService contextRetrievalService
    ) : IAuthorizedPartyRepoService
{
    /// <inheritdoc/>
    public async Task<Result<IEnumerable<AuthorizedParty>>> Get(Guid toId, CancellationToken cancellationToken = default)
    {
        Dictionary<Guid, AuthorizedParty> parties = new();
        ValidationErrorBuilder errors = default;

        var toEntity = await entityRepository.GetExtended(toId, cancellationToken: cancellationToken);
        ValidatePartyIsNotNull(toId, toEntity, ref errors, "$QUERY/to");
        ValidatePartyIsSystemUser(toEntity, ref errors, "$QUERY/to");

        if (errors.TryBuild(out var errorResult))
        {
            return errorResult;
        }

        // Get AccessPackage Delegations
        var connections = await relationService.GetConnectionsFromOthers(toId, null, null, null, cancellationToken: cancellationToken);
        EnrichWithAccessPackageParties(parties, connections);

        // Get App and Resource Delegations
        List<DelegationChange> resourceDelegations = await resourceDelegationRepository.GetAllDelegationChangesForAuthorizedParties(toId.SingleToList(), cancellationToken: cancellationToken);
        var fromParties = await contextRetrievalService.GetPartiesByUuids(resourceDelegations.Select(d => d.FromUuid.Value).Distinct().ToList(), true, cancellationToken);

        EnrichWithResourceParties(parties, resourceDelegations, fromParties);

        return parties.Values;
    }

    private static void EnrichWithAccessPackageParties(Dictionary<Guid, AuthorizedParty> parties, IEnumerable<RelationDto> connections)
    {
        foreach (var connection in connections)
        {
            if (!parties.TryGetValue(connection.Party.Id, out AuthorizedParty party))
            {
                party = BuildAuthorizedPartyFromCompactEntity(connection.Party);
                parties[connection.Party.Id] = party;
            }

            var packages = connection.Packages?.Select(cp => cp?.Urn.Split(":").Last());
            party.EnrichWithAccessPackage(packages);
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

    private static AuthorizedParty BuildAuthorizedPartyFromCompactEntity(CompactEntity entity)
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

    private static void ValidatePartyIsNotNull(Guid id, ExtEntity entity, ref ValidationErrorBuilder errors, string param)
    {
        if (entity is null)
        {
            errors.Add(ValidationErrors.EntityNotExists, param, [new("partyId", id.ToString())]);
        }
    }

    private static void ValidatePartyIsSystemUser(ExtEntity entity, ref ValidationErrorBuilder errors, string param)
    {
        if (entity is not null && !entity.Type.Name.Equals("Systembruker", StringComparison.InvariantCultureIgnoreCase))
        {
            errors.Add(ValidationErrors.InvalidQueryParameter, param, [new("partyId", $"expected party of type 'SystemUser' got '{entity.Type.Name}'.")]);
        }
    }

    [DoesNotReturn]
    private static void Unreachable()
    {
        throw new UnreachableException();
    }
}
