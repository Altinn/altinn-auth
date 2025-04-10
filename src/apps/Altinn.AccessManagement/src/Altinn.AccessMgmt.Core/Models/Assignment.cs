using Altinn.AccessManagement.Core.Extensions;

namespace Altinn.AccessMgmt.Core.Models;

/// <summary>
/// Assignment
/// </summary>
public class Assignment
{
    private Guid _id;

    /// <summary>
    /// Initializes a new instance of the <see cref="Assignment"/> class.
    /// </summary>
    public Assignment()
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
    /// RoleId
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// FromId
    /// </summary>
    public Guid FromId { get; set; }

    /// <summary>
    /// ToId
    /// </summary>
    public Guid ToId { get; set; }
}

/// <summary>
/// Extended Assignment
/// </summary>
public class ExtAssignment : Assignment
{
    /// <summary>
    /// Role
    /// </summary>
    public Role Role { get; set; }

    /// <summary>
    /// ActiveFrom (Entity)
    /// </summary>
    public Entity From { get; set; }

    /// <summary>
    /// ActiveTo (Entity)
    /// </summary>
    public Entity To { get; set; }
}
