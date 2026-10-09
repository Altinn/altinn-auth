using System.ComponentModel.DataAnnotations;

namespace Altinn.Authorization.Api.Contracts.Authorization;

/// <summary>
/// Contains attribute match info about user, reportee, resource and resourceMatchType that's being used to check all delegation changes for the resource
/// </summary>
public class DelegationChangeInputDto
{
    /// <summary>
    /// Id and value of the subject getting delegation changes info
    /// </summary>
    [Required]
    public AttributeMatchDto Subject { get; set; }

    /// <summary>
    /// Id and value of party
    /// </summary>
    [Required]
    public AttributeMatchDto Party { get; set; }

    /// <summary>
    /// Gets the Resource's id
    /// </summary>
    [Required]
    public List<AttributeMatchDto> Resource { get; set; }

    /// <summary>
    /// Controls how client-delegated access is considered when looking up delegation changes.
    /// Defaults to <see cref="AccessRestrictionDto.None"/>.
    /// </summary>
    [EnumDataType(typeof(AccessRestrictionDto))]
    public AccessRestrictionDto AccessRestriction { get; set; } = AccessRestrictionDto.None;

    /// <summary>
    /// When <see cref="AccessRestriction"/> is <see cref="AccessRestrictionDto.ClientDelegation"/>, the Norwegian organization number
    /// of the via-party organization through which client-delegated access must have been received.
    /// </summary>
    public string ViaPartyOrganizationNumber { get; set; }
}
