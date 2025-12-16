using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Models.Profile;
using Altinn.AccessManagement.Core.Resolvers;
using Altinn.AccessManagement.Core.Resolvers.Extensions;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.Platform.Profile.Models;

namespace Altinn.AccessManagement.Resolvers;

/// <summary>
/// Resolves Party attribute as a PartyId
/// </summary>
public class PartyAttributeResolver : AttributeResolver
{
    private readonly IContextRetrievalService _contextRetrievalService;
    private readonly IProfileClient _profile;

    /// <summary>
    /// Resolves Party attribute as a PartyId
    /// </summary>
    /// <param name="contextRetrievalService">service init</param>
    /// <param name="profile">profile client</param>
    public PartyAttributeResolver(IContextRetrievalService contextRetrievalService, IProfileClient profile) : base(AltinnXacmlConstants.MatchAttributeIdentifiers.PartyAttribute)
    {
        AddLeaf([AltinnXacmlConstants.MatchAttributeIdentifiers.PartyAttribute], [AltinnXacmlConstants.MatchAttributeIdentifiers.OrganizationUuid, AltinnXacmlConstants.MatchAttributeIdentifiers.PersonUuid], ResolvePartyIdOrganizationPersonUuidFromParty());
        AddLeaf([AltinnXacmlConstants.MatchAttributeIdentifiers.UserAttribute], [AltinnXacmlConstants.MatchAttributeIdentifiers.PartyAttribute, AltinnXacmlConstants.MatchAttributeIdentifiers.PersonUuid], ResolvePartyIdPersonUuidFromUser());

        _contextRetrievalService = contextRetrievalService;
        _profile = profile;
    }

    /// <summary>
    /// Resolves a PartyId if given <see cref="AltinnXacmlConstants.MatchAttributeIdentifiers.UserAttribute"/> exists
    /// </summary>
    public LeafResolver ResolvePartyIdPersonUuidFromUser() => async (attributes, cancellationToken) =>
    {
        NewUserProfile user = await _profile.GetUser(
            new() { UserId = attributes.GetRequiredInt(AltinnXacmlConstants.MatchAttributeIdentifiers.UserAttribute) },
            cancellationToken);

        if (user != null)
        {
            return
            [
                new(AltinnXacmlConstants.MatchAttributeIdentifiers.PartyAttribute, user.Party.PartyId),
                new(AltinnXacmlConstants.MatchAttributeIdentifiers.PersonUuid, user.UserUuid),
            ];
        }

        return [];
    };

    /// <summary>
    /// Resolves a PartyId if given <see cref="AltinnXacmlConstants.MatchAttributeIdentifiers.PartyAttribute"/> exists
    /// </summary>
    public LeafResolver ResolvePartyIdOrganizationPersonUuidFromParty() => async (attributes, cancellationToken) =>
    {
        if (await _contextRetrievalService.GetPartyAsync(attributes.GetRequiredInt(AltinnXacmlConstants.MatchAttributeIdentifiers.PartyAttribute), cancellationToken) is var party && party != null)
        {
            if (party.Organization != null)
            {
                return
                [
                    new(AltinnXacmlConstants.MatchAttributeIdentifiers.OrganizationUuid, party.PartyUuid),
                ];
            }
            else if (party.Person != null)
            {
                return
                [
                    new(AltinnXacmlConstants.MatchAttributeIdentifiers.PersonUuid, party.PartyUuid),
                ];
            }
        }

        return [];
    };
}
