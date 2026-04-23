namespace Altinn.AccessManagement.Core.Configuration;

/// <summary>
/// Configuration settings for resource owner delegation package whitelist
/// </summary>
public class ServiceOwnerDelegationSettings
{
    /// <summary>
    /// Gets or sets the whitelist of access packages that service owners are authorized to delegate.
    /// Key is the organization number (from consumer claim in Maskinporten).
    /// Value is the list of access package identifiers that the service owner is authorized to delegate.
    /// </summary>
    public Dictionary<string, List<string>> PackageWhiteList { get; set; } = [];
}
