using Altinn.AccessMgmt.FFB.Services.PageData;

namespace Altinn.AccessMgmt.FFB.Components.Shared;

/// <summary>
/// Norwegian display labels for <see cref="EntitySearchKind"/>, shared by the entity
/// search page and the entity lookup drawer.
/// </summary>
public static class EntitySearchKindText
{
    public static string Label(EntitySearchKind kind) => kind switch
    {
        EntitySearchKind.Uuid => "UUID",
        EntitySearchKind.OrganizationNumber => "organisasjonsnummer",
        EntitySearchKind.PersonIdentifier => "fødselsnummer",
        EntitySearchKind.PartyId => "party-id",
        EntitySearchKind.Name => "navn/brukernavn",
        _ => kind.ToString(),
    };
}
