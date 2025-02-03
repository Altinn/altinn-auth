using System.Net.Http.Json;
using System.Web;
using Altinn.Authorization.Integration.Register.Models;

namespace Altinn.Authorization.Integration.Register;

/// <inheritdoc/>
public partial class RegisterClient : IAltinnRegister
{
    /// <summary>
    /// List of available fields that can be retrieved from the register.
    /// </summary>
    private readonly IEnumerable<string> _availableFields = [
        "party",
        "organization",
        "person",
        "identifiers",
        "party-uuid",
        "party-version-id",
        "party-is-deleted",
        "organization-business-address",
        "organization-mailing-address",
        "organization-internet-address",
        "organization-email-address",
        "organization-fax-number",
        "organization-mobile-number",
        "organization-telephone-number",
        "organization-unit-type",
        "organization-unit-status",
        "person-date-of-death",
        "person-mailing-address",
        "person-address",
        "person-last-name",
        "person-middle-name",
        "person-first-name",
        "party-modified-at",
        "party-created-at",
        "party-organization-identifier",
        "party-person-identifier",
        "party-name",
        "party-type",
        "party-id",
        "person-date-of-birth",
        "sub-units"
    ];

    /// <inheritdoc/>
    public async Task<IAsyncEnumerable<Paginated<PartyModel>>> Stream(string nextPage, IEnumerable<string> fields, CancellationToken cancellationToken)
    {
        var request = NewStreamRequest(nextPage);
        var response = await HttpClient.SendAsync(request, cancellationToken);
        return new PartyStream(response, HttpClient, NewStreamRequest);
    }

    /// <summary>
    /// Creates a new HTTP request for streaming register data.
    /// </summary>
    /// <param name="nextPage">The next page URL.</param>
    /// <param name="fields">Fields to request.</param>
    private HttpRequestMessage NewStreamRequest(string nextPage, IEnumerable<string> fields = null)
    {
        fields ??= [];
        if (!fields.Distinct().All(_availableFields.Contains))
        {
            throw new ArgumentException("Some or all provided fields is not retrievable from register", nameof(fields));
        }

        var request = new HttpRequestMessage(HttpMethod.Get, nextPage);
        if (string.IsNullOrEmpty(nextPage))
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            if (fields.Any())
            {
                query["fields"] = string.Join(",", fields);
            }

            var uri = new Uri(Options.Value.Endpoint, "register/api/v2/parties/stream");
            request.RequestUri = new UriBuilder(uri)
            {
                Query = query.ToString()
            }.Uri;
        }

        AddAuthorization(request);

        return request;
    }

    /// <summary>
    /// Represents a stream of party data from the Altinn Register.
    /// </summary>
    /// <param name="response">Initial HTTP response.</param>
    /// <param name="httpClient">HTTP client for making requests.</param>
    /// <param name="newRequest">Function for creating new requests.</param>
    public class PartyStream(HttpResponseMessage response, HttpClient httpClient, Func<string, IEnumerable<string>, HttpRequestMessage> newRequest) : IAsyncEnumerable<Paginated<PartyModel>>
    {
        private HttpResponseMessage Response { get; } = response;

        private HttpClient HttpClient { get; } = httpClient;

        private Func<string, IEnumerable<string>, HttpRequestMessage> NewRequest { get; } = newRequest;

        /// <inheritdoc/>
        public async IAsyncEnumerator<Paginated<PartyModel>> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            var pageResponse = Response;
            while (!cancellationToken.IsCancellationRequested)
            {
                if (pageResponse.IsSuccessStatusCode)
                {
                    var content = await pageResponse.Content.ReadFromJsonAsync<Paginated<PartyModel>>(cancellationToken);
                    var nextPage = FetchNextPage(content, cancellationToken);

                    if (content.Items.Any())
                    {
                        yield return content;
                    }

                    pageResponse.Dispose();
                    pageResponse = await nextPage;
                    if (pageResponse == null)
                    {
                        yield break;
                    }
                }
                else
                {
                    yield break;
                }
            }
        }

        /// <summary>
        /// Fetches the next page of data from the register.
        /// </summary>
        /// <param name="content">Current page content.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        private async Task<HttpResponseMessage> FetchNextPage(Paginated<PartyModel> content, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(content.Links.Next))
            {
                return null;
            }

            var request = NewRequest(content.Links.Next, []);
            return await HttpClient.SendAsync(request, cancellationToken);
        }
    }
}

/// <summary>
/// Interface defining operations for the Altinn Register service.
/// </summary>
public partial interface IAltinnRegister
{
    /// <summary>
    /// Create Enumeratres that streams pages for Altinn Register
    /// </summary>
    /// <param name="nextPage">URL for the next page, or null to start from the beginning.</param>
    /// <param name="fields">Fields to retrieve from the register.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IAsyncEnumerable<Paginated<PartyModel>>> Stream(string nextPage, IEnumerable<string> fields, CancellationToken cancellationToken);
}
