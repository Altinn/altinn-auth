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
}
