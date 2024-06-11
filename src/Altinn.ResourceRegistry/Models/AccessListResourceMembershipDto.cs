#nullable enable

using Altinn.ResourceRegistry.Core.AccessLists;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.Register;

namespace Altinn.ResourceRegistry.Models;

/// <summary>
/// Represents a party's membership of a access list connected to a specific resource.
/// </summary>
/// <param name="Party">The party uuid.</param>
/// <param name="Resource">The resource id.</param>
/// <param name="Since">Since when the party has been a member of the access list.</param>
public record AccessListResourceMembershipDto(
    PartyUrn.PartyUuid Party,
    ResourceUrn.ResourceId Resource,
    DateTimeOffset Since)
{
    /// <summary>
    /// Creates a new <see cref="AccessListResourceMembershipDto"/> from an <see cref="AccessListMembership"/>.
    /// </summary>
    /// <param name="resourceConnection">The <see cref="AccessListResourceConnection"/>.</param>
    /// <param name="membership">The <see cref="AccessListMembership"/>.</param>
    /// <returns>A <see cref="AccessListResourceMembershipDto"/>.</returns>
    public static AccessListResourceMembershipDto From(AccessListResourceConnection resourceConnection, AccessListMembership membership)
        => new(
            PartyUrn.PartyUuid.Create(membership.PartyUuid), 
            ResourceUrn.ResourceId.Create(ResourceIdentifier.CreateUnchecked(resourceConnection.ResourceIdentifier)),
            membership.Since);

    /// <summary>
    /// Creates a new <see cref="AccessListResourceMembershipDto"/> from a <see cref="KeyValuePair{AccessListResourceConnection, AccessListMembership}"/>.
    /// </summary>
    /// <param name="kvp">The <see cref="KeyValuePair{AccessListResourceConnection, AccessListMembership}"/>.</param>
    /// <returns>A <see cref="AccessListResourceMembershipDto"/>.</returns>
    public static AccessListResourceMembershipDto From(KeyValuePair<AccessListResourceConnection, AccessListMembership> kvp)
        => From(kvp.Key, kvp.Value);
}
