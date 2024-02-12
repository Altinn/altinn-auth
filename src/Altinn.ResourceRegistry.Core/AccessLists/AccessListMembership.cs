#nullable enable

namespace Altinn.ResourceRegistry.Core.AccessLists;

/// <summary>
/// Information about an access list membership.
/// </summary>
/// <param name="PartyId">The party id.</param>
/// <param name="Since">When the party was added to the access list.</param>
public record AccessListMembership(
    Guid PartyId,
    DateTimeOffset Since);
