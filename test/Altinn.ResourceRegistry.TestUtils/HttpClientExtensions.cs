using System.Diagnostics.CodeAnalysis;

namespace Altinn.ResourceRegistry.TestUtils;

public static class HttpClientExtensions
{
    public static Task<HttpResponseMessage> DeleteAsync(this HttpClient client, [StringSyntax("Uri")] string url, HttpContent content)
    {
        var message = new HttpRequestMessage(HttpMethod.Delete, url)
        {
            Content = content
        };

        return client.SendAsync(message);
    }
}
