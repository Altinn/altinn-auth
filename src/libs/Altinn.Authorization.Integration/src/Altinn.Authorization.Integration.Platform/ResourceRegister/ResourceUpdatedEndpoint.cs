using System.Text.Json.Serialization;

namespace Altinn.Authorization.Integration.Platform.ResourceRegister;

/// <summary>
/// Client for interacting with endpoint that provides information regarding updated resources.
/// </summary>
public partial class ResourceRegisterClient
{
    /// <inheritdoc/>
    public async Task<IAsyncEnumerable<Paginated<ResourceUpdatedModel>>> StreamResources(string nextPage = null, CancellationToken cancellationToken = default)
    {
        List<Action<HttpRequestMessage>> request = [
            RequestCompositor.WithHttpVerb(HttpMethod.Get),
            RequestCompositor.WithSetUri(Options.Value.Endpoint, "resourceregistry/api/v1/resource/updated"),
            RequestCompositor.WithSetUri(nextPage),
            RequestCompositor.WithAppendQueryParam("limit", 1000),
        ];

        var response = await HttpClient.SendAsync(RequestCompositor.New([.. request]), cancellationToken);

        return new PaginatorStream<ResourceUpdatedModel>(HttpClient, response, request);
    }
}

/// <summary>
/// Represents an updated resource from Altinn Resource Register.
/// </summary>
public class ResourceUpdatedModel
{
    /// <summary>
    /// Gets or sets the subject URN of the resource.
    /// </summary>
    [JsonPropertyName("subjectUrn")]
    public string SubjectUrn { get; set; }

    /// <summary>
    /// Gets or sets the resource URN.
    /// </summary>
    [JsonPropertyName("resourceUrn")]
    public string ResourceUrn { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the update.
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the resource is deleted.
    /// </summary>
    [JsonPropertyName("deleted")]
    public bool Deleted { get; set; }
}
