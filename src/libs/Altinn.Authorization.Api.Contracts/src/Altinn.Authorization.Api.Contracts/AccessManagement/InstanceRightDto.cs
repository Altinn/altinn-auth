namespace Altinn.Authorization.Api.Contracts.AccessManagement;

/// <summary>
/// Instance rights
/// </summary>
public class InstanceRightDto
{
    /// <summary>
    /// Resource
    /// </summary>
    public ResourceDto Resource { get; set; }

    /// <summary>
    /// Instance
    /// </summary>
    public InstanceDto Instance { get; set; }

    /// <summary>
    /// Rights
    /// </summary>
    public List<RightPermission> Rights { get; set; }
}

/// <summary>
/// External Dto for instance rights
/// </summary>
public class ExtInstanceRightDto
{
    /// <summary>
    /// Resource
    /// </summary>
    public ResourceDto Resource { get; set; }

    /// <summary>
    /// Instance
    /// </summary>
    public InstanceDto Instance { get; set; }

    /// <summary>
    /// Direct Rights
    /// </summary>
    public List<RightPermission> DirectRights { get; set; }

    /// <summary>
    /// Indirect Rights (access through key-role parties or through main unit)
    /// </summary>
    public List<RightPermission> IndirectRights { get; set; }
}
