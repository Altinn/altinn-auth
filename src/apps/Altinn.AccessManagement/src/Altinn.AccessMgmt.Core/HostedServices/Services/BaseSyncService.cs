using System.Net;
using Microsoft.Extensions.Logging;

namespace Altinn.AccessMgmt.Core.HostedServices.Services;

/// <summary>
/// Base
/// </summary>
public abstract class BaseSyncService { }

/// <summary>
/// Logs for Sync Services
/// </summary>
public static partial class Log
{
    [LoggerMessage(EventId = 11, Level = LogLevel.Information, Message = "Failed to retrieve updated resources from resource register, got {StatusCode}")]
    internal static partial void UpdatedResourceError(ILogger logger, HttpStatusCode statusCode);

    [LoggerMessage(EventId = 10, Level = LogLevel.Information, Message = "Failed to retrieve service owners from resource register, got {StatusCode}")]
    internal static partial void ServiceOwnerError(ILogger logger, HttpStatusCode statusCode);

    [LoggerMessage(EventId = 9, Level = LogLevel.Information, Message = "Error occured while fetching data from register, got {StatusCode}")]
    internal static partial void ResponseError(ILogger logger, HttpStatusCode statusCode);

    [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Processing party with uuid {PartyUuid} from register. RetryCount {Count}")]
    internal static partial void Party(ILogger logger, string partyUuid, int count);

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "An error occured while streaming data from register")]
    internal static partial void SyncError(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Starting register hosted service")]
    internal static partial void StartRegisterSync(ILogger logger);

    [LoggerMessage(EventId = 22, Level = LogLevel.Information, Message = "Starting altinnrole hosted service")]
    internal static partial void StartAltinnRoleSync(ILogger logger);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Quit register hosted service")]
    internal static partial void QuitRegisterSync(ILogger logger);

    [LoggerMessage(EventId = 23, Level = LogLevel.Information, Message = "Quit altinnrole hosted service")]
    internal static partial void QuitAltinnRoleSync(ILogger logger);

    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Assignment {Action} from '{From}' to '{To}' with role '{Role}'")]
    internal static partial void AssignmentSuccess(ILogger logger, string action, string from, string to, string role);

    [LoggerMessage(EventId = 5, Level = LogLevel.Warning, Message = "Failed to {Action} assingment from '{From}' to '{To}' with role '{Role}'")]
    internal static partial void AssignmentFailed(ILogger logger, string action, string from, string to, string role);
}
