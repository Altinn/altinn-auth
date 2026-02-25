namespace Altinn.Authorization.Api.Contracts.AccessManagement;

/// <summary>
/// Resource rights
/// </summary>
public class ResourceRightDto
{
    /// <summary>
    /// Resource
    /// </summary>
    public ResourceDto Resource { get; set; }

    /// <summary>
    /// Rights
    /// </summary>
    public List<RightPermission> Rights { get; set; }
}

/// <summary>
/// Permissions for right
/// </summary>
public class RightPermission
{
    /// <summary>
    /// Gets or sets the right associated with this instance.
    /// </summary>
    public RightDto Right { get; set; }

    /// <summary>
    /// Reason
    /// </summary>
    public AccessReason Reason { get; set; }

    /// <summary>
    /// Permissions
    /// </summary>
    public List<PermissionDto> Permissions { get; set; }
}

/// <summary>
/// Resource rights
/// </summary>
public class ExternalResourceRightDto
{
    /// <summary>
    /// Resource
    /// </summary>
    public ResourceDto Resource { get; set; }

    /// <summary>
    /// Rights
    /// </summary>
    public List<RightPermission> DirectRights { get; set; }

    /// <summary>
    /// Rights
    /// </summary>
    public List<RightPermission> IndirectRights { get; set; }
}
