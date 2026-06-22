using Altinn.AccessMgmt.FFB.Config;

namespace Altinn.AccessMgmt.FFB.Services.Contracts;

public interface INotificationService
{
    Task SendAsync(string environment, NotificationLevel level, string message);
}
