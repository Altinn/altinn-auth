using Altinn.Authorization.Api.Contracts.AccessManagement.Enums;

namespace Altinn.AccessManagement.Core.Models;

/// <summary>
/// Filter inputs for authorized parties controller.
/// </summary>
public class AuthorizedPartiesFilters
{
    public bool IncludeAltinn2 { get; set; } = true;

    public bool IncludeAltinn3 { get; set; } = true;

    public bool IncludeRoles { get; set; } = true;

    public bool IncludeAccessPackages { get; set; } = false;

    public bool IncludeResources { get; set; } = true;

    public bool IncludeInstances { get; set; } = true;

    public AuthorizedPartiesIncludeFilter IncludePartiesViaKeyRoles { get; set; } = AuthorizedPartiesIncludeFilter.True;

    public AuthorizedPartiesIncludeFilter IncludeSubParties { get; set; } = AuthorizedPartiesIncludeFilter.True;

    public AuthorizedPartiesIncludeFilter IncludeInactiveParties { get; set; } = AuthorizedPartiesIncludeFilter.True;

    public SortedDictionary<Guid, Guid>? PartyFilter { get; set; } = null;

    public SortedDictionary<string, string>? RoleFilter { get; set; } = null;

    public SortedDictionary<Guid, Guid>? PackageFilter { get; set; } = null;

    public SortedDictionary<string, string> ResourceFilter { get; set; } = null;

    public string ProviderCode { get; set; } = null;

    public string[] AnyOfRoleIds { get; set; } = null;

    public string[] AllOfRoleIds { get; set; } = null;

    public string[] AnyOfPackageIds { get; set; } = null;

    public string[] AllOfPackageIds { get; set; } = null;

    public string[] AnyOfResourceIds { get; set; } = null;

    public string[] AllOfResourceIds { get; set; } = null;

    public string[] AnyOfInstanceIds { get; set; } = null;

    public string[] AllOfInstanceIds { get; set; } = null;
}
