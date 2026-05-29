using System.ComponentModel.DataAnnotations.Schema;
using Altinn.AccessMgmt.PersistenceEF.Models.Audit.Base;
using Altinn.AccessMgmt.PersistenceEF.Models.Contracts;

namespace Altinn.AccessMgmt.PersistenceEF.Models.Base;

/// <summary>
/// Entity
/// </summary>
[NotMapped]
public class BaseEntity : BaseAudit, IEntityId, IEntityName
{
    /// <summary>
    /// Id
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// TypeId
    /// </summary>
    public Guid TypeId { get; set; }

    /// <summary>
    /// VariantId
    /// </summary>
    public Guid VariantId { get; set; }

    /// <summary>
    /// Name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// RefId
    /// </summary>
    public string RefId { get; set; }

    /// <summary>
    /// Email. Used for ID-porten email login identification
    /// </summary>
    public string EmailIdentifier { get; set; }

    /// <summary>
    /// Parent identifier
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// Gets or sets legacy identifier of the party associated with this entity.
    /// </summary>
    public int? PartyId { get; set; }

    /// <summary>
    /// Gets or sets the legacy identifier associated with this entity if it is of type user.
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Gets or sets the username associated with the entity if it is a user and has a username.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the unique external identifier for the organization associated with this entity.
    /// </summary>
    public string? OrganizationIdentifier { get; set; }

    /// <summary>
    /// Gets or sets the unique external identifier for the person associated with this entity.
    /// </summary>
    public string? PersonIdentifier { get; set; }

    /// <summary>
    /// Gets or sets the date of birth.
    /// </summary>
    public DateOnly? DateOfBirth { get; set; }

    /// <summary>
    /// Gets or sets the date of death for the individual, if the person is dead.
    /// </summary>
    public DateOnly? DateOfDeath { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the entity was deleted.
    /// </summary>
    /// <remarks>A value of <see langword="null"/> indicates that the entity has not been deleted.</remarks>
    public DateTimeOffset? DeletedAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the entity is marked as deleted.
    /// </summary>
    public bool IsDeleted { get; set; }
}
