using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Altinn.Authorization.Integration.Platform.Register;

/// <summary>
/// Client for interacting with endpoint that stream roles (ER-roles).
/// </summary>
public partial class RegisterClient
{
    /// <inheritdoc/>
    public async Task<IAsyncEnumerable<PlatformResponse<PageStream<RoleModel>>>> StreamRoles(IEnumerable<string> fields, string nextPage = null, CancellationToken cancellationToken = default)
    {
        List<Action<HttpRequestMessage>> request = [
            RequestComposer.WithHttpVerb(HttpMethod.Get),
            RequestComposer.WithSetUri(RegisterOptions.Value.Endpoint, "/register/api/v2/internal/parties/external-roles/assignments/events/stream"),
            RequestComposer.WithSetUri(nextPage),
            RequestComposer.WithAppendQueryParam("fields", fields),
            RequestComposer.WithJWTToken(await TokenGenerator.CreatePlatformAccessToken(cancellationToken))
        ];

        var response = await HttpClient.SendAsync(RequestComposer.New([.. request]), cancellationToken);

        return new PaginatorStream<RoleModel>(HttpClient, response, request);
    }
}

/// <summary>
/// Represents a party with various personal and organizational details.
/// This model is used to deserialize JSON responses containing party information.
/// </summary>
[ExcludeFromCodeCoverage]
public class RoleModel
{
    /// <summary>
    /// Gets or sets the version ID of the role.
    /// </summary>
    [JsonPropertyName("versionId")]
    public int VersionId { get; set; }

    /// <summary>
    /// Gets or sets the type of the role.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; }

    /// <summary>
    /// Gets or sets the source of the role.
    /// </summary>
    [JsonPropertyName("roleSource")]
    public string RoleSource { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the role.
    /// </summary>
    [JsonPropertyName("roleIdentifier")]
    public string RoleIdentifier { get; set; }

    /// <summary>
    /// Gets or sets the party that this role is associated with (recipient).
    /// </summary>
    [JsonPropertyName("toParty")]
    public string ToParty { get; set; }

    /// <summary>
    /// Gets or sets the party that assigned this role (source).
    /// </summary>
    [JsonPropertyName("fromParty")]
    public string FromParty { get; set; }
}
