namespace Altinn.AccessMgmt.Core.Models;

/// <summary>
/// ResourceType
/// </summary>
public class ResourceType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceType"/> class.
    /// </summary>
    public ResourceType()
    {
        Id = Guid.CreateVersion7();
    }

    /// <summary>
    /// Identity
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Name
    /// </summary>
    public string Name { get; set; }
}
