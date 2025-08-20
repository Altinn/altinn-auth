namespace Altinn.AccessMgmt.Core.Models;

public class PartyBaseInternal
{
    /// <summary>
    /// Gets or sets the unique identifier for the party.
    /// </summary>
    public Guid PartyUuid { get; set; }

    /// <summary>
    /// Gets or sets the type of the PArty Uuid.
    /// </summary>
    public string EntityType { get; set; }

    /// <summary>
    /// Gets or sets the type of the entity variant.
    /// </summary>
    public string EntityVariantType { get; set; }

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who created the entity.
    /// </summary>
    public Guid? CreatedBy { get; set; }
}
