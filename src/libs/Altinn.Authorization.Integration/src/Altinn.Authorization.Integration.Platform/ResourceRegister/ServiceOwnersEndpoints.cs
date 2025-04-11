using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace Altinn.Authorization.Integration.Platform.ResourceRegister;

/// <summary>
/// Client for interacting with the Altinn Resource Register API.
/// Provides methods to service owners.
/// </summary>
public partial class ResourceRegisterClient
{
    /// <inheritdoc/>
    public async Task<PlatformResponse<ServiceOwners>> GetServiceOwners(CancellationToken cancellationToken = default)
    {
        List<Action<HttpRequestMessage>> request = [
            RequestComposer.WithHttpVerb(HttpMethod.Get),
            RequestComposer.WithSetUri(ResourceRegisterOptions.Value.Endpoint, "resourceregistry/api/v1/resource/orgs"),
        ];

        var response = await HttpClient.SendAsync(RequestComposer.New([.. request]), cancellationToken);

        return ResponseComposer.Handle(
            response,
            ResponseComposer.DeserializeProblemDetailsOnUnsuccessStatusCode,
            ResponseComposer.DeserializeResponseOnSuccess,
            ResponseComposer.ConfigureResultIfSuccessful<ServiceOwners>(content =>
            {
                foreach (var org in content.Orgs)
                {
                    var hash = MD5.HashData(Encoding.UTF8.GetBytes(org.Key));
                    org.Value.Id = new Guid(hash);
                }
            })
        );
    }
}

public class ServiceOwners
{
    [JsonPropertyName("orgs")]
    public IDictionary<string, ServiceOwner> Orgs { get; set; }
}

public class ServiceOwnerName
{
    [JsonPropertyName("en")]
    public string En { get; set; }

    [JsonPropertyName("nb")]
    public string Nb { get; set; }

    [JsonPropertyName("nn")]
    public string Nn { get; set; }
}

public class ServiceOwner
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.Empty;

    [JsonPropertyName("name")]
    public ServiceOwnerName Name { get; set; }

    [JsonPropertyName("logo")]
    public string Logo { get; set; }

    [JsonPropertyName("orgnr")]
    public string Orgnr { get; set; }

    [JsonPropertyName("homepage")]
    public string Homepage { get; set; }

    [JsonPropertyName("environments")]
    public List<string> Environments { get; set; }
}
