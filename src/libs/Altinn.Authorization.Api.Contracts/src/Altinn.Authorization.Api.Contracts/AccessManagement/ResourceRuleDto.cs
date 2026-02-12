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
    /// Unique key for action
    /// </summary>
    public string Key { get; set; }

    /// <summary>
    /// Concatenated key for subresources from policy rule
    /// </summary>
    public string SubResource { get; set; }

    /// <summary>
    /// Action
    /// </summary>
    public string Action { get; set; }

    /// <summary>
    /// Reason
    /// </summary>
    public AccessReason Reason { get; set; }

    /// <summary>
    /// Permissions
    /// </summary>
    public List<PermissionDto> Permissions { get; set; }
}
