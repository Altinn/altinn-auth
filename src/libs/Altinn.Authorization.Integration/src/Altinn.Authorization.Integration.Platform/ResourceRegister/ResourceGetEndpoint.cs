using System.Text.Json.Serialization;

namespace Altinn.Authorization.Integration.Platform.ResourceRegister;

/// <summary>
/// Client for interacting with the Altinn Resource Register API.
/// Provides methods to retrieve resource information.
/// </summary>
public partial class ResourceRegisterClient
{
    /// <inheritdoc/>
    public async Task<PlatformResponse<ResourceModel>> GetResource(string id, CancellationToken cancellationToken = default)
    {
        List<Action<HttpRequestMessage>> request = [
            RequestComposer.WithHttpVerb(HttpMethod.Get),
            RequestComposer.WithSetUri(ResourceRegisterOptions.Value.Endpoint, "/resourceregistry/api/v1/resource", id),
        ];

        var response = await HttpClient.SendAsync(RequestComposer.New([.. request]), cancellationToken);

        return ResponseComposer.Handle<ResourceModel>(
            response,
            ResponseComposer.DeserializeProblemDetailsOnUnsuccessStatusCode,
            ResponseComposer.DeserializeResponseOnSuccess
        );
    }

    public async Task<PlatformResponse<List<ResourceModel>>> GetResources(CancellationToken cancellationToken = default)
    {
        List<Action<HttpRequestMessage>> request = [
            RequestComposer.WithHttpVerb(HttpMethod.Get),
            RequestComposer.WithSetUri(ResourceRegisterOptions.Value.Endpoint, "/resourceregistry/api/v1/resource/resourcelist"),
        ];

        var response = await HttpClient.SendAsync(RequestComposer.New([.. request]), cancellationToken);

        return ResponseComposer.Handle<List<ResourceModel>>(
            response,
            ResponseComposer.DeserializeProblemDetailsOnUnsuccessStatusCode,
            ResponseComposer.DeserializeResponseOnSuccess
        );
    }
}

/// <summary>
/// Represents a resource retrieved from the Altinn Resource Register.
/// </summary>
public class ResourceModel
{
    /// <summary>
    /// Gets or sets the unique identifier for the resource.
    /// </summary>
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; }

    /// <summary>
    /// Gets or sets the title of the resource in multiple languages.
    /// </summary>
    [JsonPropertyName("title")]
    public ResourceTitle Title { get; set; }

    /// <summary>
    /// Gets or sets the description of the resource in multiple languages.
    /// </summary>
    [JsonPropertyName("description")]
    public ResourceDescription Description { get; set; }

    /// <summary>
    /// Gets or sets the description of rights associated with the resource.
    /// </summary>
    [JsonPropertyName("rightDescription")]
    public ResourceRightDescription RightDescription { get; set; }

    /// <summary>
    /// Gets or sets the homepage URL for the resource.
    /// </summary>
    [JsonPropertyName("homepage")]
    public string Homepage { get; set; }

    /// <summary>
    /// Gets or sets the current status of the resource.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; }

    /// <summary>
    /// Gets or sets a list of contact points related to the resource.
    /// </summary>
    [JsonPropertyName("contactPoints")]
    public List<object> ContactPoints { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the resource this one is part of.
    /// </summary>
    [JsonPropertyName("isPartOf")]
    public string IsPartOf { get; set; }

    /// <summary>
    /// Gets or sets a list of references related to the resource.
    /// </summary>
    [JsonPropertyName("resourceReferences")]
    public List<ResourceReference> ResourceReferences { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the resource can be delegated.
    /// </summary>
    [JsonPropertyName("delegable")]
    public bool Delegable { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the resource is visible.
    /// </summary>
    [JsonPropertyName("visible")]
    public bool Visible { get; set; }

    /// <summary>
    /// Gets or sets the competent authority responsible for the resource.
    /// </summary>
    [JsonPropertyName("hasCompetentAuthority")]
    public ResourceHasCompetentAuthority HasCompetentAuthority { get; set; }

    /// <summary>
    /// Gets or sets a list of keywords associated with the resource.
    /// </summary>
    [JsonPropertyName("keywords")]
    public List<object> Keywords { get; set; }

    /// <summary>
    /// Gets or sets the access list mode for the resource.
    /// </summary>
    [JsonPropertyName("accessListMode")]
    public string AccessListMode { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether self-identified users can access the resource.
    /// </summary>
    [JsonPropertyName("selfIdentifiedUserEnabled")]
    public bool SelfIdentifiedUserEnabled { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether enterprise users can access the resource.
    /// </summary>
    [JsonPropertyName("enterpriseUserEnabled")]
    public bool EnterpriseUserEnabled { get; set; }

    /// <summary>
    /// Gets or sets the type of the resource.
    /// </summary>
    [JsonPropertyName("resourceType")]
    public string ResourceType { get; set; }

    /// <summary>
    /// Gets or sets a list of authorization references associated with the resource.
    /// </summary>
    [JsonPropertyName("authorizationReference")]
    public List<ResourceAuthorizationReference> AuthorizationReference { get; set; }
}

/// <summary>
/// Represents an authorization reference for a resource.
/// </summary>
public class ResourceAuthorizationReference
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; }
}

/// <summary>
/// Represents a description of a resource in multiple languages.
/// </summary>
public class ResourceDescription
{
    [JsonPropertyName("en")]
    public string En { get; set; }

    [JsonPropertyName("nb")]
    public string Nb { get; set; }

    [JsonPropertyName("nn")]
    public string Nn { get; set; }
}

/// <summary>
/// Represents the competent authority responsible for a resource.
/// </summary>
public class ResourceHasCompetentAuthority
{
    /// <summary>
    /// E.g. Norwegian Tax Administration
    /// </summary>
    [JsonPropertyName("name")]
    public CompetentAuthorityName Name { get; set; }

    /// <summary>
    /// E.g. 974761076
    /// </summary>
    [JsonPropertyName("organization")]
    public string Organization { get; set; }

    /// <summary>
    /// E.g. skd
    /// </summary>
    [JsonPropertyName("orgcode")]
    public string Orgcode { get; set; }

    /// <summary>
    /// Represents the name of the competent authority in multiple languages.
    /// </summary>
    public class CompetentAuthorityName
    {
        [JsonPropertyName("en")]
        public string En { get; set; }

        [JsonPropertyName("nb")]
        public string Nb { get; set; }

        [JsonPropertyName("nn")]
        public string Nn { get; set; }
    }
}

/// <summary>
/// Represents a reference related to a resource.
/// </summary>
public class ResourceReference
{
    [JsonPropertyName("referenceSource")]
    public string ReferenceSource { get; set; }

    [JsonPropertyName("reference")]
    public string Reference { get; set; }

    [JsonPropertyName("referenceType")]
    public string ReferenceType { get; set; }
}

/// <summary>
/// Represents a right description of a resource in multiple languages.
/// </summary>
public class ResourceRightDescription
{
    [JsonPropertyName("en")]
    public string En { get; set; }

    [JsonPropertyName("nb")]
    public string Nb { get; set; }

    [JsonPropertyName("nn")]
    public string Nn { get; set; }
}

/// <summary>
/// Represents the title of a resource in multiple languages.
/// </summary>
public class ResourceTitle
{
    [JsonPropertyName("en")]
    public string En { get; set; }

    [JsonPropertyName("nb")]
    public string Nb { get; set; }

    [JsonPropertyName("nn")]
    public string Nn { get; set; }
}
