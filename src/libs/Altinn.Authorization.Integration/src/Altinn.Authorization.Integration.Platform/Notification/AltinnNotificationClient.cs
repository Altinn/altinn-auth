using Altinn.Authorization.Integration.Platform.Notification.Models;
using Microsoft.Extensions.Options;

namespace Altinn.Authorization.Integration.Platform.Notification;

/// <summary>
/// Client for interacting with the Altinn Register service.
/// </summary>
/// <param name="HttpClientFactory">Factory for creating HTTP clients.</param>
/// <param name="NotificationOptions">Options for configuring the Altinn Register services.</param>
/// <param name="PlatformOptions">Options for configuring platform integration services.</param>
/// <param name="TokenGenerator">Service for generating authentication tokens.</param>
public partial class AltinnNotificationClient(
    IHttpClientFactory HttpClientFactory,
    IOptions<AltinnNotificationOptions> NotificationOptions,
    IOptions<AltinnIntegrationOptions> PlatformOptions,
    ITokenGenerator TokenGenerator
) : IAltinnNotification
{
    private HttpClient HttpClient => HttpClientFactory.CreateClient(PlatformOptions.Value.HttpClientName);
}

/// <summary>
/// Defines operations for sending notifications through the Altinn Notifications service.
/// </summary>
public interface IAltinnNotification
{
    /// <summary>
    /// Sends an notification through the Altinn Notifications service.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the platform response
    /// with details about the created SMS notification order.
    /// </returns>
    Task<PlatformResponse<NotificationOrderChainResponseExt>> Send(NotificationOrderChainRequestExt model,CancellationToken cancellationToken = default);
}
