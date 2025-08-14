using Altinn.AccessMgmt.PersistenceEF.Models.Audit.Base;
using Altinn.AccessMgmt.PersistenceEF.Models.Extensions;
using System.ComponentModel.DataAnnotations.Schema;

namespace Altinn.AccessMgmt.PersistenceEF.Models.Base;

/// <summary>
/// Lookup for entity
/// </summary>
[NotMapped]
public class BaseEntityLookup : BaseAudit
{
    private Guid _id;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseEntityLookup"/> class.
    /// </summary>
    public BaseEntityLookup()
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
    /// Entity
    /// </summary>
    public Guid EntityId { get; set; }

    /// <summary>
    /// Key (e.g Party,SSN,OrgNo)
    /// </summary>
    public string Key { get; set; }

    /// <summary>
    /// Value
    /// </summary>
    public string Value { get; set; }

    /// <summary>
    /// Data is protected/sensitive
    /// </summary>
    public bool IsProtected { get; set; }
}
