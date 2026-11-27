#nullable enable

namespace Altinn.Authorization.Integration.Platform.Samarbeidsportalen;

/// <summary>
/// Settings for the open metadata API in Samarbeidsportalen (Digdir), used to look up which
/// organization owns a Maskinporten scope prefix. Unlike the other clients in this library, the
/// API is outside Altinn and needs no platform access token.
/// </summary>
public class SamarbeidsportalenOptions
{
    /// <summary>
    /// Full URL to the prefix list, for example <c>https://api.samarbeid.digdir.no/api/v1/prefix/all</c>.
    /// Test environments (AT, YT and TT02) use <c>https://api.test.samarbeid.digdir.no</c>.
    /// </summary>
    public Uri? PrefixEndpoint { get; set; }

    /// <summary>
    /// How long a fetched prefix list is used before it is refreshed. Requests between
    /// <see cref="PrefixCacheStaleAge"/> and <see cref="PrefixCacheExpiredAge"/> still get the
    /// cached list while it is refreshed in the background.
    /// </summary>
    public TimeSpan PrefixCacheStaleAge { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// How long a fetched prefix list may be used at all. After this, requests wait for a
    /// new list, and fail if it cannot be fetched.
    /// </summary>
    public TimeSpan PrefixCacheExpiredAge { get; set; } = TimeSpan.FromHours(1) + TimeSpan.FromMinutes(15);
}
