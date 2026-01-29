using Altinn.Register.Contracts;

namespace Altinn.Authorization.Integration.Platform.Register;

/// <summary>
/// Client for interacting with endpoint that stream parties (organization, users and systemusers).
/// </summary>
public partial class AltinnRegisterClient
{
    /// <summary>
    /// List of available fields that can be retrieved from the register.
    /// </summary>
    public static readonly IEnumerable<string> DefaultFields = [
        "identifiers",
        "type",
        "display-name",
        "deleted",
        "deleted-at",
        "user",
        "person.date-of-birth",
        "person.date-of-death",
        "sysuser.type",
        "org.type",
        "si.type",
        "si.email"
    ];

    /// <inheritdoc/>
    public async Task<IAsyncEnumerable<PlatformResponse<PageStream<Party>>>> StreamParties(IEnumerable<string> fields, string nextPage = null, CancellationToken cancellationToken = default)
    {
        IEnumerable<Action<HttpRequestMessage>> request = [
            RequestComposer.WithHttpVerb(HttpMethod.Get),
            RequestComposer.WithSetUri(RegisterOptions.Value.Endpoint, "/register/api/v2/internal/parties/stream"),
            RequestComposer.WithSetUri(nextPage),
            RequestComposer.WithAppendQueryParam("fields", fields),
            RequestComposer.WithPlatformAccessToken(async () => await TokenGenerator.CreatePlatformAccessToken(cancellationToken))
        ];

        var response = await HttpClient.SendAsync(RequestComposer.New([.. request]), cancellationToken);

        return new PaginatorStream<Party>(HttpClient, response, request);
    }
}
