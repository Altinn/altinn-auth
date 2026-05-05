namespace Altinn.Authorization.Api.Contracts.AccessManagement;

/// <summary>
/// Instance permissions
/// </summary>
public class InstancePermissionDto
{
    /// <summary>
    /// Resource the permissions are for
    /// </summary>
    public ResourceDto Resource { get; set; }

    /// <summary>
    /// Instance the permissions are for
    /// </summary>
    public InstanceDto Instance { get; set; }

    /// <summary>
    /// Parties with permissions
    /// </summary>
    public IEnumerable<PermissionDto> Permissions { get; set; }
}
