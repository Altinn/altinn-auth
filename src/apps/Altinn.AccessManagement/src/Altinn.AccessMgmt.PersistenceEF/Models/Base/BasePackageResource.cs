using Altinn.AccessMgmt.PersistenceEF.Models.Audit.Base;
using Altinn.AccessMgmt.PersistenceEF.Models.Extensions;
using System.ComponentModel.DataAnnotations.Schema;

namespace Altinn.AccessMgmt.PersistenceEF.Models.Base;

/// <summary>
/// PackageResource
/// </summary>
[NotMapped]
public class BasePackageResource : BaseAudit
{
    private Guid _id;

    /// <summary>
    /// Initializes a new instance of the <see cref="BasePackageResource"/> class.
    /// </summary>
    public BasePackageResource()
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
    /// PackageId
    /// </summary>
    public Guid PackageId { get; set; }

    /// <summary>
    /// ResourceId
    /// </summary>
    public Guid ResourceId { get; set; }
}
