using Altinn.AccessManagement.Core.Extensions;

namespace Altinn.AccessMgmt.Core.Models;

/// <summary>
/// ResourceType
/// </summary>
public class ResourceType
{
    /// <summary>
    /// Identity
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Name
    /// </summary>
    public string Name { get; set; }
}
