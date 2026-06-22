using Altinn.AccessMgmt.FFB.Config;
using Altinn.AccessMgmt.FFB.Services.Contracts;
using Microsoft.Extensions.Options;

namespace Altinn.AccessMgmt.FFB.Services;

/// <summary>
/// Sends Telegram messages to configured channels.
/// Info-level channels receive all messages; Error-level channels receive only errors.
/// </summary>
public sealed class TelegramNotificationService(
    IHttpClientFactory httpClientFactory,
    IOptions<NotificationsConfig> options,
    ILogger<TelegramNotificationService> logger) : INotificationService
{
    private readonly TelegramConfig? _config = options.Value.Telegram;

    public async Task SendAsync(string environment, NotificationLevel level, string message)
    {
        if (_config is null || !_config.IsConfigured)
        {
            return;
        }

        var targets = _config.Channels.Where(c =>
            (c.Environment == "*" || string.Equals(c.Environment, environment, StringComparison.OrdinalIgnoreCase)) &&
            (c.Level == NotificationLevel.Info || c.Level == level));

        var client = httpClientFactory.CreateClient("telegram");
        var url = $"https://api.telegram.org/bot{_config.BotToken}/sendMessage";

        foreach (var channel in targets)
        {
            try
            {
                var payload = new
                {
                    chat_id = channel.ChatId,
                    text = message,
                    parse_mode = "HTML",
                };

                var response = await client.PostAsJsonAsync(url, payload);
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    logger.LogWarning(
                        "Telegram delivery failed for chat {ChatId}: {Status} — {Body}",
                        channel.ChatId, (int)response.StatusCode, body);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Telegram delivery threw for chat {ChatId}", channel.ChatId);
            }
        }
    }
}
