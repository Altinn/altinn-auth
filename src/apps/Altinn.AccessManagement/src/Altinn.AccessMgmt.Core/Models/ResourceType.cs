using Altinn.AccessManagement.Core.Extensions;

namespace Altinn.AccessMgmt.Core.Models;

/// <summary>
/// ResourceType
/// </summary>
public class ResourceType
{
    private Guid _id;

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
    public Guid Id
    {
        get => _id;
        set
        {
            if (!value.IsVersion7Uuid())
            {
                throw new ArgumentException("Id must be a version 7 UUID", nameof(value));
            }

            _id = value;
        }
    }

    /// <summary>
    /// Name
    /// </summary>
    public string Name { get; set; }
}
