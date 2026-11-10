#nullable enable

using System.Diagnostics;
using Altinn.ResourceRegistry.Core.Register;

namespace Altinn.ResourceRegistry.Core.AccessLists;

/// <summary>
/// Information about an access list membership.
/// </summary>
/// <param name="PartyUuid">The party uuid.</param>
/// <param name="Since">When the party was added to the access list.</param>
public record AccessListMembership(
    Guid PartyUuid,
    DateTimeOffset Since);

/// <summary>
/// Enriched information about an access list membership.
/// </summary>
/// <param name="PartyIdentifiers">The party identifiers.</param>
/// <param name="Since">When the party was added to the access list.</param>
public record EnrichedAccessListMembership(
    PartyIdentifiers PartyIdentifiers,
    DateTimeOffset Since)
    : AccessListMembership(PartyIdentifiers.PartyUuid, Since)
{
    /// <summary>
    /// Constructs a new <see cref="EnrichedAccessListMembership"/>.
    /// </summary>
    /// <param name="parent">The parent <see cref="AccessListMembership"/>.</param>
    /// <param name="identifiers">The new <see cref="PartyIdentifiers"/>.</param>
    public EnrichedAccessListMembership(AccessListMembership parent, PartyIdentifiers identifiers)
        : this(identifiers, parent.Since)
    {
        Debug.Assert(parent.PartyUuid == identifiers.PartyUuid);
    }
}
