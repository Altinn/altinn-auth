#nullable enable

using System.Globalization;
using Altinn.Urn;

namespace Altinn.ResourceRegistry.Core.Register;

/// <summary>
/// A unique reference to a party in the form of an URN.
/// </summary>
[KeyValueUrn]
public abstract partial record PartyUuidUrn
{
    /// <summary>
    /// Try to get the urn as a party uuid.
    /// </summary>
    /// <param name="partyUuid">The resulting party uuid.</param>
    /// <returns><see langword="true"/> if this party reference is a party uuid, otherwise <see langword="false"/>.</returns>
    [UrnKey("altinn:party:uuid")]
    public partial bool IsPartyUuid(out Guid partyUuid);

    // Manually overridden to disallow negative party ids
    private static bool TryParsePartyId(ReadOnlySpan<char> segment, IFormatProvider? provider, out int value)
        => int.TryParse(segment, NumberStyles.None, provider, out value);
}
