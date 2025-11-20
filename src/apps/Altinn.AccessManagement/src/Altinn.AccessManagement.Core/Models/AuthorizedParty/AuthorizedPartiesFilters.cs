namespace Altinn.AccessManagement.Core.Models;

/// <summary>
/// Filter inputs for authorized parties controller.
/// </summary>
public class AuthorizedPartiesFilters
{
    public bool IncludeAltinn2 { get; set; } = true;

    public bool IncludeAltinn3 { get; set; } = true;

    public Dictionary<Guid, Guid>? PartyFilter { get; set; } = null;

    public bool IncludeRoles { get; set; } = true;

    public bool IncludeAccessPackages { get; set; } = false;

    public bool IncludeResources { get; set; } = true;

    public bool IncludeInstances { get; set; } = true;

    public bool IncludePartiesViaKeyRoles { get; set; } = true;

    /* Future filters to implement
    public string ProviderCode { get; set; } = null;

    public string[] AnyOfResourceIds { get; set; } = null;

    public string[] AllOfResourceIds { get; set; } = null;
    */
}
