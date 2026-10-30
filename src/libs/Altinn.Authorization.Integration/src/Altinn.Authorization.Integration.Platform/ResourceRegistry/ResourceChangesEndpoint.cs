using System.Text.Json.Serialization;

namespace Altinn.Authorization.Integration.Platform.ResourceRegistry;

/// <summary>
/// Client for interacting with endpoint that provides the feed of changed resources.
/// </summary>
public partial class AltinnResourceRegistryClient
{
    /// <inheritdoc/>
    public async Task<IAsyncEnumerable<PlatformResponse<PageStream<ResourceChangeModel>>>> StreamResourceChanges(string nextPage = null, CancellationToken cancellationToken = default)
    {
        List<Action<HttpRequestMessage>> request = [
            RequestComposer.WithHttpVerb(HttpMethod.Get),
            RequestComposer.WithSetUri(ResourceRegistryOptions.Value.Endpoint, "resourceregistry/api/v1/resource/changes"),
            RequestComposer.WithSetUri(nextPage),
            RequestComposer.WithAppendQueryParam("limit", 100),
        ];

        var response = await HttpClient.SendAsync(RequestComposer.New([.. request]), cancellationToken);

        return new PaginatorStream<ResourceChangeModel>(HttpClient, response, request);
    }
}

/// <summary>
/// Represents a resource whose metadata or policy has changed in Altinn Resource Register.
/// </summary>
public class ResourceChangeModel
{
    /// <summary>
    /// Gets or sets the resource identifier, without the <c>urn:altinn:resource:</c> prefix.
    /// </summary>
    [JsonPropertyName("resourceId")]
    public string ResourceId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the latest change.
    /// </summary>
    [JsonPropertyName("changedAt")]
    public DateTimeOffset ChangedAt { get; set; }
}
