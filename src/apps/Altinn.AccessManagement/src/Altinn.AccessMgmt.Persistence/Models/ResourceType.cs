using Altinn.AccessManagement.Core.Extensions;

namespace Altinn.AccessMgmt.Persistence.Models;

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

/// <summary>
/// Define the types of Resources
/// </summary>
public class ExtResourceType : ResourceType { }

/// <summary>
/// Define the types of Resources
/// </summary>
public class ExtendedResourceType : ResourceType { }
