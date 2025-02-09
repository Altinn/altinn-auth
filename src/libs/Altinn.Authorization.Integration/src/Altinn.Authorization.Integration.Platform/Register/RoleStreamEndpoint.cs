using System.Diagnostics.CodeAnalysis;

namespace Altinn.Authorization.Integration.Platform.Register;

/// <summary>
/// Client for interacting with endpoint that stream roles (ER-roles).
/// </summary>
public partial class RegisterClient
{
    /// <inheritdoc/>
    public async Task<IAsyncEnumerable<Paginated<RoleModel>>> StreamRoles(IEnumerable<string> fields, string nextPage = null, CancellationToken cancellationToken = default)
    {
        List<Action<HttpRequestMessage>> request = [
            RequestCompositor.WithHttpVerb(HttpMethod.Get),
            RequestCompositor.WithSetUri(Options.Value.Endpoint, "register/api/v2/roles/stream"),
            RequestCompositor.WithSetUri(nextPage),
            RequestCompositor.WithAppendQueryParam("fields", fields),
            RequestCompositor.WithPlatformAccessToken(AccessTokenGenerator, "access-management")
        ];

        var response = await HttpClient.SendAsync(RequestCompositor.New([.. request]), cancellationToken);

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
}
