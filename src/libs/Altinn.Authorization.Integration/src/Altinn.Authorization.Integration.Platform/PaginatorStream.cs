using System.Text.Json.Serialization;

namespace Altinn.Authorization.Integration.Platform;

/// <summary>
/// Handles asynchronous pagination of HTTP responses.
/// </summary>
/// <typeparam name="T">The type of items being paginated.</typeparam>
/// <param name="httpClient">The HTTP client to make requests.</param>
/// <param name="currentResponse">The current HTTP response message.</param>
/// <param name="newRequest">The request actions for fetching the next page.</param>
public class PaginatorStream<T>(HttpClient httpClient, HttpResponseMessage currentResponse, IEnumerable<Action<HttpRequestMessage>> newRequest) : IAsyncEnumerable<PlatformResponse<PageStream<T>>>
    where T : class, new()
{
    private HttpClient HttpClient { get; } = httpClient;

    private HttpResponseMessage CurrentResponse { get; } = currentResponse;

    private IEnumerable<Action<HttpRequestMessage>> NewRequest { get; } = newRequest;

    /// <inheritdoc/>
    public async IAsyncEnumerator<PlatformResponse<PageStream<T>>> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        var responseIterator = CurrentResponse;
        while (!cancellationToken.IsCancellationRequested)
        {
            var response = ResponseComposer.Handle<PageStream<T>>(
                responseIterator,
                ResponseComposer.DeserializeResponseOnSuccess,
                ResponseComposer.DeserializeProblemDetailsOnUnsuccessStatusCode
            );

            if (response.IsSuccessful)
            {
                var nextPage = FetchNextPage(response.Content, cancellationToken);
                yield return response;

                responseIterator.Dispose();
                responseIterator = await nextPage;

                if (responseIterator == null)
                {
                    yield break;
                }
            }
            else
            {
                yield return response;
                yield break;
            }
        }
    }

    /// <summary>
    /// Fetches the next page of data from the register.
    /// </summary>
    /// <param name="content">Current page content.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The next HTTP response message.</returns>
    private async Task<HttpResponseMessage> FetchNextPage(PageStream<T> content, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(content.Links.Next))
        {
            return null;
        }

        var request = NewRequest.Append(RequestComposer.WithSetUri(content.Links.Next));
        return await HttpClient.SendAsync(RequestComposer.New([.. request]), cancellationToken);
    }
}

/// <summary>
/// Represents a paginated stream of data with associated metadata and navigation links.
/// </summary>
/// <typeparam name="T">The type of data contained in the stream.</typeparam>
public class PageStream<T>
{
    /// <summary>
    /// Gets or sets the statistics related to the pagination of the stream.
    /// </summary>
    [JsonPropertyName("stats")]
    public StatsStream Stats { get; set; }

    /// <summary>
    /// Gets or sets the navigation links for the paginated stream.
    /// </summary>
    [JsonPropertyName("links")]
    public LinksStream Links { get; set; }

    /// <summary>
    /// Gets or sets the collection of data items in the current page of the stream.
    /// </summary>
    [JsonPropertyName("data")]
    public IEnumerable<T> Data { get; set; }

    /// <summary>
    /// Represents the navigation links for the paginated stream.
    /// </summary>
    public class LinksStream
    {
        /// <summary>
        /// Gets or sets the URL to the next page in the stream.
        /// </summary>
        [JsonPropertyName("next")]
        public string Next { get; set; }
    }

    /// <summary>
    /// Represents statistical information about the paginated stream.
    /// </summary>
    public class StatsStream
    {
        /// <summary>
        /// Gets or sets the starting position of the current page in the stream.
        /// </summary>
        [JsonPropertyName("pageStart")]
        public ulong PageStart { get; set; }

        /// <summary>
        /// Gets or sets the ending position of the current page in the stream.
        /// </summary>
        [JsonPropertyName("pageEnd")]
        public ulong PageEnd { get; set; }

        /// <summary>
        /// Gets or sets the maximum sequence value available in the stream.
        /// </summary>
        [JsonPropertyName("sequenceMax")]
        public ulong SequenceMax { get; set; }
    }
}
