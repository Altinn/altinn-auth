namespace Altinn.Authorization.Api.Contracts.AccessManagement;

/// <summary>
/// Resource rules
/// </summary>
public class ResourceRuleDto
{
    /// <summary>
    /// Resource
    /// </summary>
    public ResourceDto Resource { get; set; }

    /// <summary>
    /// Rules
    /// </summary>
    public List<RulePermission> Rules { get; set; }
}

/// <summary>
/// Permissions for rule
/// </summary>
public class RulePermission
{
    /// <summary>
    /// Gets or sets the rule associated with this instance.
    /// </summary>
    public RuleDto Rule { get; set; }

    /// <summary>
    /// Reason
    /// </summary>
    public AccessReason Reason { get; set; }

    /// <summary>
    /// Permissions
    /// </summary>
    public List<PermissionDto> Permissions { get; set; }
}
