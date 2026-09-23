#nullable enable

namespace Altinn.ResourceRegistry.Core.Register;

/// <summary>
/// A client for the register.
/// </summary>
public interface IRegisterClient
{
    /// <summary>
    /// Get party identifiers for the given party urns.
    /// </summary>
    /// <param name="parties">The party references.</param>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
    /// <returns>An async enumerable of <see cref="PartyIdentifiers"/>.</returns>
    IAsyncEnumerable<PartyIdentifiers> GetPartyIdentifiers(IEnumerable<PartyUrn> parties, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get party identifiers for the given party uuids.
    /// </summary>
    /// <param name="partyUuids">The party uuids.</param>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
    /// <returns>An async enumerable of <see cref="PartyIdentifiers"/>.</returns>
    IAsyncEnumerable<PartyIdentifiers> GetPartyIdentifiers(IEnumerable<Guid> partyUuids, CancellationToken cancellationToken = default);
}
