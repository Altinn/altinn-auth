#nullable enable

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Altinn.Authorization.Models.Register;

namespace Altinn.Authorization.Models.ResourceRegistry;

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
}
