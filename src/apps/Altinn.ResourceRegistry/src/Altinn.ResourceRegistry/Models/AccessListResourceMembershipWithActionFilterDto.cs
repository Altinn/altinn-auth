#nullable enable

using System.Text.Json.Serialization;
using Altinn.ResourceRegistry.Core.AccessLists;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.Register;

namespace Altinn.ResourceRegistry.Models;

/// <summary>
/// Represents a party's membership of a access list connected to a specific resource with an optional set of action filters.
/// </summary>
/// <param name="Party">The party UUID.</param>
/// <param name="Resource">The resource id.</param>
/// <param name="Since">Since when this party has been a member of the list connected to the party.</param>
/// <param name="ActionFilters">Optional set of action filters.</param>
public record AccessListResourceMembershipWithActionFilterDto(
    PartyUrn.PartyUuid Party,
    ResourceUrn.ResourceId Resource,
    DateTimeOffset Since,
    IReadOnlyCollection<string>? ActionFilters)
{
    /// <summary>
    /// Gets the allowed actions or <see langword="null"/> if all actions are allowed.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyCollection<string>? ActionFilters { get; }
        = ActionFilters is null or { Count: 0 } ? null : ActionFilters;

    /// <summary>
    /// Creates a new <see cref="AccessListResourceMembershipWithActionFilterDto"/> from an <see cref="AccessListMembership"/> and 
    /// an <see cref="AccessListResourceConnection"/>.
    /// </summary>
    /// <param name="resourceConnection">The resource connection.</param>
    /// <param name="membership">The list membership.</param>
    /// <returns>A <see cref="AccessListResourceMembershipWithActionFilterDto"/>.</returns>
    public static AccessListResourceMembershipWithActionFilterDto From(AccessListResourceConnection resourceConnection, AccessListMembership membership)
        => new(
            PartyUrn.PartyUuid.Create(membership.PartyUuid),
            ResourceUrn.ResourceId.Create(ResourceIdentifier.CreateUnchecked(resourceConnection.ResourceIdentifier)),
            membership.Since,
            resourceConnection.Actions);

    /// <summary>
    /// Creates a new <see cref="AccessListResourceMembershipWithActionFilterDto"/> from an <see cref="AccessListMembership"/> and 
    /// an <see cref="AccessListResourceConnection"/>.
    /// </summary>
    /// <param name="kvp">An <see cref="AccessListMembership"/> and an <see cref="AccessListResourceConnection"/>.</param>
    /// <returns>A <see cref="AccessListResourceMembershipWithActionFilterDto"/>.</returns>
    public static AccessListResourceMembershipWithActionFilterDto From(KeyValuePair<AccessListResourceConnection, AccessListMembership> kvp)
        => From(kvp.Key, kvp.Value);
}
