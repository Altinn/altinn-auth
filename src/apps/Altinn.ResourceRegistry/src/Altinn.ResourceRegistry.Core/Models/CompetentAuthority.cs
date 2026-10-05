namespace Altinn.ResourceRegistry.Core.Models;

/// <summary>
/// Model representation of Competent Authority part of the ServiceResource model
/// </summary>
public class CompetentAuthority
    : CompetentAuthorityReference
{
    /// <summary>
    /// The organization name. If not set it will be retrieved from register based on Organization number
    /// </summary>
    public IReadOnlyDictionary<string, string> Name { get; set; } = new Dictionary<string, string>();
}

/// <summary>
/// Model representation of Competent Authority part of the ServiceResource model without a name
/// </summary>
public class CompetentAuthorityReference
{
    /// <summary>
    /// The organization number
    /// </summary>
    public string Organization { get; set; }

    /// <summary>
    /// The organization code
    /// </summary>
    public string Orgcode { get; set; }
}
