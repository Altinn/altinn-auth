using Altinn.AccessManagement.Core.Extensions;

namespace Altinn.AccessMgmt.Persistence.Models;

/// <summary>
/// Resources mapped directly to roles
/// </summary>
public class RoleResource
{
    private Guid _id;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoleResource"/> class.
    /// </summary>
    public RoleResource()
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
    /// Role identity
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// Resource identity
    /// </summary>
    public Guid ResourceId { get; set; }
}

/// <summary>
/// Extended Role Resource
/// </summary>
public class ExtRoleResource : RoleResource
{
    /// <summary>
    /// Role
    /// </summary>
    public Role Role { get; set; }

    /// <summary>
    /// Resource
    /// </summary>
    public Resource Resource { get; set; }
}

/// <summary>
/// Extended Role Resource
/// </summary>
public class ExtendedRoleResource : RoleResource
{
    /// <summary>
    /// Role
    /// </summary>
    public ExtendedRole Role { get; set; }

    /// <summary>
    /// Resource
    /// </summary>
    public ExtendedResource Resource { get; set; }
}
