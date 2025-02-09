using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Altinn.Authorization.Integration.Platform;

/// <summary>
/// Handles asynchronous pagination of HTTP responses.
/// </summary>
/// <typeparam name="T">The type of items being paginated.</typeparam>
/// <param name="httpClient">The HTTP client to make requests.</param>
/// <param name="currentResponse">The current HTTP response message.</param>
/// <param name="newRequest">The request actions for fetching the next page.</param>
public class PaginatorStream<T>(HttpClient httpClient, HttpResponseMessage currentResponse, IEnumerable<Action<HttpRequestMessage>> newRequest) : IAsyncEnumerable<Paginated<T>>
    where T : class, new()
{
    private HttpClient HttpClient { get; } = httpClient;

    private HttpResponseMessage CurrentResponse { get; } = currentResponse;

    private IEnumerable<Action<HttpRequestMessage>> NewRequest { get; } = newRequest;

    /// <inheritdoc/>
    public async IAsyncEnumerator<Paginated<T>> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        var pageResponse = CurrentResponse;
        while (!cancellationToken.IsCancellationRequested)
        {
            if (pageResponse.IsSuccessStatusCode)
            {
                var content = await pageResponse.Content.ReadFromJsonAsync<Paginated<T>>(cancellationToken);

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
    /// <returns>The next HTTP response message.</returns>
    private async Task<HttpResponseMessage> FetchNextPage(Paginated<T> content, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(content.Links.Next))
        {
            return null;
        }

        var request = NewRequest.Append(RequestCompositor.WithSetUri(content.Links.Next));
        return await HttpClient.SendAsync(RequestCompositor.New([.. request]), cancellationToken);
    }
}

/// <summary>
/// Represents a stream of paginated items.
/// </summary>
/// <typeparam name="T">The type of items in the stream.</typeparam>
/// <param name="Links">Pagination links.</param>
/// <param name="Stats">Stream statistics.</param>
/// <param name="Items">The collection of items.</param>
public record ItemStream<T>(
    PaginatedLinks Links,
    ItemStreamStats Stats,
    IEnumerable<T> Items)
    : Paginated<T>(Links, Items);

/// <summary>
/// Represents item stream statistics.
/// </summary>
/// <param name="PageStart">The index of the first item on the page.</param>
/// <param name="PageEnd">The index of the last item on the page.</param>
/// <param name="SequenceMax">The highest item index in the database.</param>
public record ItemStreamStats(
    ulong PageStart,
    ulong PageEnd,
    ulong SequenceMax);

/// <summary>
/// Represents a paginated list of items.
/// </summary>
/// <typeparam name="T">The type of items.</typeparam>
/// <param name="Links">Pagination links.</param>
/// <param name="Items">The list of items.</param>
public record Paginated<T>(
    PaginatedLinks Links,
    IEnumerable<T> Items)
    : ListObject<T>(Items);

/// <summary>
/// Represents pagination links.
/// </summary>
/// <param name="Next">The URL to the next page of items (if available).</param>
public record PaginatedLinks(
    string Next);

/// <summary>
/// Represents a generic list object.
/// </summary>
/// <typeparam name="T">The type of items.</typeparam>
/// <param name="Items">The list of items.</param>
public record ListObject<T>(
    [property: JsonPropertyName("data")]
    IEnumerable<T> Items);
