using Altinn.AccessManagement.Core.Extensions;

namespace Altinn.AccessMgmt.Core.Models;

/// <summary>
/// Delegation between two assignments
/// </summary>
public class Delegation
{
    private Guid _id;

    /// <summary>
    /// Initializes a new instance of the <see cref="Delegation"/> class.
    /// </summary>
    public Delegation()
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
    /// Assignment to delegate from
    /// </summary>
    public Guid FromId { get; set; }

    /// <summary>
    /// Assignment to delegate to
    /// </summary>
    public Guid ToId { get; set; }

    /// <summary>
    /// Entity owner of the Delegation
    /// </summary>
    public Guid FacilitatorId { get; set; }
}

/// <summary>
/// Extended delegation
/// </summary>
public class ExtDelegation : Delegation
{
    /// <summary>
    /// Assignment to delegate from
    /// </summary>
    public Assignment From { get; set; }

    /// <summary>
    /// Assignment to delegate to
    /// </summary>
    public Assignment To { get; set; }

    /// <summary>
    /// Delegation facilitator
    /// </summary>
    public Entity Facilitator { get; set; }
}
