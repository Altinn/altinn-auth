using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.Authorization.Integration.Platform.Notification.Models;

namespace Altinn.Authorization.Integration.Platform.Notification;

/// <summary>
/// Client for sending notifications through the Altinn Notifications API.
/// </summary>
public partial class AltinnNotificationClient
{
    private static JsonSerializerOptions SerializerOptions { get; set; } = new JsonSerializerOptions
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <inheritdoc/>
    public async Task<PlatformResponse<NotificationOrderChainResponseExt>> Send(
        NotificationOrderChainRequestExt model,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<Action<HttpRequestMessage>> request = [
            RequestComposer.WithHttpVerb(HttpMethod.Post),
            RequestComposer.WithJSONPayload(model, SerializerOptions),
            RequestComposer.WithSetUri(NotificationOptions.Value.Endpoint, "/notifications/api/v1/future/orders"),
            RequestComposer.WithPlatformAccessToken(async () => await TokenGenerator.CreatePlatformAccessToken(cancellationToken))
        ];

        var response = await HttpClient.SendAsync(RequestComposer.New([.. request]), cancellationToken);

        return ResponseComposer.Handle<NotificationOrderChainResponseExt>(
            response,
            ResponseComposer.DeserializeResponseOnSuccess,
            ResponseComposer.DeserializeProblemDetailsOnUnsuccessStatusCode
        );
    }
}
