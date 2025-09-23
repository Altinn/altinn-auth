namespace Altinn.Authorization.Api.Contracts.AccessManagement;

/// <summary>
/// Compact Role Model
/// </summary>
public class CompactRoleDto
{
    /// <summary>
    /// Id
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Value
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// Children
    /// </summary>
    public List<CompactRoleDto> Children { get; set; }
}
