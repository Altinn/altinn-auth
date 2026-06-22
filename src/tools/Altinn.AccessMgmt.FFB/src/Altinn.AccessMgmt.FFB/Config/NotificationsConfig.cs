namespace Altinn.AccessMgmt.FFB.Config;

public class NotificationsConfig
{
    public TelegramConfig? Telegram { get; set; }
}

public class TelegramConfig
{
    public string BotToken { get; set; } = string.Empty;

    public List<TelegramChannelConfig> Channels { get; set; } = [];

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(BotToken) && Channels.Count > 0;
}

public class TelegramChannelConfig
{
    /// <summary>
    /// Environment name this channel receives notifications for.
    /// Use "*" to receive notifications for all environments.
    /// </summary>
    public string Environment { get; set; } = string.Empty;

    /// <summary>
    /// <see cref="NotificationLevel.Info"/> channels receive everything (starts, completions, failures).
    /// <see cref="NotificationLevel.Error"/> channels receive only failures.
    /// </summary>
    public NotificationLevel Level { get; set; } = NotificationLevel.Error;

    public string ChatId { get; set; } = string.Empty;
}

public enum NotificationLevel
{
    Info,
    Error,
}
