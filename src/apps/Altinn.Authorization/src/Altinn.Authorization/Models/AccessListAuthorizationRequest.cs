#nullable enable

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Altinn.Authorization.Models;
using Altinn.Authorization.Models.Register;
using Altinn.Authorization.Models.ResourceRegistry;
using Altinn.Urn.Json;

namespace Altinn.Platform.Authorization.Models;

/// <summary>
/// Contains attribute match info about the reportee party and resource that's to be authorized
/// </summary>
public class AccessListAuthorizationRequest
{
    /// <summary>
    /// Gets or sets the attributes identifying the party to be authorized
    /// </summary>
    [Required]
    [JsonRequired]
    public UrnJsonTypeValue<PartyUrn> Subject { get; set; }

    /// <summary>
    /// Gets or sets the attributes identifying the resource to authorize the party for
    /// </summary>
    [Required]
    [JsonRequired]
    public UrnJsonTypeValue<ResourceIdUrn> Resource { get; set; }

    /// <summary>
    /// Gets or sets an optional action value to authorize
    /// </summary>
    public UrnJsonTypeValue<ActionUrn> Action { get; set; }
}
